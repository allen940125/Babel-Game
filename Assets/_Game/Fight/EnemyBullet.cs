using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(LineRenderer))]
public class EnemyBullet : EnemyProjectileBase
{
    public enum EffectRotationMode { Fixed, AlignWithNormal, AlignWithReflection }

    [Header("隨機參數設定")]
    [SerializeField] private Vector2 speedRange = new Vector2(5f, 12f);
    [SerializeField] private Vector2 lifeTimeRange = new Vector2(3f, 6f);
    
    [Header("★ 純 3D 物理圖層")]
    [Tooltip("將 Wall, PlayerButton, 以及 Player 全部勾選進此單一圖層！")]
    [SerializeField] private LayerMask collisionLayer; 
    
    [SerializeField] private bool showDebugLine = true;
    public bool canPenetratePlayerButton = false;

    [Header("特效與視覺設定")]
    public bool showHitEffect = true; 
    public GameObject hitEffectPrefab; 
    public EffectRotationMode effectRotationMode = EffectRotationMode.AlignWithNormal;
    [SerializeField] private float rayLength = 5.0f;
    [SerializeField] private Color rayColor = Color.yellow;
    [SerializeField] private float rayWidth = 0.05f;
    [SerializeField] private int maxPredictionBounces = 2;

    private Vector3 _currentDirection;
    private float _currentSpeed;
    private Rigidbody _rb;
    private LineRenderer _lineRenderer;
    private bool _isInitialized = false;

    private RaycastHit[] _hitBuffer = new RaycastHit[16]; 

    // ★ 核心通訊管線：記錄我是屬於哪個 Boss 的
    private BossBase _ownerBoss;
    
    public override void Initialize(Vector3 startDirection, float finalSpeedMultiple, BossBase ownerBoss)
    {
        _ownerBoss = ownerBoss;
        if (_ownerBoss != null)
        {
            _ownerBoss.RegisterActiveBullet(this.gameObject);
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] 生成時未傳入 BossBase 參照，將無法自動除名！");
        }

        _rb = GetComponent<Rigidbody>();
        _lineRenderer = GetComponent<LineRenderer>();

        _currentSpeed = Random.Range(speedRange.x, speedRange.y) * finalSpeedMultiple;
        _currentDirection = new Vector3(startDirection.x, startDirection.y, 0f).normalized;
        
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        float lifeTime = Random.Range(lifeTimeRange.x, lifeTimeRange.y);
        Destroy(gameObject, lifeTime);
        
        SetupLineRenderer();
        UpdateVelocityAndRotation();
        _isInitialized = true;
    }

    // ★ 2. 絕對核心：死亡自動通告除名！
    // 不管它是時間到了被 Unity Destroy，還是撞到玩家被銷毀，這段必定會執行！
    private void OnDestroy()
    {
        if (_ownerBoss != null)
        {
            _ownerBoss.UnregisterActiveBullet(this.gameObject);
            Debug.Log($"<color=gray>[彈幕除名] {gameObject.name} 銷毀，已向 Boss 回報！</color>");
        }
    }

    private void SetupLineRenderer()
    {
        if (_lineRenderer == null) return;
    
        // ★ 強制鎖死世界座標系！杜絕 Inspector 漏勾選導致線條縮回局部原點
        _lineRenderer.useWorldSpace = true;
    
        _lineRenderer.startWidth = rayWidth;
        _lineRenderer.endWidth = rayWidth;
        if (_lineRenderer.sharedMaterial == null)
            _lineRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.startColor = rayColor;
        _lineRenderer.endColor = rayColor;
        _lineRenderer.sortingOrder = 10; 
    }

    private void FixedUpdate()
    {
        if (!_isInitialized) return;

        if (transform.position.z != 0f)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
        }

        MoveAndCollide();
        
        if (showDebugLine) UpdateDebugLine();
        else if (_lineRenderer != null) _lineRenderer.enabled = false;
    }

    // ==========================================
    // 核心物理：雷達偵測與反彈
    // ==========================================

    private void MoveAndCollide()
    {
        float stepDistance = _currentSpeed * Time.fixedDeltaTime;
        Vector3 halfExtents = new Vector3(rayWidth, rayWidth, 5.0f);

        int hitCount = Physics.BoxCastNonAlloc(transform.position, halfExtents, _currentDirection, _hitBuffer, Quaternion.identity, stepDistance, collisionLayer);

        if (hitCount > 0)
        {
            System.Array.Sort(_hitBuffer, 0, hitCount, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _hitBuffer[i];
                if (hit.collider == null) continue;

                // ★ 嚴格過濾 1：消除 distance == 0 導致 hit.point 變為 (0,0,0) 的物理引擎 Bug！
                if (hit.distance <= 0.0001f || hit.point == Vector3.zero) continue;

                string tag = hit.collider.tag;

                if (tag == "Player")
                {
                    TryDealDamage(hit.collider.gameObject);
                    SpawnHitEffect(hit.point, hit.normal, Vector3.zero);
                    continue; 
                }

                if (ShouldBounce(tag))
                {
                    Vector3 flatNormal = new Vector3(hit.normal.x, hit.normal.y, 0f).normalized;
                    Vector3 reflectionDir = Vector3.Reflect(_currentDirection, flatNormal);
            
                    SpawnHitEffect(hit.point, flatNormal, reflectionDir);
            
                    _currentDirection = reflectionDir.normalized;
                    UpdateVelocityAndRotation();
                    break; 
                }
            }
        }

        _rb.MovePosition(transform.position + _currentDirection * stepDistance);
    }

    private void UpdateVelocityAndRotation()
    {
        // 旋轉部分保持純 Z 軸運算
        float angle = Mathf.Atan2(_currentDirection.y, _currentDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        
        // 因為改為 Kinematic，我們在反彈或轉向瞬間，立即更新一次位置預測
        _rb.linearVelocity = Vector3.zero; // Kinematic 不使用 linearVelocity，強行歸零防干擾
    }

    private bool ShouldBounce(string targetTag)
    {
        if (targetTag == "Wall") return true;
        if (targetTag == "PlayerButton") return !canPenetratePlayerButton; 
        return false; 
    }

    private void SpawnHitEffect(Vector3 position, Vector3 normal, Vector3 reflectionDir)
    {
        if (!showHitEffect || hitEffectPrefab == null) return;
        Quaternion rotation = Quaternion.identity;
        switch (effectRotationMode)
        {
            case EffectRotationMode.Fixed: break;
            case EffectRotationMode.AlignWithNormal:
                rotation = Quaternion.FromToRotation(Vector3.up, normal);
                break;
            case EffectRotationMode.AlignWithReflection:
                if (reflectionDir != Vector3.zero)
                {
                    float angle = Mathf.Atan2(reflectionDir.y, reflectionDir.x) * Mathf.Rad2Deg;
                    rotation = Quaternion.Euler(0, 0, angle - 90f);
                }
                break;
        }
        Vector3 spawnPos = new Vector3(position.x, position.y, 0f);
        Instantiate(hitEffectPrefab, spawnPos, rotation);
    }

    // ==========================================
    // 視覺預測線：同步截斷障礙物與消除原點 Bug
    // ==========================================

    private void UpdateDebugLine()
    {
        if (_lineRenderer == null) return;
        _lineRenderer.enabled = true;

        List<Vector3> points = new List<Vector3>();
        Vector3 currentPosition = transform.position;
        Vector3 currentDir = _currentDirection;
        float remainingLength = rayLength;
        Vector3 halfExtents = new Vector3(rayWidth, rayWidth, 5.0f);

        points.Add(currentPosition);

        for (int i = 0; i <= maxPredictionBounces; i++)
        {
            if (remainingLength <= 0) break;

            int hitCount = Physics.BoxCastNonAlloc(currentPosition, halfExtents, currentDir, _hitBuffer, Quaternion.identity, remainingLength, collisionLayer);

            if (hitCount > 0)
            {
                System.Array.Sort(_hitBuffer, 0, hitCount, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));
                bool bouncedOrStopped = false;

                for (int j = 0; j < hitCount; j++)
                {
                    RaycastHit hit = _hitBuffer[j];
                    if (hit.collider == null) continue;

                    // ★ 嚴格過濾 2：絕不把 (0,0,0) 原點重疊點加入預測線！
                    if (hit.distance <= 0.0001f || hit.point == Vector3.zero) continue;

                    // 看到 Player 保持穿透
                    if (hit.collider.CompareTag("Player")) continue;

                    if (ShouldBounce(hit.collider.tag))
                    {
                        points.Add(hit.point);
                        float distanceTraveled = Vector3.Distance(currentPosition, hit.point);
                        remainingLength -= distanceTraveled;

                        Vector3 flatNormal = new Vector3(hit.normal.x, hit.normal.y, 0f).normalized;
                        currentDir = Vector3.Reflect(currentDir, flatNormal).normalized;
                        
                        // ★ 核心修復 3：將推離距離從 0.01f 提高到 rayWidth * 2f！
                        // 確保下一波 BoxCast 絕對已經離開牆面，不會觸發初始重疊Bug
                        currentPosition = hit.point + (currentDir * (rayWidth * 2f));
                        bouncedOrStopped = true;
                        break;
                    }
                    else
                    {
                        // ★ 核心修復 4：撞到「不可反彈且非玩家」的 3D 物件，線條必須立刻停在這裡，絕不穿透！
                        points.Add(hit.point);
                        bouncedOrStopped = true;
                        remainingLength = 0; // 強制結束外層預測迴圈
                        break;
                    }
                }

                if (!bouncedOrStopped)
                {
                    points.Add(currentPosition + (currentDir * remainingLength));
                    break;
                }
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
}