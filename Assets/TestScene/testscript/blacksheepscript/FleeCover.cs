using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 破碎 UI 掩體：掛在破碎 UI 的 prefab 上。會跟著地板一起往下捲，捲出畫面就自己消失。
/// 玩家 QTE 成功後會「躲進」它（由 FleeDodge 處理），躲藏期間它會暫停捲動，躲完再繼續。
///
/// 建議：
///   - 不要加會擋玩家的 Collider（躲藏靠無敵判定，不靠碰撞）。
///   - Sprite 的 Order in Layer 設得比玩家高，玩家才會看起來躲在它「裡面」。
/// </summary>
public class FleeCover : MonoBehaviour
{
    public static readonly List<FleeCover> All = new List<FleeCover>();

    [Tooltip("玩家躲進來時的位置。留空 = 這個物件的中心。")]
    [SerializeField] private Transform hidePoint;
    [Tooltip("玩家躲進來時觸發（可做破碎抖動、音效）。")]
    [SerializeField] private UnityEvent onPlayerEnter;
    [Tooltip("玩家離開時觸發。")]
    [SerializeField] private UnityEvent onPlayerLeave;

    public Vector3 HidePosition => hidePoint != null ? hidePoint.position : transform.position;
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
