using System.Collections;
using Gamemanager;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DamageDealer))] 
public class RammingWeaponBehavior3D : MonoBehaviour
{
    [Header("★ 物理撞擊設定 (完全由 Inspector 控制)")]
    [Tooltip("會受到真實傷害的目標層級 (例如 Enemy, Boss)")]
    [SerializeField] private LayerMask damageLayer; 
    
    [Tooltip("不會受到傷害，但會觸發特殊互動事件的目標層級 (例如 Player)")]
    [SerializeField] private LayerMask interactionLayer; 
    
    [SerializeField] private float minImpactSpeed = 5.0f;
    [SerializeField] private float rammingCooldown = 0.8f;
    [SerializeField] private bool isArmed = false;

    [SerializeField] private DamageDealer damageDealer;
    
    [Header("★ 事件廣播")]
    public UnityEvent<GameObject> OnRammingSuccess; // 扎到敵人 (Damage Layer)
    public UnityEvent<GameObject> OnInteractionHit; // 扎到互動目標 (Interaction Layer)

    private Vector3 _lastPosition;
    private float _currentInstantSpeed;
    private bool _isOnCooldown = false;
    private GlowBehavior3D _glow;

    private void Awake()
    {
        if (damageDealer == null) damageDealer = GetComponent<DamageDealer>();
        _glow = GetComponent<GlowBehavior3D>();
        _lastPosition = transform.position;

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

    private void Update()
    {
        _currentInstantSpeed = (transform.position - _lastPosition).magnitude / Time.deltaTime;
        _lastPosition = transform.position;
    }

    private void OnCollisionEnter(Collision collision) => TryExecuteRamming(collision.collider.gameObject);
    
    private void OnTriggerEnter(Collider other) => TryExecuteRamming(other.gameObject);

    private void TryExecuteRamming(GameObject target)
    {
        if (target == this.gameObject) return;

        // 檢查是否在冷卻或未武裝
        if (!isArmed || _isOnCooldown || _currentInstantSpeed < minImpactSpeed || damageDealer == null)
        {
            return;
        }

        // ==========================================
        // 1. 檢查是否屬於「互動目標 (Interaction Layer)」
        // ==========================================
        if ((interactionLayer.value & (1 << target.layer)) != 0)
        {
            Debug.Log($"<color=cyan>[武器] 撞擊 Interaction Layer 目標: {target.name}！觸發專屬事件。</color>");
            
            OnInteractionHit?.Invoke(target); 
            StartCoroutine(CooldownRoutine());
            return; // 互動目標絕對不扣血，直接結束
        }

        // ==========================================
        // 2. 檢查是否屬於「傷害目標 (Damage Layer)」
        // ==========================================
        if ((damageLayer.value & (1 << target.layer)) != 0)
        {
            Debug.Log($"<color=green>[武器] 撞擊 Damage Layer 目標: {target.name}！準備造成傷害。</color>");
            
            var playerRuntime = GameManager.Instance.MainGameMediator.CurrentPlayerRuntime;
            if (playerRuntime != null)
            {
                damageDealer.InjectSnapshot(playerRuntime.TotalAttackPower, playerRuntime.TotalCritRate, playerRuntime.TotalCritMultiplier);
            }

            damageDealer.DealDamageTo(target);
            OnRammingSuccess?.Invoke(target); // 廣播撞擊成功
        
            StartCoroutine(CooldownRoutine());
            return;
        }

        // 如果兩個 Layer 都沒中，就不做任何事 (例如撞到牆壁或地板)
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