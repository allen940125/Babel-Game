using UnityEngine;

// 專門處理生命值邏輯的組件，掛載在需要血量的實體上
public class EntityHealthComponent : MonoBehaviour, IDamageable, IHealable
{
    [Header("★ 資料來源綁定")]
    [SerializeField] private BaseEntityRuntimeSO entityData;
    [SerializeField] private bool showDebugLogs = true;

    private void Awake()
    {
        if (entityData == null) Debug.LogError($"[致命錯誤] {gameObject.name} 未綁定 BaseEntityRuntimeSO！");

        // ==========================================
        // ★ 核心解法：安全轉型 (Pattern Matching)
        // ==========================================
        // 因為 entityData 的宣告型別是基底類別 BaseEntityRuntimeSO，
        // 我們在這裡檢查：「目前拖進來的這個 SO，它在記憶體中真正的身分是不是 PlayerRuntimeSO？」
        // 如果是，就將它視為 playerSO 並執行專屬的註冊；如果不是 (例如是敵人的 SO)，這段就會直接跳過。
        if (entityData is PlayerRuntimeSO playerSO)
        {
            playerSO.RegisterPlayer(this.gameObject);
            if (showDebugLogs) Debug.Log($"<color=cyan>[系統] 已將 {gameObject.name} 註冊為目前操作的玩家實體！</color>");
        }
    }

    // ==========================================
    // 實作 IDamageable：專心處理純粹的傷害
    // ==========================================
    public void TakeDamage(DamagePayload payload)
    {
        if (entityData == null || entityData.CurrentHealth <= 0) return;
        
        // 嚴格排除負數傷害的髒資料傳入
        if (payload.Damage < 0)
        {
            Debug.LogError($"[邏輯錯誤] 傳入的傷害值為負數 ({payload.Damage})，請使用 ReceiveHeal 進行治療！");
            return;
        }

        // 結算防禦
        int finalDamage = Mathf.Max(1, payload.Damage - entityData.Defense);
        
        // 呼叫 SO 扣血 (明確傳入負數代表減少生命)
        entityData.ModifyHealth(-finalDamage);

        if (showDebugLogs)
        {
            Debug.Log($"<color=red>[受到傷害] {gameObject.name} 承受:{finalDamage} | 剩餘血量:{entityData.CurrentHealth}/{entityData.MaxHealth}</color>");
        }
    }

    // ==========================================
    // 實作 IHealable：專心處理純粹的治療
    // ==========================================
    public void ReceiveHeal(HealPayload payload)
    {
        if (entityData == null || entityData.CurrentHealth <= 0) return;

        // 處理戰鬥回血鎖
        if (entityData.HasHealedInThisFight)
        {
            if (showDebugLogs) Debug.LogWarning($"<color=orange>[治療攔截] {gameObject.name} 本次戰鬥已回血過！</color>");
            return;
        }

        entityData.LockHealing();

        // 呼叫 SO 加血 (明確傳入正數代表增加生命)
        entityData.ModifyHealth(payload.HealAmount);

        if (showDebugLogs)
        {
            Debug.Log($"<color=green>[受到治療] {gameObject.name} 恢復:{payload.HealAmount} | 剩餘血量:{entityData.CurrentHealth}/{entityData.MaxHealth}</color>");
        }
    }
}