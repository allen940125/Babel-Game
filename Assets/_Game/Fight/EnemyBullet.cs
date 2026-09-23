using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyBullet : EnemyProjectileBase
{
    public enum EffectRotationMode { Fixed, AlignWithNormal, AlignWithReflection }

    [System.Serializable]
    public struct BulletStats
    {
        public Vector2 speedRange;
        public Vector2 lifeTimeRange;
        public LayerMask collisionLayer;

        [Header("★ 反彈擾動設定")]
        [Range(0f, 60f)] public float maxBounceAngleJitter;
        public bool jitterFirstBounceOnly;
    }

    [System.Serializable]
    public struct VFXConfig
    {
        public bool showHitEffect;
        public GameObject hitEffectPrefab;
        public EffectRotationMode rotationMode;
    }

    [Header("★ 碰撞形狀設定")]
    public BulletDataSO bulletData;

    [Header("★ 嚴格除錯模式")]
    [Tooltip("打勾後，會在 Console 印出每一幀子彈撞到了什麼、Tag 是什麼")]
    [SerializeField] private bool enableDeepDebug = false;

    [Header("★ 模組化設定資料")]
    [SerializeField] private BulletStats stats;
    [SerializeField] private VFXConfig vfx;

    private Vector3 _currentDirection;
    private float _currentSpeed;
    private Rigidbody _rb;
    private bool _isInitialized = false;
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[16];

    private int _bounceCount = 0;
    private float[] _bounceJitterOffsets;
    
    private TrajectoryVisualizer _visualizer;

    public override void Initialize(Vector3 direction, float speed, BossStateMachine boss)
    {
        base.Initialize(direction, speed, boss);

        if (ownerBoss != null) ownerBoss.RegisterActiveBullet(this.gameObject);

        _rb = GetComponent<Rigidbody>();
        
        _visualizer = GetComponentInChildren<TrajectoryVisualizer>();

        _currentSpeed = Random.Range(stats.speedRange.x, stats.speedRange.y) * speed;
        _currentDirection = new Vector3(direction.x, direction.y, 0f).normalized;

        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;

        int maxCapacity = Mathf.Max(_hitBuffer.Length, 16);
        _bounceJitterOffsets = new float[maxCapacity];
        for (int i = 0; i < maxCapacity; i++)
        {
            _bounceJitterOffsets[i] = (stats.jitterFirstBounceOnly && i > 0) ? 0f : Random.Range(-stats.maxBounceAngleJitter, stats.maxBounceAngleJitter);
        }

        Destroy(gameObject, Random.Range(stats.lifeTimeRange.x, stats.lifeTimeRange.y));

        UpdateVelocityAndRotation();
        _isInitialized = true;
    }

    private void OnDestroy()
    {
        if (ownerBoss != null) ownerBoss.UnregisterActiveBullet(this.gameObject);
    }

    private void FixedUpdate()
    {
        if (!_isInitialized) return;
        if (transform.position.z != 0f) transform.position = new Vector3(transform.position.x, transform.position.y, 0f);

        MoveAndCollide();

        // ★ 新增：每一幀子彈移動後，把「我真正在飛的方向」餵給畫線工具
        if (_visualizer != null)
        {
            _visualizer.DrawTrajectory(transform.position, _currentDirection, _bounceJitterOffsets);
        }
    }

    private void MoveAndCollide()
    {
        float stepDistance = _currentSpeed * Time.fixedDeltaTime;

        int hitCount = ShapeCastUtility.PerformCast(transform.position, _currentDirection, stepDistance, _hitBuffer, bulletData.shapeConfig, stats.collisionLayer, enableDeepDebug);

        if (hitCount > 0)
        {
            System.Array.Sort(_hitBuffer, 0, hitCount, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _hitBuffer[i];
                if (hit.collider == null || hit.distance <= 0.0001f || hit.point == Vector3.zero) continue;

                string tag = hit.collider.tag;

                if (enableDeepDebug)
                {
                    Debug.Log($"[子彈路由分配] 命中目標: {hit.collider.name} | 讀取到的 Tag: '{tag}'");
                }

                if (tag == "Player")
                {
                    damageDealer.DealDamageTo(hit.collider.gameObject);
                    SpawnHitEffect(hit.point, hit.normal, Vector3.zero);
                    continue;
                }

                if (tag == "Wall")
                {
                    if (enableDeepDebug) Debug.Log($"[觸發反彈] 目標確認為 Wall，執行反彈邏輯。");
                    ProcessBounceTarget(hit);
                    break;
                }
                else
                {
                    if (enableDeepDebug) Debug.LogWarning($"[警告: 靜默穿透] 撞擊物體 '{hit.collider.name}' 的 Tag 是 '{tag}'，既不是 Player 也不是 Wall，將被直接穿透忽略！");
                }
            }
        }
        _rb.MovePosition(transform.position + _currentDirection * stepDistance);
    }

    private void ProcessBounceTarget(RaycastHit hit)
    {
        Vector3 flatNormal = new Vector3(hit.normal.x, hit.normal.y, 0f).normalized;
        float jitter = (_bounceCount < _bounceJitterOffsets.Length) ? _bounceJitterOffsets[_bounceCount] : 0f;

        Vector3 pureReflection = Vector3.Reflect(_currentDirection, flatNormal).normalized;
        Vector3 newDir = Quaternion.Euler(0f, 0f, jitter) * pureReflection;

        if (Vector3.Dot(newDir, flatNormal) <= 0.087f) newDir = pureReflection;

        SpawnHitEffect(hit.point, flatNormal, newDir);
        _currentDirection = newDir.normalized;
        _bounceCount++;
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
            case EffectRotationMode.AlignWithNormal: rotation = Quaternion.FromToRotation(Vector3.up, normal); break;
            case EffectRotationMode.AlignWithReflection:
                if (reflectionDir != Vector3.zero) rotation = Quaternion.Euler(0, 0, Mathf.Atan2(reflectionDir.y, reflectionDir.x) * Mathf.Rad2Deg - 90f);
                break;
        }
        Instantiate(vfx.hitEffectPrefab, new Vector3(position.x, position.y, 0f), rotation);
    }

    // ★ 編輯器視覺化
    private void OnDrawGizmos()
    {
        if (bulletData == null) return;

        Gizmos.color = Color.red;
        Gizmos.matrix = Matrix4x4.identity;   // ★ 先歸位，避免被前一次污染

        var cfg = bulletData.shapeConfig;
        Vector3 dir = Application.isPlaying ? _currentDirection : transform.up;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.up;

        // ★ 跟 ShapeCastUtility 用同一套旋轉邏輯
        Quaternion offsetRot = Quaternion.Euler(cfg.shapeRotationOffset);
        Quaternion baseRot = Quaternion.LookRotation(Vector3.forward, dir);
        Quaternion finalRot = baseRot * offsetRot;

        switch (cfg.shapeType)
        {
            case BulletShapeType.Circle:
            {
                // 球體視覺上不需要旋轉，畫圓就好
                Gizmos.DrawWireSphere(transform.position, cfg.radius);
                break;
            }

            case BulletShapeType.Box:
            {
                Matrix4x4 old = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, finalRot, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(cfg.boxSize.x, cfg.boxSize.y, 0.5f));
                Gizmos.matrix = old;
                break;
            }

            case BulletShapeType.Capsule:
            {
                float halfLen = Mathf.Max(0f, (cfg.capsuleLength * 0.5f) - cfg.radius);
                Vector3 axis = finalRot * Vector3.up;   // ★ 用旋轉後的軸線

                Vector3 p1 = transform.position - axis * halfLen;
                Vector3 p2 = transform.position + axis * halfLen;

                Gizmos.DrawWireSphere(p1, cfg.radius);
                Gizmos.DrawWireSphere(p2, cfg.radius);
                Gizmos.DrawLine(p1, p2);   // 畫中軸，比側邊線更清楚

                // 可選：畫垂直於軸的「腰帶」幫助辨識方向
                Vector3 side = finalRot * Vector3.right;
                Gizmos.DrawLine(p1 + side * cfg.radius, p2 + side * cfg.radius);
                Gizmos.DrawLine(p1 - side * cfg.radius, p2 - side * cfg.radius);
                break;
            }
        }
    }
}