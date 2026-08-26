using UnityEngine;

// 1. 直接繼承 MonoBehaviour，剝離繼承樹
[RequireComponent(typeof(DamageDealer))] // 強制掛載傷害發送器
public abstract class EnemyProjectileBase : MonoBehaviour
{
    protected DamageDealer damageDealer;

    protected virtual void Awake()
    {
        damageDealer = GetComponent<DamageDealer>();
    }

    public abstract void Initialize(Vector3 direction, float speed, BossStateMachine ownerBoss);
}