using System.Collections;
using Gamemanager;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RammingWeaponBehavior3D : MonoBehaviour
{
    [Header("★ 物理撞擊設定 (只管物理，不管數值！)")]
    [Tooltip("滑鼠拖曳甩動的最小速度閾值")]
    [SerializeField] private float minImpactSpeed = 5.0f;
    [SerializeField] private float rammingCooldown = 0.8f;
    [SerializeField] private bool isArmed = false;

    // ★ 綁定生物身上的通用戰鬥組件
    [SerializeField] private EntityCombatComponent combatComponent;

    private Vector3 _lastPosition;
    private float _currentInstantSpeed;
    private bool _isOnCooldown = false;
    private GlowBehavior3D _glow;

    private void Awake()
    {
        // 1. 自動抓取組件
        if (combatComponent == null) combatComponent = GetComponentInParent<EntityCombatComponent>();
        _glow = GetComponent<GlowBehavior3D>();
        _lastPosition = transform.position;

        // ★ 2. 致命錯誤修復：必須在 Awake 裡向 GameManager 註冊訂閱事件！否則底部的底層回調永遠不會被觸發！
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterAttackingPhaseEvent, OnBossAttacking);
            // ★ 嚴格替換：監聽 Idle 事件來開啟武裝！
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterIdlePhaseEvent, OnBossIdle);
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] 找不到 GameManager.Instance.MainGameEvent，事件訂閱失敗！");
        }
    }

    // ★ 3. 記憶體管理鐵律：有訂閱 (Subscribe) 就必須在物件銷毀時解除訂閱 (Unsubscribe)！
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
        Debug.Log($"<color=white>[物理接觸] 撞擊到了: {target.name} | 當前甩動速度: {_currentInstantSpeed:.00} m/s</color>");

        // 檢查閘門 1：武裝狀態
        if (!isArmed)
        {
            Debug.LogWarning($"<color=gray>[攻擊攔截 1] {gameObject.name} 目前並未武裝 (isArmed = false)！請檢查 Boss 是否已進入 Idle 階段！</color>");
            return;
        }

        // 檢查閘門 2：攻擊冷卻中
        if (_isOnCooldown)
        {
            Debug.LogWarning($"<color=gray>[攻擊攔截 2] {gameObject.name} 目前正在撞擊冷卻中 (_isOnCooldown = true)！</color>");
            return;
        }

        // 檢查閘門 3：速度不夠快
        if (_currentInstantSpeed < minImpactSpeed)
        {
            Debug.LogWarning($"<color=orange>[攻擊攔截 3] 甩動速度不足！當前速度 ({_currentInstantSpeed:.00} m/s) < 最小閾值 ({minImpactSpeed} m/s)</color>");
            return;
        }

        // 檢查閘門 4：是否有綁定戰鬥組件
        if (combatComponent == null)
        {
            Debug.LogError($"<color=red>[致命錯誤 4] {gameObject.name} 找不到 EntityCombatComponent！請檢查 Inspector 的欄位綁定！</color>");
            return;
        }

        // --- 進入 DealDamageTo 前的最後確認 ---
        Debug.Log($"<color=green>[條件全過！] 準備調用 combatComponent.DealDamageTo({target.name})！</color>");
        combatComponent.DealDamageTo(target);
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

    // ==========================================
    // ★ 核心修復：嚴格對應新的快節奏戰鬥階段
    // ==========================================
    private void OnBossAttacking(BossEnterAttackingPhaseEvent evt) 
    {
        Debug.Log($"[{gameObject.name}] 收到 Boss 攻擊廣播 -> 卸除武裝 (isArmed = false)");
        SetArmedState(false);
    }

    // ★ 徹底廢除舊版的 OnBossVulnerable，改為 OnBossIdle！
    private void OnBossIdle(BossEnterIdlePhaseEvent evt) 
    {
        Debug.Log($"<color=cyan>[{gameObject.name}] 收到 Boss 休眠廣播 -> 開啟重擊武裝！ (isArmed = true)</color>");
        SetArmedState(true);
    }

    public void SetArmedState(bool armed)
    {
        isArmed = armed;
        if (_glow != null) _glow.enabled = armed;
    }
}