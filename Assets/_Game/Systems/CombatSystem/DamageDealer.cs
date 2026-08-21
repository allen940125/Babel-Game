using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    [Header("★ 動態數據來源 (選填)")]
    [Tooltip("若綁定 SO，則優先使用 SO 內的攻擊力與暴擊率結算。適用於玩家武器或 Boss 攻擊。")]
    [SerializeField] private EntityRuntime sourceEntityData;

    [Header("★ 靜態固定數值 (當 SO 為空時生效)")]
    [Tooltip("若無綁定 SO，則使用此固定數值。適用於一般敵人子彈、環境陷阱。")]
    [SerializeField] private int flatDamage = 15;
    [SerializeField] private bool canCrit = false; // 靜態傷害是否允許暴擊

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
        // 模式 A：動態 SO 結算 (玩家、精英怪)
        if (sourceEntityData != null)
        {
            bool isCrit = Random.value <= sourceEntityData.TotalCritRate;
            int finalRawDamage = isCrit ? Mathf.RoundToInt(sourceEntityData.TotalAttackPower * sourceEntityData.TotalCritMultiplier) : sourceEntityData.TotalAttackPower;

            return new DamagePayload()
            {
                Damage = finalRawDamage,
                IsCrit = isCrit,
                Source = this.gameObject
            };
        }
        
        // 模式 B：靜態數值結算 (普通子彈、陷阱)
        return new DamagePayload()
        {
            Damage = flatDamage,
            IsCrit = canCrit, 
            Source = this.gameObject
        };
    }
}