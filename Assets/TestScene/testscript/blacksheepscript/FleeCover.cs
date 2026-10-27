using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 破碎 UI 掩體：掛在破碎 UI 的 prefab 上。會跟著地板一起往下捲，捲出畫面就自己消失。
/// 玩家 QTE 成功後會「躲進」它（由 FleeDodge 處理），躲藏期間它會暫停捲動，躲完再繼續。
///
/// 建議：
///   - Collider 可以保留（用來擋子彈）。玩家躲進去時，FleeDodge 會暫時讓玩家穿得進去。
///   - 躲藏同時有無敵當保險，就算子彈穿過 Collider 也不會受傷。
///   - 攻擊從畫面下方往上射，所以掩體要在玩家「下面」擋住子彈：
///     HidePoint 放在掩體正上方（掩體上緣再往上一點，玩家站的位置），不是放在掩體中心。
/// </summary>
public class FleeCover : MonoBehaviour
{
    public static readonly List<FleeCover> All = new List<FleeCover>();

    [Tooltip("玩家躲的位置：放在掩體正上方（子彈從下面來，掩體在玩家下面擋住）。留空 = 這個物件的中心。")]
    [SerializeField] private Transform hidePoint;
    [Tooltip("玩家躲進來時觸發（可做破碎抖動、音效）。")]
    [SerializeField] private UnityEvent onPlayerEnter;
    [Tooltip("玩家離開時觸發。")]
    [SerializeField] private UnityEvent onPlayerLeave;

    [Header("躲藏位置")]
    [Tooltip("勾選：自動用掩體所有 Collider 的實際範圍算躲藏位置（水平置中、站在最上緣之上）。UI 子物件跟掩體根物件錯位時也不會跑掉。取消勾選則用 Hide Point。")]
    [SerializeField] private bool autoHidePoint = true;
    [Tooltip("自動模式：玩家站在 Collider 最上緣之上多高。")]
    [SerializeField] private float standOffset = 0.8f;

    [Header("固定外觀")]
    [Tooltip("勾選：每幀把 UI 子物件強制放回「相對掩體根物件的原位」。用來防止 UI 上的腳本（例如 BaseButtonController3D）把它拉回自己的世界座標，跟不上掩體往下捲。")]
    [SerializeField] private bool lockChildrenToRoot = true;

    private struct SavedChild
    {
        public Transform t;
        public Vector3 pos;
        public Quaternion rot;
    }
    private readonly List<SavedChild> savedChildren = new List<SavedChild>();

    public Vector3 HidePosition
    {
        get
        {
            if (autoHidePoint && TryGetColliderBounds(out Bounds b))
                return new Vector3(b.center.x, b.max.y + standOffset, transform.position.z);
            return hidePoint != null ? hidePoint.position : transform.position;
        }
    }
    public bool HasHidePoint => hidePoint != null;
    public bool IsOccupied { get; private set; }

    private ScrollingBackground2D background;
    private float speedMultiplier = 1f;
    private Camera cam;

    /// <summary>由 FleeCoverSpawner 在生成後呼叫。</summary>
    public void Setup(ScrollingBackground2D bg, float multiplier, Camera camera)
    {
        background = bg;
        speedMultiplier = multiplier;
        cam = camera != null ? camera : Camera.main;
    }

    private void Awake()
    {
        // UI 子物件各自有 Rigidbody。沒設成 kinematic 的話，物理會讓它自己往下掉或被子彈推走，
        // 跟不上這個根物件的移動，結果掩體的畫面和碰撞就和 HidePoint 越離越遠。
        foreach (var rb in GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            // 有 Interpolate 的 Rigidbody 會每幀用物理姿勢覆蓋 Transform，跟著父物件移動時會被卡在原地
            rb.interpolation = RigidbodyInterpolation.None;
        }

        // 記住每個子物件相對根物件的位置（HidePoint 不算）
        foreach (Transform child in transform)
        {
            if (hidePoint != null && child == hidePoint) continue;
            savedChildren.Add(new SavedChild { t = child, pos = child.localPosition, rot = child.localRotation });
        }
    }

    private void LateUpdate()
    {
        if (!lockChildrenToRoot) return;
        foreach (var s in savedChildren)
        {
            if (s.t == null) continue;
            s.t.localPosition = s.pos;
            s.t.localRotation = s.rot;
        }
    }

    private bool TryGetColliderBounds(out Bounds bounds)
    {
        bounds = default;
        bool has = false;
        foreach (var c in GetComponentsInChildren<Collider>())
        {
            if (c == null || !c.enabled) continue;
            if (!has) { bounds = c.bounds; has = true; }
            else bounds.Encapsulate(c.bounds);
        }
        return has;
    }

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    private void Update()
    {
        if (IsOccupied) return; // 躲藏期間暫停捲動，玩家才不會被帶出畫面

        float speed = background != null ? background.CurrentScrollSpeed * speedMultiplier : 0f;
        transform.position += Vector3.down * (speed * Time.deltaTime);

        if (cam != null && cam.WorldToViewportPoint(transform.position).y < -0.3f)
            Destroy(gameObject);
    }

    public void Enter()
    {
        IsOccupied = true;
        onPlayerEnter?.Invoke();
    }

    public void Leave()
    {
        IsOccupied = false;
        onPlayerLeave?.Invoke();
    }
}