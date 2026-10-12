using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    [Header("★ 嚴格除錯模式 (Debug)")]
    [Tooltip("開啟後，會詳細印出傷害注入與扣血判定的每一環節")]
    [SerializeField] private bool enableDebug = false;

    [Header("★ 傷害穿透設定")]
    [Tooltip("勾選後，此傷害將無視實體的無敵幀，且不會觸發新的無敵時間 (適用於毒氣、岩漿等 DoT)")]
    [SerializeField] private bool _snapshottedIgnoreInvincibility = false;
    
    [Header("★ 靜態固定數值 (未注入時的預設值)")]
    [SerializeField] private float _snapshottedDamage = 15f;
    [SerializeField] private float _snapshottedCritRate = 0f;
    [SerializeField] private float _snapshottedCritMultiplier = 1.5f;

    // ==========================================
    // ★ 核心：完全移除對 EntityRuntime 的依賴
    // ==========================================
    public void InjectSnapshot(float baseDamage, float critRate, float critMultiplier, bool ignoreInvincibility = false)
    {
        _snapshottedDamage = baseDamage;
        _snapshottedCritRate = critRate;
        _snapshottedCritMultiplier = critMultiplier;
        _snapshottedIgnoreInvincibility = ignoreInvincibility;

        if (enableDebug) 
        {
            Debug.Log($"<color=cyan>[DamageDealer-注入]</color> {gameObject.name} 接收到傷害快照: 基礎傷害={baseDamage}, 忽視無敵={ignoreInvincibility}");
        }
    }

    public void DealDamageTo(GameObject target)
    {
        if (target == null) 
        {
            if (enableDebug) Debug.LogWarning($"<color=orange>[DamageDealer-落空]</color> {gameObject.name} 試圖攻擊，但目標為 null！");
            return;
        }

        if (enableDebug) Debug.Log($"<color=yellow>[DamageDealer-鎖定]</color> {gameObject.name} 範圍掃描碰觸到 {target.name} (Tag: {target.tag})，開始尋找 IDamageable 介面...");

        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable == null) damageable = target.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            DamagePayload payload = ConstructPayload();
            if (enableDebug) Debug.Log($"<color=green>[DamageDealer-命中]</color> 成功在 {target.name} 身上找到 IDamageable！正式發送 {payload.Damage} 點傷害！");
            
            damageable.TakeDamage(payload);
        }
        else
        {
            if (enableDebug) Debug.LogError($"<color=red>[DamageDealer-無效目標]</color> 警告！{target.name} 身上及其父物件都沒有實作 IDamageable 介面，傷害已流失！");
        }
    }

    private DamagePayload ConstructPayload()
    {
        bool isCrit = Random.value <= _snapshottedCritRate;
        int finalRawDamage = isCrit 
            ? Mathf.RoundToInt(_snapshottedDamage * _snapshottedCritMultiplier) 
            : Mathf.RoundToInt(_snapshottedDamage);

        return new DamagePayload()
        {
            Damage = finalRawDamage,
            IsCrit = isCrit,
            Source = this.gameObject,
            IgnoreInvincibility = _snapshottedIgnoreInvincibility 
        };
    }
}