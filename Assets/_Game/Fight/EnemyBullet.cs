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

    private const float COLLISION_SKIN = 0.005f;
    private const int MAX_BOUNCES_PER_FRAME = 8;

    private void MoveAndCollide()
    {
        float remainingDistance = _currentSpeed * Time.fixedDeltaTime;

        for (int bounceStep = 0; bounceStep < MAX_BOUNCES_PER_FRAME; bounceStep++)
        {
            if (remainingDistance <= 0.0001f)
                break;

            Vector3 currentPos = transform.position;

            // ==================================================
            // 1. 找牆壁
            // ==================================================
            int bounceHitCount = ShapeCastUtility.PerformCast(
                currentPos,
                _currentDirection,
                remainingDistance,
                _hitBuffer,
                bulletData.bounceShape,
                bulletData.bounceLayer,
                enableDeepDebug
            );

            RaycastHit nearestWallHit = default;
            bool hasWallHit = false;

            if (bounceHitCount > 0)
            {
                System.Array.Sort(
                    _hitBuffer,
                    0,
                    bounceHitCount,
                    Comparer<RaycastHit>.Create(
                        (a, b) => a.distance.CompareTo(b.distance)
                    )
                );

                for (int i = 0; i < bounceHitCount; i++)
                {
                    RaycastHit hit = _hitBuffer[i];

                    if (hit.collider == null)
                        continue;

                    // ★ 避免一開始就重疊造成 0 距離假反彈
                    if (hit.distance < 0.0001f)
                        continue;

                    nearestWallHit = hit;
                    hasWallHit = true;
                    break;
                }
            }

            // ==================================================
            // 2. 找玩家
            // ==================================================
            int damageHitCount = ShapeCastUtility.PerformCast(
                currentPos,
                _currentDirection,
                remainingDistance,
                _hitBuffer,
                bulletData.damageShape,
                bulletData.damageLayer,
                enableDeepDebug
            );

            RaycastHit nearestDamageHit = default;
            bool hasDamageHit = false;

            if (damageHitCount > 0)
            {
                float nearestDistance = float.MaxValue;

                for (int i = 0; i < damageHitCount; i++)
                {
                    RaycastHit hit = _hitBuffer[i];

                    if (hit.collider == null)
                        continue;

                    if (hit.distance < 0.0001f)
                        continue;

                    if (hit.distance < nearestDistance)
                    {
                        nearestDistance = hit.distance;
                        nearestDamageHit = hit;
                        hasDamageHit = true;
                    }
                }
            }

            // ==================================================
            // 3. 這一段誰比較近？
            // ==================================================

            bool wallFirst =
                hasWallHit &&
                (!hasDamageHit ||
                 nearestWallHit.distance <= nearestDamageHit.distance);

            bool playerFirst =
                hasDamageHit &&
                (!hasWallHit ||
                 nearestDamageHit.distance < nearestWallHit.distance);

            // ==================================================
            // 4. 先撞到牆
            // ==================================================
            if (wallFirst)
            {
                float travelDistance = nearestWallHit.distance;

                // ★ 先把子彈真正移到碰撞位置
                transform.position =
                    currentPos + _currentDirection * travelDistance;

                // 這一幀還剩多少距離？
                remainingDistance -= travelDistance;

                // ★ 在真正碰撞位置反彈
                ProcessBounceTarget(nearestWallHit);

                // ★ 稍微推離牆面，避免下一次 ShapeCast 從牆裡開始
                transform.position += _currentDirection * COLLISION_SKIN;

                remainingDistance =
                    Mathf.Max(0f, remainingDistance - COLLISION_SKIN);

                continue;
            }

            // ==================================================
            // 5. 先撞到玩家
            // ==================================================
            if (playerFirst)
            {
                if (enableDeepDebug) Debug.Log($"[傷害判定] 命中玩家: {nearestDamageHit.collider.name}");
                
                damageDealer.DealDamageTo(nearestDamageHit.collider.gameObject);
                
                // ★ 核心修正：穿透行為
                // 子彈不會因為撞到玩家而停下，它會直接無視玩家，走完這幀剩餘的全部距離。
                // (注意：這前提是你希望子彈穿過玩家後，"在這一幀內" 不會立刻撞牆。
                // 若穿透後可能立刻撞牆，你需要將位置移到玩家身上並繼續迴圈，但通常彈幕遊戲直接走完即可)
                transform.position = currentPos + _currentDirection * remainingDistance;
                remainingDistance = 0f;
                break; // 距離歸零了，跳出迴圈
            }

            // ==================================================
            // 6. 什麼都沒撞到，直接走完
            // ==================================================
            transform.position = currentPos + _currentDirection * remainingDistance;
            remainingDistance = 0f;
            break;
        }

        // 保證 Z 軸維持 0
        if (transform.position.z != 0f)
        {
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y,
                0f
            );
        }
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

    // ★ 編輯器視覺化：同時畫出兩個框，用顏色區分
    private void OnDrawGizmos()
    {
        if (bulletData == null) return;

        Vector3 dir = Application.isPlaying ? _currentDirection : transform.up;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.up;

        // 1. 畫反彈框 (黃色)
        Gizmos.color = Color.yellow;
        DrawShapeGizmo(bulletData.bounceShape, dir);

        // 2. 畫傷害框 (紅色)
        Gizmos.color = Color.red;
        DrawShapeGizmo(bulletData.damageShape, dir);
    }

    private void DrawShapeGizmo(CollisionShapeConfig cfg, Vector3 dir)
    {
        Gizmos.matrix = Matrix4x4.identity;
        Quaternion offsetRot = Quaternion.Euler(cfg.shapeRotationOffset);
        Quaternion baseRot = Quaternion.LookRotation(Vector3.forward, dir);
        Quaternion finalRot = baseRot * offsetRot;

        switch (cfg.shapeType)
        {
            case BulletShapeType.Circle:
                Gizmos.DrawWireSphere(transform.position, cfg.radius);
                break;
            case BulletShapeType.Box:
                Matrix4x4 old = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, finalRot, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(cfg.boxSize.x, cfg.boxSize.y, 0.5f));
                Gizmos.matrix = old;
                break;
            case BulletShapeType.Capsule:
                float halfLen = Mathf.Max(0f, (cfg.capsuleLength * 0.5f) - cfg.radius);
                Vector3 axis = finalRot * Vector3.up;
                Vector3 p1 = transform.position - axis * halfLen;
                Vector3 p2 = transform.position + axis * halfLen;
                Gizmos.DrawWireSphere(p1, cfg.radius);
                Gizmos.DrawWireSphere(p2, cfg.radius);
                Gizmos.DrawLine(p1, p2);
                Vector3 side = finalRot * Vector3.right;
                Gizmos.DrawLine(p1 + side * cfg.radius, p2 + side * cfg.radius);
                Gizmos.DrawLine(p1 - side * cfg.radius, p2 - side * cfg.radius);
                break;
        }
    }
}