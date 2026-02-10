using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
// 1. 修改繼承：繼承自 EnemyProjectileBase
public class HomingMissile : EnemyProjectileBase
{
    [Header("導彈運動參數")]
    [Tooltip("導彈飛行速度 (會被 Initialize 傳入的數值覆蓋)")]
    public float speed = 8f;

    [Tooltip("導引力度 (每秒轉向的角度)。數值越小轉彎越慢(甩尾)，數值越大鎖定越死")]
    public float homingStrength = 100f;

    [Tooltip("導彈存活時間")]
    public float lifeTime = 5f;

    [Header("初始弧形軌跡")]
    [Tooltip("發射時的偏離角度 (製造弧形用)，例如 45 度")]
    public float initialArcAngle = 45f;
    
    [Tooltip("隨機決定弧形往左還是往右飛？")]
    public bool randomArcDirection = true;
    
    [Tooltip("如果不隨機，是否強制往右飛？(False 則往左)")]
    public bool arcToRight = true;

    [Header("穿透與爆炸")]
    [Tooltip("是否能穿牆？(True: 飛越牆壁; False: 撞牆爆炸)")]
    public bool canPenetrateWalls = false;

    [Tooltip("爆炸半徑")]
    public float explosionRadius = 1.5f;

    [Tooltip("爆炸傷害")]
    public int damage = 1;

    [Tooltip("爆炸特效 Prefab")]
    public GameObject explosionEffectPrefab;
    
    [Tooltip("要攻擊的目標圖層 (通常是 Player)")]
    public LayerMask targetLayer;
    
    [Tooltip("會阻擋導彈的牆壁圖層")]
    public LayerMask wallLayer;

    [Header("導引優化")]
    [Tooltip("發射後幾秒才開始追蹤？(製造漂亮的發射弧線用)")]
    public float homingDelay = 0.5f; // 建議設 0.3 ~ 0.5 秒

    private float _timer = 0f; // 計時器
    
    // 內部變數
    private Rigidbody2D _rb;
    private Transform _target;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    // 2. 修改 Initialize：配合父類別簽名 (override)，並接收 speed
    public override void Initialize(Vector2 startDirection, float incomingSpeed)
    {
        // ★ 關鍵：將 Boss/Pattern 傳進來的速度，套用到導彈設定上
        // 這樣 Boss 生氣時 (速度加倍)，導彈也會跟著變快
        this.speed = incomingSpeed;

        // --- 以下邏輯保持不變 ---

        // 1. 自動尋找場景中的玩家作為目標
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _target = playerObj.transform;
        }

        // 2. 設定初始弧形軌跡
        float angleOffset = initialArcAngle;

        if (randomArcDirection)
        {
            angleOffset *= (Random.value > 0.5f) ? 1f : -1f;
        }
        else
        {
            angleOffset *= arcToRight ? -1f : 1f; 
        }

        // 將初始方向旋轉偏移角度
        Vector2 initialVelocityDir = RotateVector(startDirection.normalized, angleOffset);
        
        // 確保剛體存在 (防止 Awake 還沒執行的極端情況)
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();

        _rb.linearVelocity = initialVelocityDir * speed;
        
        // 設定銷毀時間
        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        // 1. 更新計時器
        _timer += Time.fixedDeltaTime;

        // 2. 如果還在延遲時間內，或者沒有目標
        if (_timer < homingDelay || _target == null)
        {
            // ★ 保持原本的慣性飛行 (只更新速度方向的圖片旋轉，但不改變飛行方向)
            // 這樣它就會乖乖地往「右下/左下」飛一段時間
            if (_rb.linearVelocity != Vector2.zero)
            {
                // 這裡只更新圖片朝向，不改變 rb.linearVelocity
                float angle = Mathf.Atan2(_rb.linearVelocity.y, _rb.linearVelocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle - 90f); 
            }
            return; // <--- 直接跳出，不執行下面的導引邏輯
        }

        // --- 以下是原本的導引邏輯 (時間到了才會跑這段) ---
        
        Vector2 directionToTarget = (_target.position - transform.position).normalized;
        Vector2 currentDirection = _rb.linearVelocity.normalized;

        Vector3 newDirection = Vector3.RotateTowards(
            currentDirection, 
            directionToTarget, 
            homingStrength * Mathf.Deg2Rad * Time.fixedDeltaTime, 
            0.0f
        );

        _rb.linearVelocity = newDirection.normalized * speed;

        // 更新圖片旋轉
        if (_rb.linearVelocity != Vector2.zero)
        {
            float angle = Mathf.Atan2(_rb.linearVelocity.y, _rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f); 
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 撞到牆壁
        if (((1 << other.gameObject.layer) & wallLayer) != 0)
        {
            if (canPenetrateWalls) return; 
            else Explode(); 
        }
        
        // 2. 撞到玩家
        if (other.CompareTag("Player"))
        {
            Explode();
        }
    }

    // --- 爆炸邏輯 ---
    private void Explode()
    {
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, targetLayer);
        foreach (var hit in hits)
        {
            // 直接呼叫父類別方法！
            TryDealDamage(hit);
        }

        Destroy(gameObject);
    }

    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        float tx = v.x;
        float ty = v.y;
        return new Vector2(cos * tx - sin * ty, sin * tx + cos * ty);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}