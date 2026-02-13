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
    
    public LayerMask targetLayer; // 爆炸時要偵測誰 (Player)
    public LayerMask wallLayer;   // 誰算牆壁

    [Header("導引優化")]
    public float homingDelay = 0.5f;

    private float _timer = 0f;
    private Rigidbody2D _rb;
    private Transform _target;
    
    // ★ damageAmount 已經在父類別定義了，這裡不用再寫

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public override void Initialize(Vector2 startDirection, float incomingSpeed)
    {
        this.speed = incomingSpeed;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _target = playerObj.transform;

        float angleOffset = initialArcAngle;
        if (randomArcDirection) angleOffset *= (Random.value > 0.5f) ? 1f : -1f;
        else angleOffset *= arcToRight ? -1f : 1f; 

        Vector2 initialVelocityDir = RotateVector(startDirection.normalized, angleOffset);
        
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        _rb.linearVelocity = initialVelocityDir * speed;
        
        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        _timer += Time.fixedDeltaTime;

        if (_timer < homingDelay || _target == null)
        {
            if (_rb.linearVelocity != Vector2.zero)
            {
                float angle = Mathf.Atan2(_rb.linearVelocity.y, _rb.linearVelocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle - 90f); 
            }
            return;
        }

        // 導引邏輯
        Vector2 directionToTarget = (_target.position - transform.position).normalized;
        Vector2 currentDirection = _rb.linearVelocity.normalized;
        Vector3 newDirection = Vector3.RotateTowards(currentDirection, directionToTarget, homingStrength * Mathf.Deg2Rad * Time.fixedDeltaTime, 0.0f);
        _rb.linearVelocity = newDirection.normalized * speed;

        if (_rb.linearVelocity != Vector2.zero)
        {
            float angle = Mathf.Atan2(_rb.linearVelocity.y, _rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f); 
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 撞牆判定 (統一用 LayerMask 位元運算)
        if (((1 << other.gameObject.layer) & wallLayer) != 0)
        {
            if (canPenetrateWalls) return; 
            else Explode(); 
        }
        
        // 2. 撞人判定 (簡單用 Tag，或者你也可以用 GetComponent)
        if (other.CompareTag("Player"))
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (explosionEffectPrefab != null)
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);

        // ★ 使用 Physics2D 抓取範圍內的 Player
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, targetLayer);
        foreach (var hit in hits)
        {
            TryDealDamage(hit); // 呼叫父類別方法
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
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}