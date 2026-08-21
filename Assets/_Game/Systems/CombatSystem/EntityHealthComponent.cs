using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// 專門處理生命值邏輯的組件，掛載在需要血量的實體上
public class EntityHealthComponent : MonoBehaviour, IDamageable, IHealable
{
    [Header("★ 資料來源綁定")]
    [SerializeField] private EntityRuntime entityData;

    [Header("★ 無敵時間設定")]
    [Tooltip("受傷後的無敵時間。設為 0 代表沒有無敵時間 (如：木箱、小怪)")]
    [SerializeField] private float invincibilityDuration = 1.5f;
    private bool _isInvincible = false; // 內部無敵狀態鎖

    [Header("★ 視覺表現廣播 (解耦)")]
    [Tooltip("當受傷時，呼叫這裡的事件，讓視覺腳本去處理變色")]
    public UnityEvent onTakeDamageVisuals;
    
    [SerializeField] private bool showDebugLogs = true;

    private void Awake()
    {
        if (entityData == null) Debug.LogError($"[致命錯誤] {gameObject.name} 未綁定 BaseEntityRuntimeSO！");
        
        // ==========================================
        // ★ 核心解法：特徵索取 (Trait-Based Pattern Matching)
        // ==========================================
        // 拋棄「判斷它是不是玩家SO」的舊思維。
        // 現在改為：「向 SO 索取 RuntimeAnchorTrait，如果有拿到，代表這個實體是需要被外部指令控制的目標 (例如玩家)。」
        if (entityData.TryGetTrait(out RuntimeAnchorTrait anchor))
        {
            // 將自己身上的受擊與受療介面，註冊進這個錨點特徵中
            anchor.RegisterEntity(this.gameObject);
            
            if (showDebugLogs) 
            {
                Debug.Log($"<color=cyan>[系統] 已將 {gameObject.name} 註冊為外部指令錨點目標！</color>");
            }
        }
    }

    // ==========================================
    // 實作 IDamageable：專心處理純粹的傷害
    // ==========================================
    public void TakeDamage(DamagePayload payload)
    {
        // 1. 死亡或無敵狀態下，直接擋掉傷害！
        if (entityData == null || entityData.CurrentHealth <= 0 || _isInvincible) return;
        
        if (payload.Damage < 0) return; // 防呆

        // 2. 結算扣血
        int finalDamage = Mathf.Max(1, payload.Damage - entityData.TotalDefense);
        entityData.ModifyHealth(-finalDamage);

        // 3. 廣播受傷事件 (讓另外掛載的視覺腳本去發光發亮)
        onTakeDamageVisuals?.Invoke();

        // 4. 如果這個實體有設定無敵時間，且還沒死，就開啟無敵協程
        if (invincibilityDuration > 0 && entityData.CurrentHealth > 0)
        {
            StartCoroutine(InvincibilityRoutine());
        }
    }

    private IEnumerator InvincibilityRoutine()
    {
        _isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        _isInvincible = false;
    }

    // ==========================================
    // 實作 IHealable：專心處理純粹的治療
    // ==========================================
    public void ReceiveHeal(HealPayload payload)
    {
        if (entityData == null || entityData.CurrentHealth <= 0) return;

        // 呼叫 SO 加血 (明確傳入正數代表增加生命)
        entityData.ModifyHealth(payload.HealAmount);

        if (showDebugLogs)
        {
            Debug.Log($"<color=green>[受到治療] {gameObject.name} 恢復:{payload.HealAmount} | 剩餘血量:{entityData.CurrentHealth}/{entityData.MaxHealth}</color>");
        }
    }
}