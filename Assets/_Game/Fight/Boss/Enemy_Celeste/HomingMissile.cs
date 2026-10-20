using Gamemanager;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyHomingMissile : EnemyAttackBase, IProjectile
{
    [Header("★ 導彈資料配置")]
    [SerializeField] private HomingMissileDataSO missileData;
    
    [Header("★ 旋轉平滑度")] 
    [SerializeField] private float rotationSmoothSpeed = 15f; // 數值越大轉越快，但能完美過濾瞬間抖動（建議 15 - 30 之間）
        
    // 用來記錄上一次安全的、被採用的角度，萬一發生極小抖動就使用舊角度，拒絕亂跳
    private float _lastSafeAngle = 0f;
    private bool _hasInitializedAngle = false;

    private Rigidbody _rb;
    private Transform _target;
    private float _timer = 0f;
    private bool _hasExploded = false;
    private Vector3 _currentVelocity;
    private Vector3 _lastPosition;
    
    // 貝茲曲線專用控制點
    private Vector3 _p0, _p1, _p2;
    private float _bezierT = 0f;     // 獨立的貝茲進度
    private float _curveLength = 1f; // 曲線總長度
    
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[4];
    
    // ★ 1. 宣告畫筆
    private TrajectoryVisualizer _visualizer;

    public override void Initialize(BossStateMachine boss, float damageMultiplier = 1f, bool ignoreInvincibility = false)
    {
        float finalMultiplier = (missileData != null) ? missileData.damageMultiplier * damageMultiplier : damageMultiplier;
        bool finalIgnore = (missileData != null && missileData.ignoreInvincibility) || ignoreInvincibility;
        base.Initialize(boss, finalMultiplier, finalIgnore);

        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true; 
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) _target = playerObj.transform;

        // ★ 2. 抓取畫筆組件
        _visualizer = GetComponentInChildren<TrajectoryVisualizer>();
    }
    
    private void FixedUpdate()
    {
        if (_hasExploded || missileData == null) return;

        // ==========================================
        // ★ 3. 雷射車頭燈模式：每一幀重新預測未來軌跡
        // ==========================================
        if (_visualizer != null)
        {
            // 將導彈「當下的位置」與「當下實際飛行的方向」餵給 SO 算軌跡
            Vector3 currentDir = _currentVelocity.normalized;
            if (currentDir.sqrMagnitude < 0.001f) currentDir = transform.up;

            Vector3[] futurePoints = missileData.CalculatePredictedTrajectory(transform.position, currentDir);
            _visualizer.DrawTrajectory(futurePoints);
        }

        _timer += Time.fixedDeltaTime;

        if (missileData.launchMode == HomingMissileDataSO.LaunchMode.BezierArc)
        {
            if (_bezierT < 1f)
            {
                _bezierT += (missileData.speed * Time.fixedDeltaTime) / _curveLength;
                float t = Mathf.Clamp01(_bezierT);
                float u = 1f - t;
                Vector3 nextPos = (u * u * _p0) + (2f * u * t * _p1) + (t * t * _p2);
                Vector3 moveDelta = nextPos - _lastPosition;

                if (moveDelta.sqrMagnitude > 0.0001f)
                {
                    _currentVelocity = moveDelta.normalized * missileData.speed;
                }
            }
            else
            {
                ApplyHomingDynamics();
            }
        }
        else if (missileData.launchMode == HomingMissileDataSO.LaunchMode.AngleSpread)
        {
            if (_timer >= missileData.homingDelay)
            {
                ApplyHomingDynamics();
            }
        }

        // ==========================================
        // 2. 實體撞擊掃描 (★ 牆壁與玩家合併掃描)
        // ==========================================
        float distanceThisFrame = _currentVelocity.magnitude * Time.fixedDeltaTime;
        Vector3 moveDir = _currentVelocity.normalized;

        // ★ 把傷害層(玩家)與障礙物層(牆壁)加在一起，只要撞到任何一個都會有反應
        LayerMask combinedMask = missileData.damageLayer | missileData.obstacleLayer;

        int hitCount = ShapeCastUtility.PerformCast(
            transform.position, moveDir, distanceThisFrame,
            _hitBuffer, missileData.damageShape, combinedMask
        );

        if (hitCount > 0)
        {
            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCol = _hitBuffer[i].collider;
                if (hitCol != null)
                {
                    // ★ 檢查撞到的這個東西，是不是屬於「要扣血的目標 (damageLayer)」？
                    // 使用位元運算來極速比對 Layer
                    if ((missileData.damageLayer.value & (1 << hitCol.gameObject.layer)) != 0)
                    {
                        damageDealer.DealDamageTo(hitCol.gameObject);
                        break; // 扣到一次血就中斷，避免一發導彈扣兩次血
                    }
                }
            }

            // 不管是撞到玩家還是撞到牆壁，導彈最終都會引爆並銷毀
            TriggerPayload();
            return;
        }

        // ==========================================
        // 3. 實際位移與旋轉
        // ==========================================
        transform.position += _currentVelocity * Time.fixedDeltaTime;
        _lastPosition = transform.position;

        if (_currentVelocity.sqrMagnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(_currentVelocity.y, _currentVelocity.x) * Mathf.Rad2Deg - 90f;

            if (!_hasInitializedAngle)
            {
                _lastSafeAngle = targetAngle;
                _hasInitializedAngle = true;
                transform.rotation = Quaternion.Euler(0, 0, targetAngle);
            }
            else
            {
                float currentAngle = transform.rotation.eulerAngles.z;
                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));

                if (angleDiff > 90f) targetAngle = _lastSafeAngle;

                float smoothedAngle = Mathf.LerpAngle(currentAngle, targetAngle, rotationSmoothSpeed * Time.fixedDeltaTime);
                transform.rotation = Quaternion.Euler(0, 0, smoothedAngle);
                _lastSafeAngle = smoothedAngle;
            }
        }
    }

    public void SetTrajectory(Vector3 direction, float speed)
    {
        if (missileData == null) return;
        
        _lastPosition = transform.position;

        if (missileData.launchMode == HomingMissileDataSO.LaunchMode.AngleSpread)
        {
            // --- 模式 A：角度散射 ---
            float angleOffset = missileData.initialArcAngle;
            if (missileData.randomArcDirection) angleOffset *= (Random.value > 0.5f) ? 1f : -1f;
            else angleOffset *= missileData.arcToRight ? -1f : 1f; 

            Vector3 rotatedDir = Quaternion.Euler(0, 0, angleOffset) * direction;
            _currentVelocity = rotatedDir.normalized * missileData.speed;
        }
        else
        {
            // --- 模式 B：貝茲曲線 ---
            _p0 = transform.position;
            _p2 = (_target != null) ? new Vector3(_target.position.x, _target.position.y, 0f) : _p0 + direction.normalized * 10f;
            
            Vector3 midPoint = (_p0 + _p2) / 2f;
            Vector3 dirToTarget = (_p2 - _p0).normalized;
            Vector3 perpendicular = new Vector3(-dirToTarget.y, dirToTarget.x, 0f);

            float actualOffset = missileData.bezierArcOffset;
            if (missileData.randomBezierFlip && Random.value > 0.5f) actualOffset = -actualOffset;
            
            _p1 = midPoint + perpendicular * actualOffset;

            // ★ 新增：估算二次貝茲曲線的總長度 (弦長與控制網的平均值，效能極佳的逼近法)
            float chord = Vector3.Distance(_p0, _p2);
            float contNet = Vector3.Distance(_p0, _p1) + Vector3.Distance(_p1, _p2);
            _curveLength = (chord + contNet) / 2f;
            _bezierT = 0f; // 歸零進度
        }
        
        Destroy(gameObject, missileData.lifeTime);
    }

    private void ApplyHomingDynamics()
    {
        if (_target != null && missileData.homingStrength > 0f)
        {
            Vector3 targetPos = new Vector3(_target.position.x, _target.position.y, 0f);
            Vector3 myPos = new Vector3(transform.position.x, transform.position.y, 0f);
            Vector3 offset = targetPos - myPos;
            
            if (offset.sqrMagnitude < 0.25f) return; 

            Vector3 directionToTarget = offset.normalized;
            float angleToTarget = Vector3.Angle(_currentVelocity.normalized, directionToTarget);
            
            if (angleToTarget > 60f) return; 

            Vector3 newDirection = Vector3.RotateTowards(
                _currentVelocity.normalized, 
                directionToTarget, 
                missileData.homingStrength * Mathf.Deg2Rad * Time.fixedDeltaTime, 
                0.0f
            );
            newDirection.z = 0f;
            
            if (newDirection.sqrMagnitude > 0.001f)
            {
                _currentVelocity = newDirection.normalized * missileData.speed;
            }
        }
    }

    private void TriggerPayload()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        if (missileData.hitEffectPrefab != null)
        {
            GameObject payload = Instantiate(missileData.hitEffectPrefab, transform.position, Quaternion.identity);
            
            // ★ 註解區塊：不再把傷害資料傳遞給特效 (現在特效純粹只是視覺煙火)
            /*
            if (payload.TryGetComponent<EnemyAttackBase>(out var payloadAttack))
            {
                payloadAttack.Initialize(ownerBoss, missileData.damageMultiplier, missileData.ignoreInvincibility);
            }
            */
        }
        
        if (missileData != null && missileData.enableCameraShake && GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.Send(new CameraShakeEvent { Intensity = missileData.shakeIntensity, Duration = missileData.shakeDuration });
        }
        
        Destroy(gameObject);
    }
    
    private void OnDrawGizmos()
    {
        if (missileData != null && !Application.isPlaying)
        {
            Vector3 dir = transform.up;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.up;
            // 畫出導彈彈體大小 (不是爆炸大小)
            missileData.damageShape.DrawGizmo(transform.position, Quaternion.LookRotation(Vector3.forward, dir), Color.yellow);
        }
    }
}