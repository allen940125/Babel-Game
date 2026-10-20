using UnityEngine;

[RequireComponent(typeof(EntityHealthComponent))]
public class DestructibleBomb : MonoBehaviour
{
    [Header("★ 爆炸設定")]
    [Tooltip("爆炸時生成的特效 (這個特效裡面應該要有 DamageDealer)")]
    public GameObject explosionPrefab;
    
    private EntityHealthComponent _healthComp;

    private void Awake()
    {
        _healthComp = GetComponent<EntityHealthComponent>();
        if (_healthComp != null)
        {
            // 訂閱死亡事件
            _healthComp.OnDeath += TriggerExplosion;
        }
    }

    private void OnDestroy()
    {
        // 防呆：註銷事件
        if (_healthComp != null)
        {
            _healthComp.OnDeath -= TriggerExplosion;
        }
    }

    // 也可以開放給外部強制引爆 (例如：總管說時間到了，全爆！)
    public void TriggerExplosion()
    {
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        
        // 炸彈完成使命，自己銷毀
        Destroy(gameObject);
    }
}