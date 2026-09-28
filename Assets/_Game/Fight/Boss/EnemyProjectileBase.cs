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
                // ★ 將外部傳入的倍率，灌給 DamageDealer
                damageDealer.BindSourceData(core.RuntimeData, damageMultiplier);
            }
            else
            {
                Debug.LogError($"[架構錯誤] {boss.name} 缺少 EntityCore 或 RuntimeData。");
            }
        }
    }
}