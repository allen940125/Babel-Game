using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(LineRenderer))] // 1. 強制要求 LineRenderer 元件
public class EnemyBullet : MonoBehaviour
{
    [Header("隨機參數設定 (序列化)")]
    [SerializeField] private Vector2 speedRange = new Vector2(5f, 12f);
    [SerializeField] private Vector2 lifeTimeRange = new Vector2(3f, 6f);
    
    [Header("反彈設定")]
    [SerializeField] private LayerMask collisionLayer;
    [SerializeField] private bool showDebugLine = true; 

    // --- 新增：線條外觀設定 ---
    [Header("視覺射線設定")]
    [SerializeField] private float rayLength = 1.5f; // 線要畫多長
    [SerializeField] private Color rayColor = Color.yellow;
    [SerializeField] private float rayWidth = 0.05f;

    private Vector2 _currentDirection;
    private float _currentSpeed;
    private Rigidbody2D _rb;
    private LineRenderer _lineRenderer; // 快取元件

    public void Initialize(Vector2 startDirection , float finalSpeedMultiple)
    {
        _rb = GetComponent<Rigidbody2D>();
        _lineRenderer = GetComponent<LineRenderer>(); // 抓取 LineRenderer

        // 初始化隨機數值
        _currentSpeed = Random.Range(speedRange.x, speedRange.y);
        _currentSpeed *= finalSpeedMultiple;
        float lifeTime = Random.Range(lifeTimeRange.x, lifeTimeRange.y);
        _currentDirection = startDirection.normalized;
        
        Destroy(gameObject, lifeTime);

        // --- 初始化 LineRenderer 設定 (也可以在 Inspector 手動調) ---
        SetupLineRenderer();
    }

    private void SetupLineRenderer()
    {
        if (_lineRenderer == null) return;

        // 設定寬度
        _lineRenderer.startWidth = rayWidth;
        _lineRenderer.endWidth = rayWidth;
        
        // 設定顏色 (需要材質支援 Vertex Color，通常用 Sprites-Default 材質即可)
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.startColor = rayColor;
        _lineRenderer.endColor = rayColor;

        // 設定點數 (起點跟終點)
        _lineRenderer.positionCount = 2;
        
        // 如果不想讓線擋住子彈，可以調整 Sorting Order
        _lineRenderer.sortingOrder = 10; 
    }

    private void FixedUpdate()
    {
        MoveAndBounce();
        
        // 更新線的位置
        if (showDebugLine)
        {
            UpdateDebugLine();
        }
        else
        {
            _lineRenderer.enabled = false;
        }
    }

    private void UpdateDebugLine()
    {
        if (_lineRenderer == null) return;
        _lineRenderer.enabled = true;

        Vector3 startPos = transform.position;
        // 預設終點：子彈前方 rayLength 的距離
        Vector3 endPos = startPos + (Vector3)_currentDirection * rayLength;

        // --- 修改重點：發射一條射線去檢查有沒有撞到牆 ---
        // 使用與移動邏輯相同的 LayerMask (collisionLayer)
        RaycastHit2D hit = Physics2D.Raycast(startPos, _currentDirection, rayLength, collisionLayer);

        if (hit.collider != null)
        {
            // 如果撞到了牆壁 (或其他在 LayerMask 裡的東西)
            // 將線的終點「截斷」在撞擊點上
            endPos = hit.point;
        }

        _lineRenderer.SetPosition(0, startPos);
        _lineRenderer.SetPosition(1, endPos);
    }

    private void MoveAndBounce()
    {
        float stepDistance = _currentSpeed * Time.fixedDeltaTime;
        
        // 射線檢測
        RaycastHit2D hit = Physics2D.Raycast(transform.position, _currentDirection, stepDistance, collisionLayer);

        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("Wall"))
            {
                // 反射邏輯
                _currentDirection = Vector2.Reflect(_currentDirection, hit.normal);
            }
        }

        // 移動與旋轉
        _rb.linearVelocity = _currentDirection * _currentSpeed;
        float angle = Mathf.Atan2(_currentDirection.y, _currentDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) 
        {
            PlayerController2D player = other.GetComponent<PlayerController2D>();
            if (player == null) player = other.GetComponentInParent<PlayerController2D>();

            if (player != null)
            {
                player.TakeDamage(1);
            }
            Destroy(gameObject);
        }
    }
}