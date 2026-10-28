using System.Collections;
using Gamemanager;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DamageDealer))] 
[RequireComponent(typeof(KinematicTracker3D))] // ★ 強制掛載速度追蹤器
public class RammingWeaponBehavior3D : MonoBehaviour
{
    [Header("★ 物理撞擊設定")]
    [SerializeField] private LayerMask damageLayer; 
    [SerializeField] private LayerMask interactionLayer; 
    
    [Header("★ 動能閾值 (滿足其一即可判定為攻擊)")]
    [Tooltip("直線揮擊的最小速度")]
    [SerializeField] private float minLinearSpeed = 3.0f;
    [Tooltip("轉手腕甩動的最小角速度 (度/秒)")]
    [SerializeField] private float minAngularSpeed = 300.0f; 

    [Header("★ 狀態設定")]
    [SerializeField] private float rammingCooldown = 0.8f;
    [SerializeField] private bool isArmed = false;

    [SerializeField] private DamageDealer damageDealer;
    
    [Header("★ 事件廣播")]
    public UnityEvent<GameObject> OnRammingSuccess; 
    public UnityEvent<GameObject> OnInteractionHit; 

    private KinematicTracker3D _tracker; // ★ 速度資料來源
    private bool _isOnCooldown = false;
    private GlowBehavior3D _glow;

    private void Awake()
    {
        _tracker = GetComponent<KinematicTracker3D>(); // 抓取追蹤器
        if (damageDealer == null) damageDealer = GetComponent<DamageDealer>();
        _glow = GetComponent<GlowBehavior3D>();

        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterAttackingPhaseEvent, OnBossAttacking);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterIdlePhaseEvent, OnBossIdle);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterAttackingPhaseEvent>(OnBossAttacking);
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterIdlePhaseEvent>(OnBossIdle);
        }
    }

    private void OnCollisionEnter(Collision collision) => TryExecuteRamming(collision.collider.gameObject);
    
    private void OnTriggerEnter(Collider other) => TryExecuteRamming(other.gameObject);

    private void TryExecuteRamming(GameObject target)
    {
        if (target == this.gameObject) return;

        // 1. 基礎防呆
        if (!isArmed || _isOnCooldown || damageDealer == null) return;

        // 2. ★ 動能檢定 (向 Tracker 索取數據)
        bool isFastEnough = _tracker.LinearSpeed >= minLinearSpeed || _tracker.AngularSpeed >= minAngularSpeed;
        
        if (!isFastEnough)
        {
            // 動能不足，判定為輕輕碰到，不構成攻擊
            return; 
        }

        // ==========================================
        // 3. 處理 Interaction Layer (互動：例如扎玩家補血)
        // ==========================================
        if ((interactionLayer.value & (1 << target.layer)) != 0)
        {
            OnInteractionHit?.Invoke(target); 
            StartCoroutine(CooldownRoutine());
            return; 
        }

        // ==========================================
        // 4. 處理 Damage Layer (傷害：例如扎敵人抽血)
        // ==========================================
        if ((damageLayer.value & (1 << target.layer)) != 0)
        {
            var playerRuntime = GameManager.Instance.MainGameMediator.CurrentPlayerRuntime;
            if (playerRuntime != null)
            {
                damageDealer.InjectSnapshot(playerRuntime.TotalAttackPower, playerRuntime.TotalCritRate, playerRuntime.TotalCritMultiplier);
            }

            damageDealer.DealDamageTo(target);
            OnRammingSuccess?.Invoke(target); 
        
            StartCoroutine(CooldownRoutine());
            return;
        }
    }

    private IEnumerator CooldownRoutine()
    {
        _isOnCooldown = true;
        if (_glow != null) _glow.enabled = false;
        yield return new WaitForSeconds(rammingCooldown);
        _isOnCooldown = false;
        if (isArmed && _glow != null) _glow.enabled = true;
    }

    private void OnBossAttacking(BossEnterAttackingPhaseEvent evt) => SetArmedState(false);
    private void OnBossIdle(BossEnterIdlePhaseEvent evt) => SetArmedState(true);

    public void SetArmedState(bool armed)
    {
        isArmed = armed;
        if (_glow != null) _glow.enabled = armed;
    }
}