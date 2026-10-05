using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 黑羊戰專用的移動：玩家「按住 W 才會往前走」，放開就停下。
///   W / 上方向鍵：往前走（沿著 Direction）
///   A / D（或左右方向鍵）：左右移動，限制在路寬範圍內
///   S：預設無效（身後的地面會被回收，退回去會踩空）；需要的話勾 Allow Backward
///
/// 同時累計「往前走了多遠 / 按住 W 多久」，劇情流程可以用它判斷要不要進入下一段。
/// 刻意不依賴同事的 Rigidbody / EntityRuntime，直接改 Transform，
/// 所以掛在任何一個測試用的 Player 物件上都能跑。
/// </summary>
public class RunnerMover : MonoBehaviour
{
    [Header("前進（按住 W）")]
    [SerializeField] private Vector3 direction = Vector3.right;
    [SerializeField] private float speed = 5f;
    [Tooltip("勾選後按 S 可以後退（黑羊戰建議取消）")]
    [SerializeField] private bool allowBackward = false;

    [Header("左右（A / D）")]
    [SerializeField] private float lateralSpeed = 4f;
    [Tooltip("可偏離起跑線的最大距離（左右各一邊，約等於路寬的一半再扣掉角色半徑）")]
    [SerializeField] private float lateralLimit = 4f;

    [Header("動畫（可選）")]
    [SerializeField] private Animator animator;
    [SerializeField] private string runBoolParam = "IsRunning";

    /// <summary>目前是否接受玩家操控（StartRun 開啟，StopRun 關閉）。</summary>
    public bool IsRunning { get; private set; }

    /// <summary>這一幀玩家是否正在按住 W 往前走。</summary>
    public bool IsWalkingForward { get; private set; }

    /// <summary>從上次 ResetWalkCounters 起，累計往前走的距離。</summary>
    public float ForwardDistance { get; private set; }

    /// <summary>從上次 ResetWalkCounters 起，累計按住 W 的秒數。</summary>
    public float WalkedSeconds { get; private set; }

    private Vector3 startPos;
    private bool animState;

    /// <summary>開始接受操控，並以目前位置當作左右範圍的中心。</summary>
    public void StartRun()
    {
        IsRunning = true;
        startPos = transform.position;
    }

    /// <summary>停止接受操控（QTE、畫面中斷時用）。</summary>
    public void StopRun()
    {
        IsRunning = false;
        IsWalkingForward = false;
        SetAnim(false);
    }

    /// <summary>暫停後恢復操控。不會重設左右範圍的中心點。</summary>
    public void ResumeRun()
    {
        IsRunning = true;
    }

    /// <summary>把「走了多遠、走了多久」歸零，每個 Walk 步驟開始時會呼叫。</summary>
    public void ResetWalkCounters()
    {
        ForwardDistance = 0f;
        WalkedSeconds = 0f;
    }

    private void Update()
    {
        if (!IsRunning) return;

        Vector3 forward = direction.normalized;
        // 玩家面向 forward 時的右手邊（Unity 左手座標系）
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        float h = 0f; // 左右
        float v = 0f; // 前後

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
            if (allowBackward && (kb.sKey.isPressed || kb.downArrowKey.isPressed)) v -= 1f;
        }

        Vector3 move = forward * (v * speed) + right * (h * lateralSpeed);
        Vector3 newPos = transform.position + move * Time.deltaTime;

        // 限制左右範圍（以起跑位置為中心）
        float lateral = Vector3.Dot(newPos - startPos, right);
        float clamped = Mathf.Clamp(lateral, -lateralLimit, lateralLimit);
        newPos += right * (clamped - lateral);

        // 累計往前走的距離與時間（只算往前，後退不倒扣）
        float forwardDelta = Vector3.Dot(newPos - transform.position, forward);
        IsWalkingForward = v > 0f;
        if (IsWalkingForward)
        {
            ForwardDistance += Mathf.Max(0f, forwardDelta);
            WalkedSeconds += Time.deltaTime;
        }

        transform.position = newPos;
        SetAnim(v != 0f || h != 0f);
    }

    private void SetAnim(bool running)
    {
        if (running == animState) return;
        animState = running;

        if (animator != null && !string.IsNullOrEmpty(runBoolParam))
            animator.SetBool(runBoolParam, running);
    }
}