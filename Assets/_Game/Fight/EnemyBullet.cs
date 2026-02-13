using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(LineRenderer))]
public class EnemyBullet : EnemyProjectileBase
{
    // --- 新增：特效旋轉模式列舉 ---
    public enum EffectRotationMode
    {
        Fixed,              // 固定不旋轉 (Quaternion.identity)
        AlignWithNormal,    // 背對牆壁 (法線方向)
        AlignWithReflection // 跟隨子彈反彈後的方向
    }

    [Header("隨機參數設定 (序列化)")]
    [SerializeField] private Vector2 speedRange = new Vector2(5f, 12f);
    [SerializeField] private Vector2 lifeTimeRange = new Vector2(3f, 6f);
    
    [Header("反彈與穿透設定")]
    [SerializeField] private LayerMask collisionLayer; 
    [SerializeField] private bool showDebugLine = true;
    public bool canPenetratePlayerButton = false;

    [Header("特效設定")]
    public bool showHitEffect = true; 
    public GameObject hitEffectPrefab; 
    public EffectRotationMode effectRotationMode = EffectRotationMode.AlignWithNormal;
    
    [Header("視覺射線設定")]
    [SerializeField] private float rayLength = 5.0f;
    [SerializeField] private Color rayColor = Color.yellow;
    [SerializeField] private float rayWidth = 0.05f;
    [SerializeField] private int maxPredictionBounces = 2;

    private Vector2 _currentDirection;
    private float _currentSpeed;
    private Rigidbody2D _rb;
    private LineRenderer _lineRenderer;
    
    [Header("特殊設定")]
    [SerializeField] private bool isNotNeedInit;

    private void Awake()
    {
        if (isNotNeedInit)
        {
            _rb = GetComponent<Rigidbody2D>();
            float lifeTime = Random.Range(lifeTimeRange.x, lifeTimeRange.y);
            Destroy(gameObject, lifeTime);
        }
    }

    public override void Initialize(Vector2 startDirection, float finalSpeedMultiple)
    {
        _rb = GetComponent<Rigidbody2D>();
        _lineRenderer = GetComponent<LineRenderer>();

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

    private void UpdateDebugLine()
    {
        if (_lineRenderer == null) return;
        _lineRenderer.enabled = true;

        List<Vector3> points = new List<Vector3>();
        
        Vector2 currentPosition = transform.position;
        Vector2 currentDir = _currentDirection;
        float remainingLength = rayLength;

        points.Add(currentPosition);

        for (int i = 0; i <= maxPredictionBounces; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(currentPosition, currentDir, remainingLength, collisionLayer);

            if (hit.collider != null)
            {
                points.Add(hit.point);

                float distanceTraveled = Vector2.Distance(currentPosition, hit.point);
                remainingLength -= distanceTraveled;

                if (remainingLength <= 0) break;

                if (ShouldBounce(hit.collider))
                {
                    currentDir = Vector2.Reflect(currentDir, hit.normal);
                }
                
                currentPosition = hit.point + (currentDir * 0.01f);
            }
            else
            {
                points.Add(currentPosition + (currentDir * remainingLength));
                break;
            }
        }

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
                Vector2 reflectionDir = Vector2.Reflect(_currentDirection, hit.normal);
                SpawnHitEffect(hit.point, hit.normal, reflectionDir);
                _currentDirection = reflectionDir;
            }
        }

        _rb.linearVelocity = _currentDirection * _currentSpeed;
        float angle = Mathf.Atan2(_currentDirection.y, _currentDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f); 
    }

    // --- 修改：生成特效的方法 ---
    private void SpawnHitEffect(Vector2 position, Vector2 normal, Vector2 reflectionDir)
    {
        // 1. 檢查開關 & Prefab 是否存在
        if (!showHitEffect || hitEffectPrefab == null) return;

        Quaternion rotation = Quaternion.identity;

        // 2. 根據設定決定旋轉角度
        switch (effectRotationMode)
        {
            case EffectRotationMode.Fixed:
                // 固定不轉 (適合圓形爆炸)
                //rotation = Quaternion.identity;
                break;

            case EffectRotationMode.AlignWithNormal:
                // 特效的 "上方 (Up)" 會朝向法線方向 (也就是背對牆壁)
                // 適合：撞擊塵土、碎片噴濺
                rotation = Quaternion.FromToRotation(Vector3.up, normal);
                break;

            case EffectRotationMode.AlignWithReflection:
                // 特效的 "上方" 會朝向子彈反彈後飛行的方向
                // 適合：衝擊波、導向特效
                float angle = Mathf.Atan2(reflectionDir.y, reflectionDir.x) * Mathf.Rad2Deg;
                rotation = Quaternion.Euler(0, 0, angle - 90f); // 假設特效圖也是頭朝上
                break;
        }

        Instantiate(hitEffectPrefab, position, rotation);
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
            // 直接呼叫父類別方法！
            TryDealDamage(other);
        }
    }
}