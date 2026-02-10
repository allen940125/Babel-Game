using UnityEngine;

public abstract class EnemyProjectileBase : MonoBehaviour
{
    [Header("基礎傷害設定")]
    [Tooltip("這個攻擊造成的傷害值")]
    public int damageAmount = 1;

    // 抽象方法：初始化 (給子類別實作)
    public abstract void Initialize(Vector2 direction, float speed);

    // ★ 新增：統一的傷害處理方法
    // 子類別只要呼叫這個方法，把撞到的 Collider 傳進來就好
    protected void TryDealDamage(Collider2D hitCollider)
    {
        // 1. 找玩家元件
        PlayerController2D player = hitCollider.GetComponent<PlayerController2D>();
        
        // 2. 如果沒找到，往父物件找 (因為有時候 Collider 在子物件上)
        if (player == null) 
        {
            player = hitCollider.GetComponentInParent<PlayerController2D>();
        }

        // 3. 真的找到了，就造成傷害
        if (player != null)
        {
            player.TakeDamage(damageAmount);
            
            // ★ 如果以後要加事件，只要改這裡就好！
            // GameManager.Instance.Event.Send(new PlayerTakeDamageEvent(...));
        }
    }
}