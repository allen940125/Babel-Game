using UnityEngine;
using System.Collections.Generic; // 需要引用 List

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(LineRenderer))]
public class EnemyBullet : MonoBehaviour
{
    [Header("隨機參數設定 (序列化)")]
    [SerializeField] private Vector2 speedRange = new Vector2(5f, 12f);
    [SerializeField] private Vector2 lifeTimeRange = new Vector2(3f, 6f);
    
    [Header("反彈與穿透設定")]
    [SerializeField] private LayerMask collisionLayer; 
    [SerializeField] private bool showDebugLine = true;
    
    // --- 控制穿透 ---
    [Tooltip("如果打勾，就會穿過 PlayerButton；如果沒打勾，就會反彈")]
    public bool canPenetratePlayerButton = false; 

    // --- 新增：特效設定 ---
    [Header("特效設定")]
    [Tooltip("撞到牆壁反彈時要生成的特效 Prefab")]
    public GameObject hitEffectPrefab; 

    [Header("視覺射線設定")]
    [SerializeField] private float rayLength = 5.0f; // 總長度建議拉長一點，才看得到多次反彈
    [SerializeField] private Color rayColor = Color.yellow;
    [SerializeField] private float rayWidth = 0.05f;
    
    // --- 新增：反彈預測次數 ---
    [Tooltip("射線要預測幾次反彈？")]
    [SerializeField] private int maxPredictionBounces = 2; 

    private Vector2 _currentDirection;
    private float _currentSpeed;
    private Rigidbody2D _rb;
    private LineRenderer _lineRenderer;

    public void Initialize(Vector2 startDirection, float finalSpeedMultiple)
    {
        _rb = GetComponent<Rigidbody2D>();
        _lineRenderer = GetComponent<LineRenderer>();

        // 不需要再設定 canPenetratePlayerButton，直接用 Inspector 的設定

        _currentSpeed = Random.Range(speedRange.x, speedRange.y);
        _currentSpeed *= finalSpeedMultiple;
        float lifeTime = Random.Range(lifeTimeRange.x, lifeTimeRange.y);
        _currentDirection = startDirection.normalized;
        
        Destroy(gameObject, lifeTime);
        SetupLineRenderer();
    }

    private void SetupLineRenderer()
    {
        if (_lineRenderer == null) return;
        _lineRenderer.startWidth = rayWidth;
        _lineRenderer.endWidth = rayWidth;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.startColor = rayColor;
        _lineRenderer.endColor = rayColor;
        _lineRenderer.sortingOrder = 10; 
    }

    private void FixedUpdate()
    {
        MoveAndBounce();
        
        if (showDebugLine)
        {
            UpdateDebugLine();
        }
        else
        {
            _lineRenderer.enabled = false;
        }
    }

    // --- 修改重點 1: 多重反彈預測邏輯 ---
    private void UpdateDebugLine()
    {
        if (_lineRenderer == null) return;
        _lineRenderer.enabled = true;

        // 用來存儲所有路徑點 (起點 -> 撞擊點1 -> 撞擊點2 -> 終點)
        List<Vector3> points = new List<Vector3>();
        
        Vector2 currentPosition = transform.position;
        Vector2 currentDir = _currentDirection;
        float remainingLength = rayLength;

        // 加入起點
        points.Add(currentPosition);

        // 模擬迴圈
        for (int i = 0; i <= maxPredictionBounces; i++)
        {
            // 發射射線，長度為「剩餘長度」
            RaycastHit2D hit = Physics2D.Raycast(currentPosition, currentDir, remainingLength, collisionLayer);

            if (hit.collider != null)
            {
                // 如果撞到了...
                
                // 1. 加入撞擊點
                points.Add(hit.point);

                // 2. 扣除已經走過的距離
                float distanceTraveled = Vector2.Distance(currentPosition, hit.point);
                remainingLength -= distanceTraveled;

                // 如果剩餘長度歸零，就結束預測
                if (remainingLength <= 0) break;

                // 3. 判斷是否需要反彈 (如果是穿透，就不用改變方向，但要更新位置)
                if (ShouldBounce(hit.collider))
                {
                    // 計算反射方向
                    currentDir = Vector2.Reflect(currentDir, hit.normal);
                }
                
                // 4. 更新下一次發射的起點 (稍微往前推一點點 0.01f，避免射線卡在牆壁裡自己撞自己)
                currentPosition = hit.point + (currentDir * 0.01f);
            }
            else
            {
                // 如果沒撞到，就畫到剩餘長度的盡頭
                points.Add(currentPosition + (currentDir * remainingLength));
                break; // 沒撞到東西，預測結束
            }
        }

        // 更新 LineRenderer
        _lineRenderer.positionCount = points.Count;
        _lineRenderer.SetPositions(points.ToArray());
    }

    private void MoveAndBounce()
    {
        float stepDistance = _currentSpeed * Time.fixedDeltaTime;
        
        RaycastHit2D hit = Physics2D.Raycast(transform.position, _currentDirection, stepDistance, collisionLayer);

        if (hit.collider != null)
        {
            if (ShouldBounce(hit.collider))
            {
                // --- 修改重點 2: 生成撞擊特效 ---
                SpawnHitEffect(hit.point, hit.normal);

                // 反射方向
                _currentDirection = Vector2.Reflect(_currentDirection, hit.normal);
            }
        }

        _rb.linearVelocity = _currentDirection * _currentSpeed;

        float angle = Mathf.Atan2(_currentDirection.y, _currentDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f); 
    }

    // --- 新增：生成特效的方法 ---
    private void SpawnHitEffect(Vector2 position, Vector2 normal)
    {
        if (hitEffectPrefab != null)
        {
            // 生成特效
            // Quaternion.identity 代表不旋轉
            // 如果你的特效需要「背對牆壁噴發」，可以用 Quaternion.FromToRotation(Vector3.up, normal)
            Instantiate(hitEffectPrefab, position, Quaternion.identity);
        }
    }

    private bool ShouldBounce(Collider2D collider)
    {
        if (collider.CompareTag("Wall")) return true;

        if (collider.CompareTag("PlayerButton"))
        {
            return !canPenetratePlayerButton; 
        }

        return true; 
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