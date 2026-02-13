using UnityEngine;

public class EnemyAttackObject : MonoBehaviour
{
    [Header("基礎傷害設定 (Base)")]
    public int damageAmount = 1;

    // ★ 統一的傷害處理邏輯
    // 所有的子類別 (子彈、導彈、爆炸) 都只要呼叫這個方法就好
    protected void TryDealDamage(Collider2D hitCollider)
    {
        // 建議統一用 Component 來判定身分，比 Tag 更穩
        // 先找自己，再找父物件 (應對碰撞器在子物件的情況)
        PlayerController2D player = hitCollider.GetComponent<PlayerController2D>();
        if (player == null) player = hitCollider.GetComponentInParent<PlayerController2D>();

        if (player != null)
        {
            player.TakeDamage(damageAmount);
            // Debug.Log($"{name} 造成了傷害！");
        }
    }
}