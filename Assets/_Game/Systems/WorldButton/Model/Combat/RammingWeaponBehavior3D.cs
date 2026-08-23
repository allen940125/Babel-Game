using System.Collections;
using Gamemanager;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
// ★ 強制要求這個武器必須掛載 DamageDealer (傷害發送器)
[RequireComponent(typeof(DamageDealer))] 
public class RammingWeaponBehavior3D : MonoBehaviour
{
    [Header("★ 物理撞擊設定 (只管物理，不管數值！)")]
    [Tooltip("滑鼠拖曳甩動的最小速度閾值")]
    [SerializeField] private float minImpactSpeed = 5.0f;
    [SerializeField] private float rammingCooldown = 0.8f;
    [SerializeField] private bool isArmed = false;

    // ★ 徹底替換：武器只需要 DamageDealer 來發送傷害，不需要知道 Health
    [SerializeField] private DamageDealer damageDealer;

    private Vector3 _lastPosition;
    private float _currentInstantSpeed;
    private bool _isOnCooldown = false;
    private GlowBehavior3D _glow;

    private void Awake()
    {
        // 1. 自動抓取本物件上的傷害發送器
        if (damageDealer == null) damageDealer = GetComponent<DamageDealer>();
        _glow = GetComponent<GlowBehavior3D>();
        _lastPosition = transform.position;

        // 2. 訂閱事件
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterAttackingPhaseEvent, OnBossAttacking);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterIdlePhaseEvent, OnBossIdle);
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] 找不到 GameManager.Instance.MainGameEvent，事件訂閱失敗！");
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

    private void OnCollisionEnter(Collision collision) => TryExecuteRamming(collision.gameObject);
    private void OnTriggerEnter(Collider other) => TryExecuteRamming(other.gameObject);

    private void TryExecuteRamming(GameObject target)
    {
        // ★ 物理過濾：不要打到自己，也不要打到沒有實體的東西
        if (target == this.gameObject) return;

        Debug.Log($"<color=white>[物理接觸] 撞擊到了: {target.name} | 當前甩動速度: {_currentInstantSpeed:.00} m/s</color>");

        // 檢查閘門 1：武裝狀態
        if (!isArmed)
        {
            Debug.LogWarning($"<color=gray>[攻擊攔截 1] {gameObject.name} 目前並未武裝！請檢查 Boss 是否已進入 Idle 階段！</color>");
            return;
        }

        // 檢查閘門 2：攻擊冷卻中
        if (_isOnCooldown)
        {
            Debug.LogWarning($"<color=gray>[攻擊攔截 2] {gameObject.name} 目前正在撞擊冷卻中！</color>");
            return;
        }

        // 檢查閘門 3：速度不夠快
        if (_currentInstantSpeed < minImpactSpeed)
        {
            Debug.LogWarning($"<color=orange>[攻擊攔截 3] 甩動速度不足！當前速度 ({_currentInstantSpeed:.00} m/s) < 最小閾值 ({minImpactSpeed} m/s)</color>");
            return;
        }

        // 檢查閘門 4：確保有傷害發送器
        if (damageDealer == null)
        {
            Debug.LogError($"<color=red>[致命錯誤 4] {gameObject.name} 找不到 DamageDealer 組件！</color>");
            return;
        }

        // --- 進入結算 ---
        Debug.Log($"<color=green>[條件全過！] 準備調用 damageDealer.DealDamageTo({target.name})！</color>");
        
        // ★ 核心執行：由 DamageDealer 把傷害封包砸在目標身上！
        damageDealer.DealDamageTo(target);
        
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        _isOnCooldown = true;
        if (_glow != null) _glow.enabled = false;
        yield return new WaitForSeconds(rammingCooldown);
        _isOnCooldown = false;
        if (isArmed && _glow != null) _glow.enabled = true;
    }

    private void OnBossAttacking(BossEnterAttackingPhaseEvent evt) 
    {
        SetArmedState(false);
    }

    private void OnBossIdle(BossEnterIdlePhaseEvent evt) 
    {
        SetArmedState(true);
    }

    public void SetArmedState(bool armed)
    {
        isArmed = armed;
        if (_glow != null) _glow.enabled = armed;
    }
}