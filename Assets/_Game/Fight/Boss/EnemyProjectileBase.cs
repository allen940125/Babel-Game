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

    // ★ 統一介面：所有投射物都接收 direction 與 speed
    public virtual void Initialize(Vector3 direction, float speed, BossStateMachine boss)
    {
        ownerBoss = boss;
        if (boss != null)
        {
            EntityCore core = boss.GetComponent<EntityCore>();
            if (core != null && core.RuntimeData != null)
            {
                damageDealer.BindSourceData(core.RuntimeData);
            }
            else
            {
                Debug.LogError($"[架構錯誤] {boss.name} 缺少 EntityCore 或 RuntimeData。");
            }
        }
    }
}