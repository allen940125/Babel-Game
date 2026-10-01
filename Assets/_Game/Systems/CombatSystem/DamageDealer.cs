using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    [Header("★ 傷害穿透設定")]
    [Tooltip("勾選後，此傷害將無視實體的無敵幀，且不會觸發新的無敵時間 (適用於毒氣、岩漿等 DoT)")]
    [SerializeField] private bool _snapshottedIgnoreInvincibility = false;
    
    [Header("★ 靜態固定數值 (未注入時的預設值)")]
    [SerializeField] private float _snapshottedDamage = 15f;
    [SerializeField] private float _snapshottedCritRate = 0f;
    [SerializeField] private float _snapshottedCritMultiplier = 1.5f;

    private bool _hasBeenInjected = false;

    // ==========================================
    // ★ 核心：完全移除對 EntityRuntime 的依賴
    // 外部生成子彈的系統，必須自己算出數字並塞進來
    // ==========================================
    public void InjectSnapshot(float baseDamage, float critRate, float critMultiplier, bool ignoreInvincibility = false)
    {
        _snapshottedDamage = baseDamage;
        _snapshottedCritRate = critRate;
        _snapshottedCritMultiplier = critMultiplier;
        _snapshottedIgnoreInvincibility = ignoreInvincibility;
    }
    public void DealDamageTo(GameObject target)
    {
        if (target == null) return;

        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable == null) damageable = target.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(ConstructPayload());
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
            IgnoreInvincibility = _snapshottedIgnoreInvincibility // ★ 從快照中讀取
        };
    }
}