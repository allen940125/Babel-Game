using UnityEngine;

[RequireComponent(typeof(DamageDealer))]
public abstract class EnemyProjectileBase : MonoBehaviour
{
    protected DamageDealer damageDealer;
    protected BossStateMachine ownerBoss;

    protected virtual void Awake()
    {
        damageDealer = GetComponent<DamageDealer>();
    }

    // ★ 擴充介面：強制要求外部傳入 damageMultiplier (可設預設值 1.0f 防呆)
    public virtual void Initialize(Vector3 direction, float speed, BossStateMachine boss, float damageMultiplier = 1f)
    {
        ownerBoss = boss;
        if (boss != null)
        {
            EntityCore core = boss.GetComponent<EntityCore>();
            if (core != null && core.RuntimeData != null)
            {
                // 1. 在子彈生成的瞬間 (Snapshot)，抽出 Boss 當下的面板屬性
                float snapshotAttack = core.RuntimeData.TotalAttackPower * damageMultiplier;
                float snapshotCritRate = core.RuntimeData.TotalCritRate;
                float snapshotCritMult = core.RuntimeData.TotalCritMultiplier;

                // 2. 將純數字注入 DamageDealer，徹底切斷子彈與 Boss 大腦的後續關聯
                damageDealer.InjectSnapshot(snapshotAttack, snapshotCritRate, snapshotCritMult);
            }
            else
            {
                Debug.LogError($"[架構錯誤] {boss.name} 缺少 EntityCore 或 RuntimeData。");
            }
        }
    }
}