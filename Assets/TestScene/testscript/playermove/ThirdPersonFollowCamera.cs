using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>
/// 第三人稱跟隨鏡頭：固定在主角背後（世界座標方向固定），平滑跟著主角走。
/// 並可處理「鏡頭被障礙物擋住」：
///   Hide Blockers  → 鏡頭和主角之間的物件暫時隱藏（只留影子），離開就恢復。鏡頭位置不變，永遠看得到主角。
///   Pull Closer    → 鏡頭被牆擋住時往主角靠近。
///
/// 用法：掛在 Main Camera 上，Target 指定主角。
/// 注意：障礙物要有 Collider（大型模型請加 Mesh Collider 或 Box Collider）才偵測得到。
/// </summary>
[DefaultExecutionOrder(100)]
public class ThirdPersonFollowCamera : MonoBehaviour
{
    public enum ObstacleMode { None, HideBlockers, PullCloser, Both }

    [SerializeField] private Transform target;

    [Header("位置")]
    [Tooltip("鏡頭從主角往『後方』退多遠（公尺）。")]
    [SerializeField] private float distance = 1.5f;
    [Tooltip("鏡頭比主角腳底高多少（公尺）。")]
    [SerializeField] private float height = 0.8f;
    [Tooltip("鏡頭在世界中的水平朝向（度）。0 = 看向 +Z；90 = 看向 +X；180 = 看向 -Z；270 = 看向 -X。")]
    [SerializeField] private float yaw = 0f;

    [Header("滑鼠左右轉動視角")]
    [SerializeField] private bool mouseLook = true;
    [Tooltip("勾選 = 按住滑鼠右鍵才轉（平常游標正常顯示，不影響點擊 UI）。取消 = 一直跟著滑鼠轉。")]
    [SerializeField] private bool holdRightMouseToRotate = true;
    [Tooltip("轉動速度（度 / 每像素）。覺得太快就調小。")]
    [SerializeField] private float mouseSensitivity = 0.15f;
    [Tooltip("轉動方向反了就勾這個。")]
    [SerializeField] private bool invertHorizontal = false;
    [Tooltip("限制轉動範圍：以遊戲開始時的 Yaw 為中心，左右各只能轉 Yaw Limit 度。取消勾選 = 可以轉整圈。")]
    [SerializeField] private bool limitRotation = true;
    [Tooltip("從中心往左、往右各能轉多少度。例如 60 = 總共可轉 120 度。")]
    [SerializeField, Range(1f, 180f)] private float yawLimit = 60f;
    [Tooltip("按住右鍵轉動時，隱藏並鎖住游標。")]
    [SerializeField] private bool lockCursorWhileRotating = true;

    [Header("看哪裡")]
    [SerializeField] private float lookHeight = 0.25f;

    [Header("跟隨手感")]
    [SerializeField] private float followSharpness = 8f;

    [Header("障礙物處理")]
    [Tooltip("建議 Hide Blockers：不管怎麼移動，擋在中間的東西都會暫時消失。")]
    [SerializeField] private ObstacleMode obstacleMode = ObstacleMode.HideBlockers;
    [Tooltip("哪些 Layer 算障礙物。主角自己（Target 底下的東西）會自動排除。")]
    [SerializeField] private LayerMask obstacleMask = ~0;
    [Tooltip("偵測用的射線粗細（公尺）。越粗，擦邊的東西也會被隱藏。")]
    [SerializeField] private float obstacleRadius = 0.2f;
    [Tooltip("Pull Closer 時，鏡頭最近能靠到離主角多近。")]
    [SerializeField] private float minDistance = 0.5f;

    private readonly RaycastHit[] hitBuffer = new RaycastHit[32];
    private readonly Dictionary<Renderer, ShadowCastingMode> hidden = new Dictionary<Renderer, ShadowCastingMode>();
    private readonly HashSet<Renderer> thisFrame = new HashSet<Renderer>();
    private readonly List<Renderer> toRestore = new List<Renderer>();

    private float centerYaw;

    private void Awake()
    {
        centerYaw = yaw;
    }

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("[ThirdPersonFollowCamera] 尚未指定 Target。", this);
            return;
        }
        Snap();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        HandleMouseLook();

        Vector3 desired = ComputeDesiredPosition();
        float k = followSharpness <= 0f ? 1f : 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        Vector3 pos = Vector3.Lerp(transform.position, desired, k);

        Vector3 pivot = target.position + Vector3.up * lookHeight;

        if (obstacleMode == ObstacleMode.PullCloser || obstacleMode == ObstacleMode.Both)
            pos = PullCloser(pivot, pos);

        transform.position = pos;
        transform.rotation = Quaternion.LookRotation(pivot - pos, Vector3.up);

        if (obstacleMode == ObstacleMode.HideBlockers || obstacleMode == ObstacleMode.Both)
            UpdateHiddenBlockers(pivot, pos);
        else
            RestoreAll();
    }

    private bool rotating;

    private void HandleMouseLook()
    {
        bool allowed = mouseLook
                       && Mouse.current != null
                       && Time.timeScale > 0f
                       && !ShowcaseOverlay.IsShowing
                       && !FullscreenImage.IsShowing
                       && !IsDialoguePlaying();

        bool wantRotate = allowed && (!holdRightMouseToRotate || Mouse.current.rightButton.isPressed);

        if (wantRotate)
        {
            float dx = Mouse.current.delta.ReadValue().x;
            yaw += (invertHorizontal ? -dx : dx) * mouseSensitivity;
            if (limitRotation)
                yaw = centerYaw + Mathf.Clamp(Mathf.DeltaAngle(centerYaw, yaw), -yawLimit, yawLimit);
            else
                yaw = Mathf.Repeat(yaw, 360f);
        }

        // 游標鎖定只在「按住右鍵轉動」時處理，其他時候不碰，避免干擾小遊戲 / 選單
        if (holdRightMouseToRotate && lockCursorWhileRotating)
        {
            if (wantRotate && !rotating)
            {
                rotating = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (!wantRotate && rotating)
            {
                ReleaseCursor();
            }
        }
    }

    private void ReleaseCursor()
    {
        rotating = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static bool IsDialoguePlaying()
    {
        var dc = Dialoguecontroller.Instance;
        return dc != null && dc.DialogueIsPlaying;
    }

    public void Snap()
    {
        if (target == null) return;
        transform.position = ComputeDesiredPosition();
        Vector3 pivot = target.position + Vector3.up * lookHeight;
        transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
    }

    public void SetTarget(Transform t)
    {
        target = t;
        Snap();
    }

    private Vector3 ComputeDesiredPosition()
    {
        Vector3 back = -(Quaternion.Euler(0f, yaw, 0f) * Vector3.forward);
        return target.position + back * distance + Vector3.up * height;
    }

    // ───────────── Pull Closer ─────────────

    private Vector3 PullCloser(Vector3 pivot, Vector3 camPos)
    {
        Vector3 dir = camPos - pivot;
        float len = dir.magnitude;
        if (len < 0.01f) return camPos;
        dir /= len;

        int count = Physics.SphereCastNonAlloc(pivot, obstacleRadius, dir, hitBuffer, len, obstacleMask, QueryTriggerInteraction.Ignore);
        float nearest = len;
        for (int i = 0; i < count; i++)
        {
            if (IsTargetPart(hitBuffer[i].collider)) continue;
            if (hitBuffer[i].distance < nearest) nearest = hitBuffer[i].distance;
        }
        if (nearest < len) camPos = pivot + dir * Mathf.Max(minDistance, nearest);
        return camPos;
    }

    // ───────────── Hide Blockers ─────────────

    private void UpdateHiddenBlockers(Vector3 pivot, Vector3 camPos)
    {
        thisFrame.Clear();

        Vector3 dir = pivot - camPos;
        float len = dir.magnitude;
        if (len > 0.01f)
        {
            dir /= len;
            // 從鏡頭射向主角，擋在中間的都算障礙
            int count = Physics.SphereCastNonAlloc(camPos, obstacleRadius, dir, hitBuffer, len, obstacleMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var col = hitBuffer[i].collider;
                if (IsTargetPart(col)) continue;

                // 優先用碰撞體所在物件上的 Renderer；沒有才找它底下的
                var r = col.GetComponent<Renderer>();
                if (r != null) thisFrame.Add(r);
                else foreach (var child in col.GetComponentsInChildren<Renderer>()) thisFrame.Add(child);
            }

            // 鏡頭本身已經鑽進物件裡（射線起點在 Collider 內不會有 hit）：用 OverlapSphere 補抓
            var inside = Physics.OverlapSphere(camPos, obstacleRadius + 0.3f, obstacleMask, QueryTriggerInteraction.Ignore);
            foreach (var col in inside)
            {
                if (IsTargetPart(col)) continue;
                var r = col.GetComponent<Renderer>();
                if (r != null) thisFrame.Add(r);
                else foreach (var child in col.GetComponentsInChildren<Renderer>()) thisFrame.Add(child);
            }
        }

        foreach (var r in thisFrame)
        {
            if (r == null || hidden.ContainsKey(r)) continue;
            hidden[r] = r.shadowCastingMode;
            r.shadowCastingMode = ShadowCastingMode.ShadowsOnly; // 看不到，但影子還在，場景不會突然變亮
        }

        toRestore.Clear();
        foreach (var kv in hidden)
            if (kv.Key == null || !thisFrame.Contains(kv.Key)) toRestore.Add(kv.Key);
        foreach (var r in toRestore)
        {
            if (r != null) r.shadowCastingMode = hidden[r];
            hidden.Remove(r);
        }
    }

    private void RestoreAll()
    {
        if (hidden.Count == 0) return;
        foreach (var kv in hidden)
            if (kv.Key != null) kv.Key.shadowCastingMode = kv.Value;
        hidden.Clear();
    }

    private bool IsTargetPart(Collider col)
    {
        return col.transform == target || col.transform.IsChildOf(target);
    }

    private void OnDisable()
    {
        RestoreAll();
        if (rotating) ReleaseCursor();
    }

    private void OnDrawGizmosSelected()
    {
        if (target == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(target.position + Vector3.up * lookHeight, ComputeDesiredPosition());
    }
}