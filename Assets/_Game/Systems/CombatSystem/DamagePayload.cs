using UnityEngine;

// 1. 定義明確的介面
public interface IDamageable
{
    void TakeDamage(DamagePayload payload);
}

public interface IHealable
{
    void ReceiveHeal(HealPayload payload);
}

// 2. 定義純粹的封包
public struct DamagePayload
{
    public int Damage;
    public bool IsCrit;
    public GameObject Source;
}

public struct HealPayload
{
    public int HealAmount;
    public GameObject Source;
    // 未來可以擴充：public bool IsOverHealAllowed; 等
}