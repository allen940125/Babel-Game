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
    // 1. 主動攻擊：支援 3D 子物件向上追溯
    // ==========================================
    public void DealDamageTo(GameObject target)
    {
        if (target == null || entityData == null) return;

        // ★ 嚴格修正：必須使用 GetComponentInParent！
        // 因為 3D 遊戲的 Collider 通常在骨架或 Hitbox 子物件上，Root 才有 IDamageable！
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable == null) damageable = target.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            bool isCrit = Random.value <= entityData.CritRate;
            int rawDamage = isCrit ? Mathf.RoundToInt(entityData.AttackPower * entityData.CritMultiplier) : entityData.AttackPower;

            DamagePayload payload = new DamagePayload()
            {
                Damage = rawDamage,
                IsCrit = isCrit,
                Source = this.gameObject
            };

            if (showDebugLogs)
            {
                Debug.Log($"<color=cyan>[攻擊發動] {gameObject.name} 命中 {target.name}！發射傷害封包 | 傷害:{rawDamage}</color>");
            }

            damageable.TakeDamage(payload);
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning($"[攻擊無效] 撞擊到了 {target.name}，但其身上與父物件皆無 IDamageable 介面！");
        }
    }

    // ==========================================
    // 2. 被動受擊 (保持原本邏輯)
    // ==========================================
    public void TakeDamage(DamagePayload payload)
    {
        if (entityData == null || entityData.CurrentHealth <= 0) return;
        int finalDamage = Mathf.Max(1, payload.Damage - entityData.Defense);
        entityData.ModifyHealth(-finalDamage);

        if (showDebugLogs)
        {
            Debug.Log($"<color=red>[受到傷害] {gameObject.name} 遭受來自 {payload.Source.name} 的攻擊 | 承受傷害:{finalDamage} | 剩餘血量:{entityData.CurrentHealth}/{entityData.MaxHealth}</color>");
        }
        OnEntityTakeDamage(payload, finalDamage);
    }

    protected virtual void OnEntityTakeDamage(DamagePayload payload, int finalDamage) { }
}