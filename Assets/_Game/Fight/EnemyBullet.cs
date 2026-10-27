using System.Collections.Generic;
using Gamemanager;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
public class EnemyBullet : EnemyAttackBase, IProjectile
{
    public enum EffectRotationMode { Fixed, AlignWithNormal, AlignWithReflection }

    [System.Serializable]
    public struct BulletStats
    {
        public Vector2 speedRange;
        public Vector2 lifeTimeRange;
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

    public Vector3 currentDirection;
    public float currentSpeed;
    private Rigidbody _rb;
    private bool _isInitialized = false;
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[16];

    private int _bounceCount = 0;
    private float[] _bounceJitterOffsets;
    
    private TrajectoryVisualizer _visualizer;
    
    private Dictionary<GameObject, float> _lastDamageTimes;
    
    [Header("Debug")] 
    [SerializeField] private float debug_bulletData_damageMultiplier;
    [SerializeField] private float debug_AttackPattern_damageMultiplier;
    [SerializeField] private float debug_actualMultiplier;

    private void OnDestroy()
    {
        if (ownerBoss != null) ownerBoss.UnregisterActiveBullet(this.gameObject);
    }
    
    // ==================================================
    // ★ 職責 1：覆寫基類，只處理「數值、狀態與記憶體初始化」
    // ==================================================
    public override void Initialize(BossStateMachine boss, float damageMultiplier = 1f, bool ignoreInvincibility = false)
    {
        // 1. 記憶體分配
        _lastDamageTimes = new Dictionary<GameObject, float>();

        // 2. 結合 BulletDataSO 的專屬倍率
        float actualMultiplier = (bulletData != null) ? bulletData.damageMultiplier * damageMultiplier : damageMultiplier;
        bool finalIgnoreInvincibility = (bulletData != null && bulletData.ignoreInvincibility) || ignoreInvincibility;

        // 3. 呼叫父類別，完成 Snapshot 注入 (無物理參數)
        base.Initialize(boss, actualMultiplier, finalIgnoreInvincibility);

        if (ownerBoss != null) ownerBoss.RegisterActiveBullet(this.gameObject);

        _rb = GetComponent<Rigidbody>();
        _visualizer = GetComponentInChildren<TrajectoryVisualizer>();

        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;

        int maxCapacity = Mathf.Max(_hitBuffer.Length, 16);
        _bounceJitterOffsets = new float[maxCapacity];
        for (int i = 0; i < maxCapacity; i++)
        {
            _bounceJitterOffsets[i] = (bulletData != null && bulletData.jitterFirstBounceOnly && i > 0) ? 0f : Random.Range(-bulletData.maxBounceAngleJitter, bulletData.maxBounceAngleJitter);
        }
    }

    // ==================================================
    // ★ 職責 2：實作 IProjectile，專職處理「飛行物理軌跡」
    // ==================================================
    // 把原本那個 _precalculatedTrajectory 變數刪掉，我們不需要記住舊軌跡了！

// 新增一個 autoDestroy 參數，預設為 true 保留敵人子彈原本的行為
    public void SetTrajectory(Vector3 direction, float speed, bool autoDestroy = true)
    {
        currentSpeed = Random.Range(stats.speedRange.x, stats.speedRange.y) * speed;
        currentDirection = new Vector3(direction.x, direction.y, 0f).normalized;
    
        // ★ 只有敵人的子彈會自爆，玩家的投擲物傳入 false 就不會消失了！
        if (autoDestroy)
        {
            Destroy(gameObject, Random.Range(stats.lifeTimeRange.x, stats.lifeTimeRange.y));
        }
    
        UpdateVelocityAndRotation();
        _isInitialized = true; 
    }

    private void FixedUpdate()
    {
        if (!_isInitialized) return;
        MoveAndCollide();

        // ==========================================
        // ★ 核心修復：雷射車頭燈模式 (即時動態預測)
        // ==========================================
        if (_visualizer != null && bulletData != null)
        {
            // 每一幀都從「子彈當下的位置」與「當下的方向」，重新往未來發射虛擬射線！
            // 這樣算出來的點，永遠是精確的「接下來會去哪裡」
            Vector3[] futurePoints = bulletData.CalculatePredictedTrajectory(transform.position, currentDirection);
            
            _visualizer.DrawTrajectory(futurePoints);
        }
    }

    private const float COLLISION_SKIN = 0.005f;
    private const int MAX_BOUNCES_PER_FRAME = 8;

    private void MoveAndCollide()
    {
        float remainingDistance = currentSpeed * Time.fixedDeltaTime;

        for (int bounceStep = 0; bounceStep < MAX_BOUNCES_PER_FRAME; bounceStep++)
        {
            if (remainingDistance <= 0.0001f) break;
            Vector3 currentPos = transform.position;

            // ==================================================
            // 1. 找牆壁 (★ 實體物理的唯一標準)
            // ==================================================
            int bounceHitCount = ShapeCastUtility.PerformCast(
                currentPos, currentDirection, remainingDistance,
                _hitBuffer, bulletData.bounceShape, bulletData.bounceLayer, enableDeepDebug);
            
            RaycastHit nearestWallHit = default;
            bool hasWallHit = false;

            if (bounceHitCount > 0)
            {
                System.Array.Sort(_hitBuffer, 0, bounceHitCount, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));
                for (int i = 0; i < bounceHitCount; i++)
                {
                    RaycastHit hit = _hitBuffer[i];
                    if (hit.collider == null || hit.distance < 0.0001f) continue;
                    nearestWallHit = hit;
                    hasWallHit = true;
                    break;
                }
            }

            // ★ 決定本次真正能走的距離 (沒牆壁就走到底，有牆壁就走到牆壁前)
            float travelDistance = hasWallHit ? nearestWallHit.distance : remainingDistance;

            // ==================================================
            // 2. 找玩家 (★ 純結算傷害，不影響物理軌跡)
            // ==================================================
            int damageHitCount = ShapeCastUtility.PerformCast(
                currentPos, currentDirection, remainingDistance,
                _hitBuffer, bulletData.damageShape, bulletData.damageLayer, enableDeepDebug);

            if (damageHitCount > 0)
            {
                for (int i = 0; i < damageHitCount; i++)
                {
                    RaycastHit hit = _hitBuffer[i];
                    if (hit.collider == null) continue;

                    // ★ 核心過濾：只要玩家在「本次即將飛越的距離」內，就對他造成傷害
                    if (hit.distance <= travelDistance + 0.01f)
                    {
                        GameObject targetObj = hit.collider.gameObject;

                        // 持續傷害的冷卻判定
                        if (bulletData != null && bulletData.useDamageTick)
                        {
                            if (_lastDamageTimes.TryGetValue(targetObj, out float lastTime))
                            {
                                if (Time.time < lastTime + bulletData.damageTickInterval)
                                    continue; // 冷卻中，跳過
                            }
                            _lastDamageTimes[targetObj] = Time.time;
                        }

                        damageDealer.DealDamageTo(targetObj);
                    }
                }
            }

            // ==================================================
            // 3. 移動與反彈結算
            // ==================================================
            // 子彈瞬間走到決定好的位置
            transform.position = currentPos + currentDirection * travelDistance;
            remainingDistance -= travelDistance;

            if (hasWallHit)
            {
                // 觸發相機震動
                if (bulletData != null && bulletData.enableCameraShake && GameManager.Instance?.MainGameEvent != null)
                {
                    GameManager.Instance.MainGameEvent.Send(new CameraShakeEvent { Intensity = bulletData.shakeIntensity, Duration = bulletData.shakeDuration });
                }

                // 處理反彈，並把子彈稍微推離牆壁一點點避免卡死
                ProcessBounceTarget(nearestWallHit);
                transform.position += currentDirection * COLLISION_SKIN;
                remainingDistance = Mathf.Max(0f, remainingDistance - COLLISION_SKIN);
            }
            else
            {
                // 沒撞到牆壁，已經走完所有距離，結束這幀的運算
                break;
            }
        }

        // 保證 Z 軸維持 0
        if (transform.position.z != 0f)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
        }
    }

    private void ProcessBounceTarget(RaycastHit hit)
    {
        Vector3 flatNormal = new Vector3(hit.normal.x, hit.normal.y, 0f).normalized;
        float jitter = (_bounceCount < _bounceJitterOffsets.Length) ? _bounceJitterOffsets[_bounceCount] : 0f;

        Vector3 pureReflection = Vector3.Reflect(currentDirection, flatNormal).normalized;
        Vector3 newDir = Quaternion.Euler(0f, 0f, jitter) * pureReflection;

        if (Vector3.Dot(newDir, flatNormal) <= 0.087f) newDir = pureReflection;

        SpawnHitEffect(hit.point, flatNormal, newDir);
        currentDirection = newDir.normalized;
        _bounceCount++;
        UpdateVelocityAndRotation();
    }

    private void UpdateVelocityAndRotation()
    {
        float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
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
        if (bulletData != null)
        {
            bulletData.bounceShape.DrawGizmo(transform.position, transform.rotation, Color.yellow);
            bulletData.damageShape.DrawGizmo(transform.position, transform.rotation, Color.red);
        }
    }
}