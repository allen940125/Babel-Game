using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 逃跑攻擊導演：每隔一段時間，從清單挑一個攻擊 prefab，擺到畫面指定位置後發射。
/// 攻擊 prefab 就是你現有的發射器（GeneralAttackPattern / PatternSequence 等 AttackPatternBase），
/// 不需要 BOSS（內部以 Execute(null, ...) 呼叫）。
///
/// 做「從畫面下方往上飛」的攻擊：
///   GeneralAttackPattern → Pattern Type = RandomSpray，Use Random Direction 取消，Fixed Angle = 90，
///   Spawn Area Size 的 X 設成比畫面寬，Snap To World Origin 關閉（否則位置會被強制搬到原點）。
/// </summary>
public class FleeAttackDirector : MonoBehaviour
{
    public enum SpawnMode
    {
        BottomRandomX,   // 畫面下緣，X 在範圍內隨機
        BottomAtPlayerX, // 畫面下緣，X 對準玩家
        ScreenCenter     // 畫面正中央
    }

    [Serializable]
    public class AttackEntry
    {
        public string label;
        [Tooltip("攻擊 prefab（根物件上要有 AttackPatternBase，例如 GeneralAttackPattern）")]
        public AttackPatternBase prefab;
        [Min(0f)] public float weight = 1f;
        public SpawnMode mode = SpawnMode.BottomRandomX;
    }

    [Header("攻擊清單")]
    [SerializeField] private List<AttackEntry> attacks = new List<AttackEntry>();

    [Header("節奏")]
    [Tooltip("開始後先等幾秒才發第一個攻擊。")]
    [SerializeField] private float startDelay = 3f;
    [Tooltip("攻擊間隔：一開始的秒數 → 隨時間縮短到結束的秒數（越來越密集）。")]
    [SerializeField] private float intervalStart = 2.5f;
    [SerializeField] private float intervalEnd = 0.8f;
    [Tooltip("間隔從 Start 縮短到 End 花多少秒。")]
    [SerializeField] private float rampSeconds = 30f;
    [Tooltip("傳給發射器的速度倍率（1 = 原速）。")]
    [SerializeField] private float speedMultiplier = 1f;

    [Header("位置（畫面比例：0 = 左/下緣，1 = 右/上緣）")]
    [SerializeField] private Vector2 xRange = new Vector2(0.1f, 0.9f);
    [Tooltip("下緣的位置。負數 = 畫面外；0 = 剛好在邊緣。若場景下方有牆，請調成畫面內一點。")]
    [SerializeField] private float bottomViewportY = -0.05f;

    [Header("結局最後一擊（選填）")]
    [SerializeField] private AttackEntry finale;

    [Header("其他")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private string playerTag = "Player";

    private int loopVersion;
    private bool running;
    private float loopStartTime;
    private Transform player;

    public bool IsRunning => running;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
    }

    /// <summary>開始一般攻擊循環。</summary>
    public void StartLoop()
    {
        if (running) return;
        running = true;
        loopStartTime = Time.time;
        Loop(++loopVersion).Forget();
    }

    /// <summary>停止攻擊循環（已經發射出去的子彈不受影響）。</summary>
    public void StopLoop()
    {
        running = false;
        loopVersion++;
    }

    /// <summary>結局：停止一般攻擊，放出最後一擊。</summary>
    public void StartFinale()
    {
        StopLoop();
        if (finale != null && finale.prefab != null) Spawn(finale);
    }

    /// <summary>立刻發射一個攻擊（測試用，也可以從別的腳本呼叫）。</summary>
    [ContextMenu("Spawn One Attack (Play Mode)")]
    public void SpawnOnce()
    {
        var entry = PickRandom();
        if (entry != null) Spawn(entry);
    }

    private async UniTaskVoid Loop(int version)
    {
        var token = this.GetCancellationTokenOnDestroy();

        await UniTask.Delay(TimeSpan.FromSeconds(startDelay), cancellationToken: token);

        while (version == loopVersion)
        {
            SpawnOnce();

            float t = rampSeconds > 0f ? Mathf.Clamp01((Time.time - loopStartTime) / rampSeconds) : 1f;
            float wait = Mathf.Max(0.05f, Mathf.Lerp(intervalStart, intervalEnd, t));
            await UniTask.Delay(TimeSpan.FromSeconds(wait), cancellationToken: token);
        }
    }

    private AttackEntry PickRandom()
    {
        float total = 0f;
        foreach (var a in attacks)
            if (a != null && a.prefab != null) total += a.weight;

        if (total <= 0f)
        {
            Debug.LogWarning("[FleeAttackDirector] 攻擊清單是空的（或權重都是 0）。", this);
            return null;
        }

        float r = UnityEngine.Random.value * total;
        foreach (var a in attacks)
        {
            if (a == null || a.prefab == null) continue;
            r -= a.weight;
            if (r <= 0f) return a;
        }
        return attacks[attacks.Count - 1];
    }

    private void Spawn(AttackEntry entry) => SpawnAttack(entry.prefab, entry.mode);

    /// <summary>在指定位置模式生成並發射一個攻擊，回傳生成出來的發射器（它發射完會自己銷毀）。</summary>
    public AttackPatternBase SpawnAttack(AttackPatternBase prefab, SpawnMode mode)
    {
        if (prefab == null) return null;

        Vector3 pos = GetSpawnPosition(mode);
        var instance = Instantiate(prefab, pos, Quaternion.identity);
        instance.Execute(null, speedMultiplier, false);
        return instance;
    }

    private Vector3 GetSpawnPosition(SpawnMode mode)
    {
        switch (mode)
        {
            case SpawnMode.BottomAtPlayerX:
            {
                if (player == null)
                {
                    var go = GameObject.FindGameObjectWithTag(playerTag);
                    if (go != null) player = go.transform;
                }
                Vector3 bottom = ViewportToPlane(0.5f, bottomViewportY);
                if (player != null) bottom.x = player.position.x;
                return bottom;
            }
            case SpawnMode.ScreenCenter:
                return ViewportToPlane(0.5f, 0.5f);
            default:
                return ViewportToPlane(UnityEngine.Random.Range(xRange.x, xRange.y), bottomViewportY);
        }
    }

    private Vector3 ViewportToPlane(float vx, float vy) => FleeView.ViewportToPlane(targetCamera, vx, vy);
}
