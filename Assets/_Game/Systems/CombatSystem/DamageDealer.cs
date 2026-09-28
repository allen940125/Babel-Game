using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    private EntityRuntime _sourceEntityData;
    
    // 增加一個內部變數來儲存倍率，預設為 1
    [SerializeField] private float _currentMultiplier = 1.0f;

    [Header("★ 靜態固定數值 (當 SO 為空時生效)")]
    [SerializeField] private int flatDamage = 15;
    [SerializeField] private bool canCrit = false;

    // ==========================================
    // ★ 核心：開放給外部注入資料的接口 (擴充倍率參數)
    // ==========================================
    public void BindSourceData(EntityRuntime runtime, float multiplier = 1.0f)
    {
        _sourceEntityData = runtime;
        _currentMultiplier = multiplier;
        Debug.Log($"<color=cyan>[DamageDealer] 已成功綁定資料來源！倍率設定為: {_currentMultiplier}</color>");
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
        if (_sourceEntityData != null)
        {
            // 將倍率加入計算公式
            bool isCrit = Random.value <= _sourceEntityData.TotalCritRate;
            
            // 計算基礎傷害 (攻擊力 * 子彈倍率)
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
        
        // 如果沒有實體資料，則使用 flatDamage 乘上倍率
        return new DamagePayload()
        {
            Damage = Mathf.RoundToInt(flatDamage * _currentMultiplier),
            IsCrit = canCrit, 
            Source = this.gameObject
        };
    }
}