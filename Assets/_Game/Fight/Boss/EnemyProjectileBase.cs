using UnityEngine;

// 繼承 EnemyAttackObject，所以它也有 TryDealDamage 功能
public abstract class EnemyProjectileBase : EnemyAttackObject
{
    // 強制子類別必須實作初始化 (因為你是投射物，你必須要會飛)
    public abstract void Initialize(Vector2 direction, float speed);
}