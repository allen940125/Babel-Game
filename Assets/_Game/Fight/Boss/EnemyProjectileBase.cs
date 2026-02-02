using UnityEngine;

public abstract class EnemyProjectileBase : MonoBehaviour
{
    // 發射器會呼叫這個方法，傳入方向和速度
    // 具體怎麼動，由子類別自己實作
    public abstract void Initialize(Vector2 direction, float speed);
}