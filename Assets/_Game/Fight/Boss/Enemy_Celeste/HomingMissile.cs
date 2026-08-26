using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class HomingMissile : EnemyProjectileBase
{
    [Header("導彈運動參數")]
    public float speed = 8f;
    public float homingStrength = 100f;
    public float lifeTime = 5f;

    [Header("初始弧形軌跡")]
    public float initialArcAngle = 45f;
    public bool randomArcDirection = true;
    public bool arcToRight = true;

    [Header("穿透與爆炸")]
    public bool canPenetrateWalls = false;
    public float explosionRadius = 1.5f;
    public GameObject explosionEffectPrefab;
    
    public LayerMask targetLayer; // 爆炸要偵測的目標 (Player)
    public LayerMask wallLayer;   // 牆壁圖層

    [Header("導引優化")]
    public float homingDelay = 0.5f;

    private float _timer = 0f;
    private Rigidbody2D _rb;
    private Transform _target;
    private bool _hasExploded = false;
    
    private BossStateMachine _ownerBoss;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        
        // ★ 第一道防線：在剛體層面直接鎖死 Z 軸移動與 X/Y 軸翻滾！
        //_rb.constraints = RigidbodyConstraints2D.FreezePositionZ | RigidbodyConstraints2D.FreezeRotation;
        
        // 強制歸零 Z 軸座標
        //transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
    }

    public override void Initialize(Vector3 startDirection, float incomingSpeed, BossStateMachine _ownerBoss)
    {
        this.speed = incomingSpeed;
        
        // 效能優化：改用 FindWithTag，比 FindGameObjectWithTag 更快
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) _target = playerObj.transform;

        float angleOffset = initialArcAngle;
        if (randomArcDirection) angleOffset *= (Random.value > 0.5f) ? 1f : -1f;
        else angleOffset *= arcToRight ? -1f : 1f; 

        // ★ 確保初始發射方向是一個純 2D 向量
        Vector2 initialVelocityDir = RotateVector(startDirection.normalized, angleOffset);
        
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        _rb.linearVelocity = initialVelocityDir * speed;
        
        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        if (_hasExploded) return;

        _timer += Time.fixedDeltaTime;

        // ★ 第二道防線：每一幀強力將 Z 軸歸零，防止任何碰撞造成的漂移
        if (transform.position.z != 0f)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
        }

        if (_timer < homingDelay || _target == null)
        {
            UpdateRotationFromVelocity();
            return;
        }

        // --- 核心導引邏輯 ---
        // ★ 第三道防線：計算目標方向時，強力把目標和自己的 Z 軸當作 0 來算！
        Vector2 targetPos2D = new Vector2(_target.position.x, _target.position.y);
        Vector2 myPos2D = new Vector2(transform.position.x, transform.position.y);
        
        Vector2 directionToTarget = (targetPos2D - myPos2D).normalized;
        Vector2 currentDirection = _rb.linearVelocity.normalized;
        
        Vector3 newDirection = Vector3.RotateTowards(currentDirection, directionToTarget, homingStrength * Mathf.Deg2Rad * Time.fixedDeltaTime, 0.0f);
        
        // 確保設定速度時只有 XY 分量
        _rb.linearVelocity = new Vector2(newDirection.x, newDirection.y).normalized * speed;

        UpdateRotationFromVelocity();
    }

    private void UpdateRotationFromVelocity()
    {
        if (_rb.linearVelocity != Vector2.zero)
        {
            float angle = Mathf.Atan2(_rb.linearVelocity.y, _rb.linearVelocity.x) * Mathf.Rad2Deg;
            // 旋轉時也絕對只轉 Z 軸 (Euler Z)
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f); 
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasExploded) return;

        // 1. 撞牆判定
        if (((1 << other.gameObject.layer) & wallLayer) != 0)
        {
            if (canPenetrateWalls) return; 
            else Explode(); 
        }
        
        // 2. 撞人判定
        if (other.CompareTag("Player"))
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        if (explosionEffectPrefab != null)
        {
            // 特效也強制生成在 Z = 0 的平面上
            Vector3 spawnPos = new Vector3(transform.position.x, transform.position.y, 0f);
            Instantiate(explosionEffectPrefab, spawnPos, Quaternion.identity);
        }

        // 使用 Physics2D 抓取範圍內的目標
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, targetLayer);
        foreach (var hit in hits)
        {
            // ★ 修正合約：將 Collider2D 轉為 GameObject 傳入！
            //TryDealDamage(hit.gameObject); 
        }

        Destroy(gameObject);
    }

    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(new Vector3(transform.position.x, transform.position.y, 0f), explosionRadius);
    }
}