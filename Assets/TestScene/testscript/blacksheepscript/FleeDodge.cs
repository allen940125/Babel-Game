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
    [Tooltip("走進掩體花多少秒。")]
    [SerializeField] private float enterSeconds = 0.25f;
    [Tooltip("躲藏時玩家的透明度。")]
    [SerializeField, Range(0f, 1f)] private float hiddenAlpha = 0.25f;
    [Tooltip("離開掩體後，再多無敵幾秒，避免剛出來就被打到。")]
    [SerializeField] private float graceSeconds = 0.5f;

    public bool IsHiding { get; private set; }

    private GameObject player;
    private EntityRuntime data;
    private SpriteRenderer sprite;
    private Rigidbody body;

    private bool EnsurePlayer()
    {
        if (player != null && data != null) return true;

        player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null) return false;

        var core = player.GetComponentInChildren<EntityCore>();
        if (core == null) core = player.GetComponentInParent<EntityCore>();
        data = core != null ? core.RuntimeData : null;

        sprite = player.GetComponentInChildren<SpriteRenderer>();
        body = player.GetComponent<Rigidbody>();

        if (data == null) Debug.LogWarning("[FleeDodge] 找不到玩家的 EntityCore / RuntimeData，無法無敵。", this);
        return data != null;
    }

    /// <summary>躲進掩體，躲 seconds 秒，結束後才回傳。</summary>
    public async UniTask HideIn(FleeCover cover, float seconds)
    {
        if (cover == null || IsHiding || !EnsurePlayer()) return;

        var token = this.GetCancellationTokenOnDestroy();
        IsHiding = true;
        cover.Enter();
        data.AddState(EntityStateFlags.Invincible | EntityStateFlags.Stunned);

        // 走進掩體
        Vector3 from = player.transform.position;
        for (float t = 0f; t < enterSeconds; t += Time.deltaTime)
        {
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, enterSeconds));
            Place(Vector3.Lerp(from, Target(cover), k));
            SetAlpha(Mathf.Lerp(1f, hiddenAlpha, k));
            await UniTask.Yield(token);
        }

        // 躲藏
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            Place(Target(cover));
            SetAlpha(hiddenAlpha);
            await UniTask.Yield(token);
        }

        // 離開
        SetAlpha(1f);
        data.RemoveState(EntityStateFlags.Stunned);
        cover.Leave();

        await UniTask.Delay(TimeSpan.FromSeconds(graceSeconds), cancellationToken: token);
        data.RemoveState(EntityStateFlags.Invincible);
        IsHiding = false;
    }

    private static Vector3 Target(FleeCover cover)
    {
        Vector3 p = cover.HidePosition;
        p.z = 0f;
        return p;
    }

    private void Place(Vector3 position)
    {
        player.transform.position = position;
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
