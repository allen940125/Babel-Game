using UnityEngine;

// ★ 1. 傷害封包：所有戰鬥數據的通訊載體，未來想加擊退力道 (Knockback)、元素屬性，都加在這裡！
public struct DamagePayload
{
    public int Damage;          // 最終實質傷害
    public bool IsCrit;         // 是否暴擊
    public GameObject Source;   // 傷害來源 (誰打的，方便反傷或除錯)
}

// ★ 2. 受擊介面：任何生物、Boss 或物件，只要能受傷，就必須遵守這個合約
public interface IDamageable
{
    void TakeDamage(DamagePayload payload);
}