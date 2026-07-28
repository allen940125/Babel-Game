using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CircleCollider2D))]
public class EnemyTurretAttack : EnemyAttackObject
{
    [Header("範圍偵測設定")]
    [Tooltip("要攻擊的目標圖層 (必須設定！例如 Player)")]
    public LayerMask targetLayer; 

    [Header("特效設定")]
    public GameObject hitEffectPrefab;

    private CircleCollider2D _myCollider;
    private bool _hasExploded = false;

    private void Awake()
    {
        _myCollider = GetComponent<CircleCollider2D>();
        
        // 關閉碰撞器，只用它的物理半徑數據來做 OverlapCircle
        _myCollider.enabled = false;
        _myCollider.isTrigger = true;
        
        // ★ 強力鎖死 Z 軸座標為 0，確保 2D 畫面絕對不會前後漂移
        Vector3 fixedPos = transform.position;
        fixedPos.z = 0f;
        transform.position = fixedPos;
    }

    // ★ 刪除了浪費效能的 Update！
    // 請在 Unity 動畫編輯器 (Animation Window) 中，於爆炸動畫的「最後一格」加入 Animation Event 呼叫此方法！
    public void Explode()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        if (hitEffectPrefab != null)
        {
            // 特效生成時也強制把 Z 軸設為 0
            Vector3 spawnPos = new Vector3(transform.position.x, transform.position.y, 0f);
            Instantiate(hitEffectPrefab, spawnPos, Quaternion.identity);
        }

        // 取得實際縮放後的偵測半徑
        float radius = _myCollider.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
        
        // 在 2D 平面上做圓形偵測
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, targetLayer);

        foreach (var hit in hits)
        {
            // ★ 修正合約：將 Collider2D 轉為 GameObject 傳入！
            TryDealDamage(hit.gameObject);
        }

        Destroy(gameObject);
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            float r = col.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y, 0f), r);
        }
    }
}