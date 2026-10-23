using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 逃跑場景的總控制（玩家必定失敗版）：
///   - 開始時：玩家移動變慢、啟動攻擊導演與中央文字。
///   - 結束：玩家被打死（或到時間被強制倒下）→ 等一下 → 用 SceneTransition 進下一個場景。
///
/// 放在逃跑場景任何一個物件上（建議建一個空物件叫 FleeSystem）。
/// 注意：玩家要有 "Player" tag，身上要有 PlayerCombatController2D 與 EntityHealthComponent。
/// </summary>
public class FleeSceneController : MonoBehaviour
{
    [Header("玩家")]
    [SerializeField] private string playerTag = "Player";
    [Tooltip("玩家移動速度倍率（1 = 原速，0.5 = 一半）。")]
    [SerializeField, Range(0.1f, 1f)] private float playerSpeedMultiplier = 0.5f;

    [Header("必定失敗")]
    [Tooltip("保險用計時：開始後幾秒仍沒進入結局就強制進入。0 = 不使用（只靠時間軸的 Finale 步驟或玩家被打死）。")]
    [SerializeField] private float forceDefeatAfterSeconds = 40f;
    [Tooltip("結局開始後，等幾秒才強制讓玩家倒下（留給最後一擊的演出時間）。")]
    [SerializeField] private float killDelaySeconds = 2f;
    [Tooltip("結局開始時觸發（可接鏡頭震動、音效、閃白等）。")]
    [SerializeField] private UnityEvent onFinaleStarted;

    [Header("結束")]
    [Tooltip("玩家倒下後要切去的場景（要先加入 Build Settings）。")]
    [SerializeField] private string nextSceneName;
    [Tooltip("玩家倒下後，等幾秒才開始切場景。")]
    [SerializeField] private float delayAfterDeathSeconds = 1.5f;
    [SerializeField] private float fadeOutSeconds = 0.8f;
    [SerializeField] private float fadeInSeconds = 0.6f;
    [Tooltip("玩家倒下時觸發（在切場景之前）。")]
    [SerializeField] private UnityEvent onPlayerDefeated;

    [Header("其他系統（選填）")]
    [SerializeField] private FleeAttackDirector director;
    [SerializeField] private FleeTextSequence textSequence;
    [Tooltip("劇情 / QTE / 攻擊的時間軸。它的 Finale 步驟會呼叫本控制的 ForceDefeatNow()。")]
    [SerializeField] private FleeSequence sequence;
    [SerializeField] private FleeCoverSpawner coverSpawner;
    [SerializeField] private bool startOnStart = true;

    private GameObject player;
    private EntityHealthComponent health;
    private bool ended;
    private bool finaleStarted;
    private PlayerCombatController2D moveController;

    // 每幀同步倍率：在 Play 模式中改 Inspector 的數值也會立刻生效
    private void Update()
    {
        if (moveController != null && !ended)
            moveController.MoveSpeedMultiplier = playerSpeedMultiplier;
    }

    private void Start()
    {
        if (startOnStart) Run().Forget();
    }

    private void OnDestroy()
    {
        if (health != null) health.OnDeath -= OnPlayerDeath;
    }

    private async UniTaskVoid Run()
    {
        var token = this.GetCancellationTokenOnDestroy();

        // 玩家可能晚一點才生成，最多等 5 秒
        float waited = 0f;
        while (player == null && waited < 5f)
        {
            player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null) break;
            await UniTask.Yield(token);
            waited += Time.deltaTime;
        }

        if (player == null)
        {
            Debug.LogError($"[FleeSceneController] 找不到 Tag 為 {playerTag} 的玩家。", this);
            return;
        }

        moveController = player.GetComponentInChildren<PlayerCombatController2D>();
        if (moveController == null) moveController = player.GetComponentInParent<PlayerCombatController2D>();
        if (moveController != null)
        {
            moveController.MoveSpeedMultiplier = playerSpeedMultiplier;
            Debug.Log($"[FleeSceneController] 已套用玩家速度倍率 {playerSpeedMultiplier}（物件：{moveController.name}）", moveController);
        }
        else Debug.LogWarning("[FleeSceneController] 玩家身上找不到 PlayerCombatController2D，無法減速。", this);

        health = player.GetComponentInChildren<EntityHealthComponent>();
        if (health == null) health = player.GetComponentInParent<EntityHealthComponent>();
        if (health != null) health.OnDeath += OnPlayerDeath;
        else Debug.LogWarning("[FleeSceneController] 玩家身上找不到 EntityHealthComponent，無法偵測死亡。", this);

        // 劇情文字與攻擊由 FleeSequence 的時間軸驅動；沒有時間軸時才用舊的自動模式
        coverSpawner?.StartLoop();
        if (sequence != null)
        {
            sequence.Play();
        }
        else
        {
            director?.StartLoop();
            textSequence?.Play();
        }

        if (forceDefeatAfterSeconds > 0f)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(forceDefeatAfterSeconds), cancellationToken: token);
            if (!ended) await ForceDefeat(token);
        }
    }

    /// <summary>進入結局（可由時間軸的 Finale 步驟呼叫）：等一下之後強制讓玩家倒下。</summary>
    public void ForceDefeatNow()
    {
        if (ended || finaleStarted) return;
        ForceDefeat(this.GetCancellationTokenOnDestroy()).Forget();
    }

    /// <summary>結局：停止一般攻擊、放最後一擊，等一下之後強制讓玩家倒下。</summary>
    private async UniTask ForceDefeat(System.Threading.CancellationToken token)
    {
        if (finaleStarted) return;
        finaleStarted = true;

        director?.StartFinale();
        onFinaleStarted?.Invoke();

        await UniTask.Delay(TimeSpan.FromSeconds(killDelaySeconds), cancellationToken: token);
        if (ended) return;

        if (health != null)
        {
            health.TakeDamage(new DamagePayload
            {
                Damage = int.MaxValue / 2,
                IsCrit = false,
                Source = gameObject,
                IgnoreInvincibility = true // 連 dash 的無敵都擋不住
            });
        }

        // 保險：萬一血量系統沒有觸發死亡，1 秒後仍然進入結束流程
        await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: token);
        if (!ended) OnPlayerDeath();
    }

    private void OnPlayerDeath()
    {
        if (ended) return;
        ended = true;
        End().Forget();
    }

    private async UniTaskVoid End()
    {
        var token = this.GetCancellationTokenOnDestroy();

        director?.StopLoop();
        coverSpawner?.StopLoop();
        sequence?.Stop();
        onPlayerDefeated?.Invoke();

        await UniTask.Delay(TimeSpan.FromSeconds(delayAfterDeathSeconds), cancellationToken: token);

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("[FleeSceneController] Next Scene Name 是空的，不會切場景。", this);
            return;
        }

        SceneTransition.Load(nextSceneName, fadeOutSeconds, fadeInSeconds).Forget();
    }
}