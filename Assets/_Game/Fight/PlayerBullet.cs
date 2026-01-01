using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerBullet : MonoBehaviour
{
    public float lifeTime = 3.0f; 
    public int damage = 1;

    private void Start()
    {
        GetComponent<Rigidbody2D>().gravityScale = 0;
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 處理牆壁 (保持原本邏輯)
        if (other.CompareTag("Wall"))
        {
            //Destroy(gameObject);
            return; // 撞牆就結束了，不用往下判斷
        }

        // 2. 處理 Boss (重點修改)
        // 檢查 Tag 可以保留，這是一個過濾的好習慣
        if (other.CompareTag("Boss")) 
        {
            // --- 修改重點 ---
            // 改用 GetComponentInParent，這樣就算 Collider 在子物件，也能找到父物件的腳本
            BossBase boss = other.GetComponentInParent<BossBase>();
            
            if (boss != null)
            {
                boss.TakeHit(); // 呼叫 Boss 受傷
            }
            else
            {
                // Debug 用的，萬一 Tag 對了但找不到腳本，會報錯提醒你
                Debug.LogWarning($"撞到了 Tag 為 Boss 的物件 {other.name}，但在其父層找不到 BossBase 腳本！");
            }

            Destroy(gameObject); // 撞到敵人後銷毀子彈
        }
    }
}