using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    private EntityRuntime _sourceEntityData;
    
    [SerializeField] private float _currentMultiplier = 1.0f;

    [Header("★ 傷害計算模式")]
    [Tooltip("打勾：使用 Boss的攻擊力與爆擊率。\n取消打勾：無視 Boss 數值，使用下方的 固定傷害與固定爆擊設定。")]
    [SerializeField] private bool useAttackerStats = true;

    [Header("★ 靜態固定數值 (當不使用攻擊者數值時生效)")]
    [Tooltip("基礎傷害 (依然會乘上子彈與發射器的倍率)")]
    [SerializeField] private int flatDamage = 15;
    [SerializeField] private bool canCrit = false;

    // ==========================================
    // ★ 核心：開放給外部注入資料的接口
    // ==========================================
    public void BindSourceData(EntityRuntime runtime, float multiplier = 1.0f)
    {
        _sourceEntityData = runtime;
        _currentMultiplier = multiplier;
    }

    public void DealDamageTo(GameObject target)
    {
        if (target == null) return;

        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable == null) damageable = target.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            DamagePayload payload = ConstructPayload();
            damageable.TakeDamage(payload);
        }
    }

    private DamagePayload ConstructPayload()
    {
        // 模式 A：使用 Boss 的素質 (且 Boss 資料存在)
        if (useAttackerStats && _sourceEntityData != null)
        {
            bool isCrit = Random.value <= _sourceEntityData.TotalCritRate;
            
            // 計算基礎傷害 (攻擊力 * 總倍率)
            float baseCalculatedDamage = _sourceEntityData.TotalAttackPower * _currentMultiplier;
            
            // 計算爆擊傷害
            int finalRawDamage = isCrit 
                ? Mathf.RoundToInt(baseCalculatedDamage * _sourceEntityData.TotalCritMultiplier) 
                : Mathf.RoundToInt(baseCalculatedDamage);

            return new DamagePayload()
            {
                Damage = finalRawDamage,
                IsCrit = isCrit,
                Source = this.gameObject
            };
        }
        
        // 模式 B：不使用 Boss 素質，或 Boss 資料遺失時，使用 FlatDamage
        // 將你手動設定的 flatDamage 乘上外部傳入的倍率
        float staticBaseDamage = flatDamage * _currentMultiplier;
        
        return new DamagePayload()
        {
            Damage = Mathf.RoundToInt(staticBaseDamage),
            IsCrit = canCrit, 
            Source = this.gameObject
        };
    }
}