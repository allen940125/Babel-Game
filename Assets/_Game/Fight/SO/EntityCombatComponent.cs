using UnityEngine;

public class EntityCombatComponent : MonoBehaviour, IDamageable
{
    [Header("★ 資料來源綁定")]
    [SerializeField] private BaseEntityRuntimeSO entityData;
    [SerializeField] private bool showDebugLogs = true;

    private void Awake()
    {
        if (entityData == null) Debug.LogError($"[戰鬥致命錯誤] {gameObject.name} 未綁定 BaseEntityRuntimeSO！");
    }

    // ==========================================
    // 1. 主動發起：通用數值異動 (支援正數傷害 / 負數治療)
    // ==========================================
    public void DealDamageTo(GameObject target, int customValue = 0)
    {
        if (target == null || entityData == null) return;

        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable == null) damageable = target.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            // 如果外部沒有傳入自訂數值，就使用 SO 原本的攻擊力；否則使用外部指定數值(支援負數回血)
            int baseValue = (customValue != 0) ? customValue : entityData.AttackPower;

            // 只有「傷害 (正數)」才計算暴擊；「治療 (負數)」絕不暴擊！
            bool isCrit = (baseValue > 0) && (Random.value <= entityData.CritRate);
            int rawValue = isCrit ? Mathf.RoundToInt(baseValue * entityData.CritMultiplier) : baseValue;

            DamagePayload payload = new DamagePayload()
            {
                Damage = rawValue,
                IsCrit = isCrit,
                Source = this.gameObject
            };

            if (showDebugLogs)
            {
                string actionType = rawValue > 0 ? "攻擊" : "治療";
                Debug.Log($"<color=cyan>[{actionType}發動] {gameObject.name} -> {target.name} | 封包數值: {rawValue}</color>");
            }

            damageable.TakeDamage(payload);
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning($"[失效] 撞擊到了 {target.name}，但其身上與父物件皆無 IDamageable 介面！");
        }
    }

    // 相容舊版無參數調用
    public void DealDamageTo(GameObject target) => DealDamageTo(target, 0);

    // ==========================================
    // 2. 被動受擊/受療：雙流向數值仲裁
    // ==========================================
    public void TakeDamage(DamagePayload payload)
    {
        if (entityData == null || entityData.CurrentHealth <= 0) return;

        int finalValue = 0;

        // ★ 路由分流 A：負數 = 神聖治療 (無視防禦)
        if (payload.Damage < 0)
        {
            // 嚴格閘門：查核 SO 裡的單次回血鎖！
            if (entityData.HasHealedInThisFight)
            {
                Debug.LogWarning($"<color=orange>[治療攔截] {gameObject.name} 在本次戰鬥已回血過 (HasHealedInThisFight=true)，拒絕二次治療！</color>");
                return;
            }

            finalValue = payload.Damage; // 保持負數 (例如 -20)
            entityData.HasHealedInThisFight = true; // ★ 寫入 SO 上鎖！整場戰鬥只能用一次！

            if (showDebugLogs)
            {
                Debug.Log($"<color=green>[受到治療] {gameObject.name} 恢復 {Mathf.Abs(finalValue)} 點生命 | 剩餘血量:{entityData.CurrentHealth - finalValue}/{entityData.MaxHealth} | 戰鬥回血鎖已啟動！</color>");
            }
        }
        // ★ 路由分流 B：正數 = 物理傷害 (計算防禦)
        else
        {
            finalValue = Mathf.Max(1, payload.Damage - entityData.Defense);

            if (showDebugLogs)
            {
                Debug.Log($"<color=red>[受到傷害] {gameObject.name} 遭受攻擊 | 承受傷害:{finalValue} | 剩餘血量:{entityData.CurrentHealth - finalValue}/{entityData.MaxHealth}</color>");
            }
        }

        // 修改血量 (傳入負數減去負數 = 加上正數回血！)
        entityData.ModifyHealth(-finalValue);
        OnEntityTakeDamage(payload, finalValue);
    }

    protected virtual void OnEntityTakeDamage(DamagePayload payload, int finalDamage) { }
}