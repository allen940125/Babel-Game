using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    // ★ 拔除 SerializeField，改為私有變數
    private EntityRuntime _sourceEntityData;

    [Header("★ 靜態固定數值 (當 SO 為空時生效)")]
    [SerializeField] private int flatDamage = 15;
    [SerializeField] private bool canCrit = false;

    // ==========================================
    // ★ 核心：開放給外部注入資料的接口
    // ==========================================
    public void BindSourceData(EntityRuntime runtime)
    {
        _sourceEntityData = runtime;
        Debug.Log($"<color=cyan>[DamageDealer] 已成功綁定資料來源！</color>");
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
            bool isCrit = Random.value <= _sourceEntityData.TotalCritRate;
            int finalRawDamage = isCrit ? Mathf.RoundToInt(_sourceEntityData.TotalAttackPower * _sourceEntityData.TotalCritMultiplier) : _sourceEntityData.TotalAttackPower;

            return new DamagePayload()
            {
                Damage = finalRawDamage,
                IsCrit = isCrit,
                Source = this.gameObject
            };
        }
        
        return new DamagePayload()
        {
            Damage = flatDamage,
            IsCrit = canCrit, 
            Source = this.gameObject
        };
    }
}