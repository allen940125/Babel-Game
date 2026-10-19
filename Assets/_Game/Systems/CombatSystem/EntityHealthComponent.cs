using System; 
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class EntityHealthComponent : MonoBehaviour, IEntityRuntimeDependent, IDamageable, IHealable
{
    [Header("★ 嚴格除錯模式 (Debug)")]
    [Tooltip("開啟後，會詳細印出受擊與扣血判定的每一環節")]
    [SerializeField] private bool enableDebug = false;

    [Header("★ 無敵幀設定")]
    [SerializeField] private float invincibilityDuration = 1.5f;

    public UnityEvent onTakeDamageVisuals;
    
    public event Action OnDeath;
    public event Action<int> OnDamageTaken;

    private EntityRuntime _entityData;

    // ==========================================
    // ★ 實作介面：大腦推播資料時，立刻更新參考並註冊錨點
    // ==========================================
    public void OnRuntimeDataChanged(EntityRuntime newData)
    {
        _entityData = newData;

        if (_entityData != null && _entityData.TryGetTrait(out RuntimeAnchorTrait anchor))
        {
            anchor.RegisterEntity(this.gameObject);
        }
    }

    // ==========================================
    // ★ 核心傷害結算與 Debug 報表
    // ==========================================
    public void TakeDamage(DamagePayload payload)
    {
        // 1. 基礎防呆檢查 (移除了錯誤的無敵攔截，把它放到後面處理)
        if (_entityData == null || _entityData.CurrentHealth <= 0) 
        {
            if (enableDebug) Debug.LogWarning($"[Health-無效受擊] {gameObject.name} 已死亡或無資料，略過傷害。");
            return;
        }
        if (payload.Damage < 0) return;
        
        // 2. 處理無敵幀與穿透邏輯
        if (_entityData.HasState(EntityStateFlags.Invincible))
        {
            if (!payload.IgnoreInvincibility)
            {
                if (enableDebug) Debug.Log($"<color=gray>[Health-無敵抵擋]</color> {gameObject.name} 處於無敵狀態，成功擋下 {payload.Damage} 點傷害。");
                return; 
            }
            else
            {
                if (enableDebug) Debug.Log($"<color=magenta>[Health-穿透無敵]</color> 警告！受到無視無敵的傷害 (如毒氣/岩漿)！");
            }
        }
        
        // 3. 數學結算
        int rawDamage = payload.Damage;
        int defense = _entityData.TotalDefense;
        int finalDamage = Mathf.Max(1, rawDamage - defense); // 保底至少扣 1 滴血
        int healthBefore = _entityData.CurrentHealth;
        
        // ★ 4. 印出詳細傷害報表 (僅在 Debug 模式開啟時)
        if (enableDebug)
        {
            string sourceName = payload.Source != null ? payload.Source.name : "未知來源";
            string critTag = payload.IsCrit ? " <color=yellow><b>(爆擊!)</b></color>" : "";
            
            Debug.Log($"<color=red><b>[Health-傷害結算報表]</b></color> 承受來自 <b>{sourceName}</b> 的攻擊{critTag}\n" +
                      $" ├─ 傳入傷害：{rawDamage}\n" +
                      $" ├─ 實體防禦：{defense}\n" +
                      $" ├─ 減傷公式：Max(1, {rawDamage} - {defense})\n" +
                      $" ├─ 實際扣血：<b><color=red>{finalDamage}</color></b>\n" +
                      $" └─ 血量變化：{healthBefore} ➔ {Mathf.Max(0, healthBefore - finalDamage)} / {_entityData.MaxHealth}");
        }

        // 5. 實際套用扣血 (大腦內部會自己去觸發 OnHealthRatioChanged 廣播)
        _entityData.ModifyHealth(-finalDamage);
        
        onTakeDamageVisuals?.Invoke();
        OnDamageTaken?.Invoke(finalDamage);

        // 6. 死亡判定
        if (_entityData.CurrentHealth <= 0)
        {
            if (enableDebug) Debug.Log($"<color=black><b>[Health-實體死亡]</b></color> {gameObject.name} 血量歸零！觸發 OnDeath。");
            OnDeath?.Invoke();
            return; 
        }

        // 7. 賦予新的無敵時間 (如果是持續傷害就不會給無敵幀)
        if (!payload.IgnoreInvincibility && invincibilityDuration > 0)
        {
            StartCoroutine(InvincibilityRoutine());
        }
    }

    private IEnumerator InvincibilityRoutine()
    {
        _entityData.AddState(EntityStateFlags.Invincible);
        yield return new WaitForSeconds(invincibilityDuration);
        _entityData.RemoveState(EntityStateFlags.Invincible);
    }

    public void ReceiveHeal(HealPayload payload)
    {
        if (_entityData == null || _entityData.CurrentHealth <= 0) return;
        
        int healthBefore = _entityData.CurrentHealth;
        _entityData.ModifyHealth(payload.HealAmount);
        
        if (enableDebug)
        {
            string sourceName = payload.Source != null ? payload.Source.name : "未知來源";
            Debug.Log($"<color=green><b>[Health-治療結算]</b></color> 接受來自 <b>{sourceName}</b> 的治療\n" +
                      $" ├─ 恢復量：{payload.HealAmount}\n" +
                      $" └─ 血量變化：{healthBefore} ➔ {_entityData.CurrentHealth} / {_entityData.MaxHealth}");
        }
    }
}