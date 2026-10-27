using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 玩家躲進破碎 UI：玩家移動到掩體的躲藏點、半透明、無敵、不能動，躲完再恢復。
/// 躲藏靠狀態旗標（Invincible + Stunned）與你現有的血量／移動系統配合，不需要碰撞。
/// 由 FleeSequence 在 QTE 成功時呼叫 HideIn()。
/// </summary>
public class FleeDodge : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [Tooltip("往旁邊撲進掩體後面花多少秒。越短越像一個動作。")]
    [SerializeField] private float diveSeconds = 0.2f;
    [Tooltip("撲的時候往上彈起的高度（做出一躍的感覺）。0 = 平移。")]
    [SerializeField] private float diveArcHeight = 0.3f;
    [Tooltip("躲在掩體後面時玩家的透明度。1 = 完全不透明（看起來就是被擋在後面）。")]
    [SerializeField, Range(0f, 1f)] private float hiddenAlpha = 1f;
    [Tooltip("離開掩體後，再多無敵幾秒，避免剛出來就被打到。")]
    [SerializeField] private float graceSeconds = 0.5f;

    public bool IsHiding { get; private set; }

    private GameObject player;      // Tag 為 Player 的物件（可能只是底下的 Hitbox）
    private Transform root;          // 真正要移動的玩家根物件（有 Rigidbody 的那個）
    private EntityRuntime data;
    private SpriteRenderer sprite;
    private Rigidbody body;

    private bool EnsurePlayer()
    {
        if (player != null && data != null) return true;

        player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null) return false;

        // Player Tag 可能掛在底下的 Physics_Hitbox 上。要移動的是有 Rigidbody 的根物件，
        // 否則只有 Hitbox 被移走，玩家的圖和操作都留在原地。
        body = player.GetComponentInParent<Rigidbody>();
        root = body != null ? body.transform : player.transform.root;

        var core = root.GetComponentInChildren<EntityCore>();
        if (core == null) core = player.GetComponentInParent<EntityCore>();
        data = core != null ? core.RuntimeData : null;

        sprite = root.GetComponentInChildren<SpriteRenderer>();

        if (data == null) Debug.LogWarning("[FleeDodge] 找不到玩家的 EntityCore / RuntimeData，無法無敵。", this);
        return data != null;
    }

    /// <summary>躲進掩體，躲 seconds 秒，結束後才回傳。</summary>
    public async UniTask HideIn(FleeCover cover, float seconds)
    {
        if (cover == null) { Debug.LogWarning("[FleeDodge] 沒有掩體可以躲。", this); return; }
        if (IsHiding) { Debug.LogWarning("[FleeDodge] 已經在躲了，忽略這次。", this); return; }
        if (!EnsurePlayer()) return;

        var token = this.GetCancellationTokenOnDestroy();
        IsHiding = true;

        Vector3 from = root.position;
        Vector3 to = Target(cover);
        Debug.Log($"[FleeDodge] 開始躲：掩體 {cover.name}，玩家 {from} → 目標 {to}（HidePoint 是否有指定：{cover.HasHidePoint}）", cover);

        // 不管中途發生什麼錯誤，結束時都一定要把玩家恢復（否則會永遠動不了）
        try
        {
            cover.Enter();
            data.AddState(EntityStateFlags.Invincible | EntityStateFlags.Stunned);

            // 掩體有 Collider（要用來擋子彈）。躲藏期間讓玩家穿得進去，不然物理會把玩家推出來。
            SetIgnoreCollision(cover, true);

            // 往旁邊一撲：快速、先快後慢，中間往上彈一下
            for (float t = 0f; t < diveSeconds; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / Mathf.Max(0.01f, diveSeconds));
                float eased = 1f - (1f - k) * (1f - k);
                Vector3 pos = Vector3.Lerp(from, to, eased);
                pos.y += Mathf.Sin(k * Mathf.PI) * diveArcHeight;
                Place(pos);
                SetAlpha(Mathf.Lerp(1f, hiddenAlpha, k));
                await UniTask.Yield(token);
            }
            Place(to);
            Debug.Log($"[FleeDodge] 撲完：玩家實際位置 {root.position}，目標 {to}，被移動的物件 {root.name}，Rigidbody：{(body != null ? $"isKinematic={body.isKinematic}, constraints={body.constraints}" : "沒有（在這個物件上找不到 Rigidbody）")}", this);

            // 躲藏
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                Place(Target(cover));
                SetAlpha(hiddenAlpha);
                await UniTask.Yield(token);
            }
        }
        finally
        {
            Debug.Log($"[FleeDodge] 躲完：玩家位置 {root.position}", this);
            SetAlpha(1f);
            if (data != null) data.RemoveState(EntityStateFlags.Stunned);
            if (cover != null)
            {
                cover.Leave();
                SetIgnoreCollision(cover, false);
            }
        }

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(graceSeconds), cancellationToken: token);
        }
        finally
        {
            if (data != null) data.RemoveState(EntityStateFlags.Invincible);
            IsHiding = false;
        }
    }

    /// <summary>讓玩家的所有 Collider 與掩體的所有 Collider 互相忽略（或恢復）碰撞。</summary>
    private void SetIgnoreCollision(FleeCover cover, bool ignore)
    {
        if (cover == null || root == null) return;

        var mine = root.GetComponentsInChildren<Collider>();
        var theirs = cover.GetComponentsInChildren<Collider>();
        foreach (var a in mine)
        {
            if (a == null) continue;
            foreach (var b in theirs)
                if (b != null) Physics.IgnoreCollision(a, b, ignore);
        }
    }

    private static Vector3 Target(FleeCover cover)
    {
        Vector3 p = cover.HidePosition;
        p.z = 0f;
        return p;
    }

    private void Place(Vector3 position)
    {
        root.position = position;
        if (body != null) body.position = position;
    }

    private void SetAlpha(float a)
    {
        if (sprite == null) return;
        Color c = sprite.color;
        c.a = a;
        sprite.color = c;
    }
}