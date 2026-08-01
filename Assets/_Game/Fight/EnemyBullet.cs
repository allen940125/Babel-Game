using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(LineRenderer))]
public class EnemyBullet : EnemyProjectileBase
{
    public enum EffectRotationMode { Fixed, AlignWithNormal, AlignWithReflection }

    [System.Serializable]
    public struct BulletStats
    {
        public Vector2 speedRange;
        public Vector2 lifeTimeRange;
        [Tooltip("僅勾選會反彈的(Wall)與會受傷的(Player)！其他介面一律不要勾選！")]
        public LayerMask collisionLayer;
    }

    [System.Serializable]
    public struct VFXConfig
    {
        public bool showHitEffect;
        public GameObject hitEffectPrefab;
        public EffectRotationMode rotationMode;
        public float rayWidth;
    }

    [System.Serializable]
    public struct LinePredictionConfig
    {
        public bool showDebugLine;
        public float rayLength;
        public Color rayColor;
        public int maxBounces;
    }

    [Header("★ 模組化設定資料")]
    [SerializeField] private BulletStats stats = new BulletStats { speedRange = new Vector2(5f, 12f), lifeTimeRange = new Vector2(3f, 6f), collisionLayer = -1 };
    [SerializeField] private VFXConfig vfx = new VFXConfig { showHitEffect = true, rotationMode = EffectRotationMode.AlignWithNormal, rayWidth = 0.05f };
    [SerializeField] private LinePredictionConfig prediction = new LinePredictionConfig { showDebugLine = true, rayLength = 5.0f, rayColor = Color.yellow, maxBounces = 2 };

    [Header("★ 偵錯與日誌")]
    [SerializeField] private bool enableDebugLog = false;

    // --- 內部運行時數據 (唯讀) ---
    private Vector3 _currentDirection;
    private float _currentSpeed;
    private Rigidbody _rb;
    private LineRenderer _lineRenderer;
    private bool _isInitialized = false;
    private BossBase _ownerBoss;
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[16];

    public override void Initialize(Vector3 startDirection, float finalSpeedMultiple, BossBase ownerBoss)
    {
        _ownerBoss = ownerBoss;
        if (_ownerBoss != null) _ownerBoss.RegisterActiveBullet(this.gameObject);

        _rb = GetComponent<Rigidbody>();
        _lineRenderer = GetComponent<LineRenderer>();

        _currentSpeed = Random.Range(stats.speedRange.x, stats.speedRange.y) * finalSpeedMultiple;
        _currentDirection = new Vector3(startDirection.x, startDirection.y, 0f).normalized;
        
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;

        // 強制把子彈本體與子物件所有 Collider 轉為 Trigger，杜絕實體幾何擠壓
        foreach (Collider col in GetComponentsInChildren<Collider>()) col.isTrigger = true;

        Destroy(gameObject, Random.Range(stats.lifeTimeRange.x, stats.lifeTimeRange.y));
        
        SetupLineRenderer();
        UpdateVelocityAndRotation();
        _isInitialized = true;
    }

    private void OnDestroy()
    {
        if (_ownerBoss != null) _ownerBoss.UnregisterActiveBullet(this.gameObject);
    }

    private void FixedUpdate()
    {
        if (!_isInitialized) return;
        if (transform.position.z != 0f) transform.position = new Vector3(transform.position.x, transform.position.y, 0f);

        MoveAndCollide();
        
        if (prediction.showDebugLine) UpdateDebugLine();
        else if (_lineRenderer != null) _lineRenderer.enabled = false;
    }

    // ==========================================
    // 核心物理：極限收斂的碰撞路由
    // ==========================================
    private void MoveAndCollide()
    {
        float stepDistance = _currentSpeed * Time.fixedDeltaTime;
        Vector3 halfExtents = new Vector3(vfx.rayWidth, vfx.rayWidth, 5.0f);

        int hitCount = Physics.BoxCastNonAlloc(transform.position, halfExtents, _currentDirection, _hitBuffer, Quaternion.identity, stepDistance, stats.collisionLayer);

        if (hitCount > 0)
        {
            System.Array.Sort(_hitBuffer, 0, hitCount, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _hitBuffer[i];
                if (hit.collider == null || hit.distance <= 0.0001f || hit.point == Vector3.zero) continue;

                GameObject targetObj = hit.collider.gameObject;
                string tag = hit.collider.tag;

                // ★ 路由分流 1：傷害端（處理受傷、扣血、穿透）
                if (tag == "Player")
                {
                    ProcessDamageTarget(targetObj, hit.point, hit.normal);
                    continue; // 傷害端完畢後，維持穿透繼續往前飛行
                }

                // ★ 路由分流 2：反彈端（處理牆壁、反射角、改方向）
                if (tag == "Wall")
                {
                    ProcessBounceTarget(targetObj, hit);
                    break; // 反彈端完畢後，方向已改變，立刻中斷本幀後續偵測！
                }

                // ★ 路由分流 3：未知物體直接靜默穿透
                continue;
            }
        }

        _rb.MovePosition(transform.position + _currentDirection * stepDistance);
    }

    // ==========================================
    // [傷害端專區] 只處理數值扣除、暴擊與受傷表現
    // ==========================================
    private void ProcessDamageTarget(GameObject target, Vector3 hitPoint, Vector3 normal)
    {
        if (enableDebugLog) Debug.Log($"<color=green>[傷害端觸發]</color> 命中目標: {target.name}");
        
        TryDealDamage(target); // 繼承自父類別的扣血邏輯
        SpawnHitEffect(hitPoint, normal, Vector3.zero);
    }

    // ==========================================
    // [反彈端專區] 只處理幾何運算、動能反射與轉向
    // ==========================================
    private void ProcessBounceTarget(GameObject target, RaycastHit hit)
    {
        if (enableDebugLog) Debug.Log($"<color=yellow>[反彈端觸發]</color> 撞擊牆面: {target.name} | 改變飛行軌跡");

        Vector3 flatNormal = new Vector3(hit.normal.x, hit.normal.y, 0f).normalized;
        Vector3 reflectionDir = Vector3.Reflect(_currentDirection, flatNormal).normalized;
            
        SpawnHitEffect(hit.point, flatNormal, reflectionDir);
            
        _currentDirection = reflectionDir;
        UpdateVelocityAndRotation();
    }

    private void UpdateVelocityAndRotation()
    {
        float angle = Mathf.Atan2(_currentDirection.y, _currentDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        _rb.linearVelocity = Vector3.zero; 
    }

    private void SpawnHitEffect(Vector3 position, Vector3 normal, Vector3 reflectionDir)
    {
        if (!vfx.showHitEffect || vfx.hitEffectPrefab == null) return;
        Quaternion rotation = Quaternion.identity;
        switch (vfx.rotationMode)
        {
            case EffectRotationMode.Fixed: break;
            case EffectRotationMode.AlignWithNormal: rotation = Quaternion.FromToRotation(Vector3.up, normal); break;
            case EffectRotationMode.AlignWithReflection:
                if (reflectionDir != Vector3.zero)
                {
                    float angle = Mathf.Atan2(reflectionDir.y, reflectionDir.x) * Mathf.Rad2Deg;
                    rotation = Quaternion.Euler(0, 0, angle - 90f);
                }
                break;
        }
        Instantiate(vfx.hitEffectPrefab, new Vector3(position.x, position.y, 0f), rotation);
    }

    // ==========================================
    // [預測線渲染專區] 嚴格對應反彈與傷害端規則
    // ==========================================
    private void SetupLineRenderer()
    {
        if (_lineRenderer == null) return;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.startWidth = vfx.rayWidth;
        _lineRenderer.endWidth = vfx.rayWidth;
        if (_lineRenderer.sharedMaterial == null) _lineRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.startColor = prediction.rayColor;
        _lineRenderer.endColor = prediction.rayColor;
        _lineRenderer.sortingOrder = 10; 
    }

    private void UpdateDebugLine()
    {
        if (_lineRenderer == null) return;
        _lineRenderer.enabled = true;

        List<Vector3> points = new List<Vector3>();
        Vector3 currentPos = transform.position;
        Vector3 currentDir = _currentDirection;
        float remainingLength = prediction.rayLength;
        Vector3 halfExtents = new Vector3(vfx.rayWidth, vfx.rayWidth, 5.0f);

        points.Add(currentPos);

        for (int i = 0; i <= prediction.maxBounces; i++)
        {
            if (remainingLength <= 0) break;

            int hitCount = Physics.BoxCastNonAlloc(currentPos, halfExtents, currentDir, _hitBuffer, Quaternion.identity, remainingLength, stats.collisionLayer);

            if (hitCount > 0)
            {
                System.Array.Sort(_hitBuffer, 0, hitCount, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));
                bool bouncedOrStopped = false;

                for (int j = 0; j < hitCount; j++)
                {
                    RaycastHit hit = _hitBuffer[j];
                    if (hit.collider == null || hit.distance <= 0.0001f || hit.point == Vector3.zero) continue;

                    string tag = hit.collider.tag;

                    // 遇到玩家直接穿透，預測線繼續往後延伸
                    if (tag == "Player") continue;

                    if (tag == "Wall")
                    {
                        points.Add(hit.point);
                        remainingLength -= Vector3.Distance(currentPos, hit.point);

                        Vector3 flatNormal = new Vector3(hit.normal.x, hit.normal.y, 0f).normalized;
                        currentDir = Vector3.Reflect(currentDir, flatNormal).normalized;
                        currentPos = hit.point + (currentDir * (vfx.rayWidth * 2f));
                        bouncedOrStopped = true;
                        break;
                    }
                    else
                    {
                        // 撞到不可知的實體，強制截斷
                        points.Add(hit.point);
                        bouncedOrStopped = true;
                        remainingLength = 0; 
                        break;
                    }
                }

                if (!bouncedOrStopped) { points.Add(currentPos + (currentDir * remainingLength)); break; }
            }
            else
            {
                points.Add(currentPos + (currentDir * remainingLength));
                break;
            }
        }

        _lineRenderer.positionCount = points.Count;
        _lineRenderer.SetPositions(points.ToArray());
    }
}