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

    private void FixedUpdate()
    {
        if (_hasExploded || missileData == null) return;

        _timer += Time.fixedDeltaTime;

        // ==========================================
        // 1. 移動向量計算：基於發射模式的狀態機解耦
        // ==========================================
        if (missileData.launchMode == HomingMissileDataSO.LaunchMode.BezierArc)
        {
            // --- 模式 B：貝茲曲線 ---
            if (_bezierT < 1f)
            {
                _bezierT += (missileData.speed * Time.fixedDeltaTime) / _curveLength;

                float t = Mathf.Clamp01(_bezierT);
                float u = 1f - t;
                Vector3 nextPos = (u * u * _p0) + (2f * u * t * _p1) + (t * t * _p2);

                Vector3 moveDelta = nextPos - _lastPosition;

                // 【防抖防護 1】只有當位移大於一個安全微小值時，才更新速度，避免浮點數誤差引起方向突變
                if (moveDelta.sqrMagnitude > 0.0001f)
                {
                    _currentVelocity = moveDelta.normalized * missileData.speed;
                }
            }
            else
            {
                // 曲線已精準抵達
                ApplyHomingDynamics();
            }
        }
        else if (missileData.launchMode == HomingMissileDataSO.LaunchMode.AngleSpread)
        {
            // --- 模式 A：角度散射 ---
            if (_timer >= missileData.homingDelay)
            {
                ApplyHomingDynamics();
            }
        }

        // ==========================================
        // 2. 實體撞擊掃描
        // ==========================================
        float distanceThisFrame = _currentVelocity.magnitude * Time.fixedDeltaTime;
        Vector3 moveDir = _currentVelocity.normalized;

        int hitCount = ShapeCastUtility.PerformCast(
            transform.position, moveDir, distanceThisFrame,
            _hitBuffer, missileData.damageShape, missileData.damageLayer
        );

        if (hitCount > 0)
        {
            TriggerPayload();
            return;
        }

        // ==========================================
        // 3. 實際位移與旋轉 (升級：極致抗抖動旋轉)
        // ==========================================
        transform.position += _currentVelocity * Time.fixedDeltaTime;
        _lastPosition = transform.position;

        // 【防抖防護 2】大幅提高速度採樣門檻。極微小的速度波動不允許計算旋轉
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
                // 【防抖防護 3】使用平滑插值 (LerpAngle)，並過濾不可能的「瞬間 180 度大反轉」
                // 由於 Mathf.LerpAngle 會自動處理 360 度跨界，它能強行抹平任何 1 幀內發生的抖動
                float currentAngle = transform.rotation.eulerAngles.z;

                // 計算這一幀與上一幀的角度差
                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));

                // 如果在 1 幀內角速度差大於 90 度，很可能是物理突變或穿過目標時的方向反轉，
                // 這時候直接捨棄突變，以防跳動
                if (angleDiff > 90f)
                {
                    targetAngle = _lastSafeAngle;
                }

                // 平滑過渡到目標角度，徹底告別 1 幀內的“卡頓和瞬跳”
                float smoothedAngle =
                    Mathf.LerpAngle(currentAngle, targetAngle, rotationSmoothSpeed * Time.fixedDeltaTime);
                transform.rotation = Quaternion.Euler(0, 0, smoothedAngle);
                _lastSafeAngle = smoothedAngle;
            }
        }
    }

    // 獨立出追蹤邏輯，消除重複代碼
    // 獨立出追蹤邏輯，消除重複代碼
    private void ApplyHomingDynamics()
    {
        if (_target != null && missileData.homingStrength > 0f)
        {
            Vector3 targetPos = new Vector3(_target.position.x, _target.position.y, 0f);
            Vector3 myPos = new Vector3(transform.position.x, transform.position.y, 0f);
            
            // 算出玩家與導彈的相對向量
            Vector3 offset = targetPos - myPos;
            
            // ★ 防抖動核心：近距離致盲
            // 如果距離玩家太近 (這裡設為距離 0.5，換算平方為 0.25f，可依你的碰撞體大小調整)
            // 就直接 return，保持最後的方向直衝，避免向量反轉導致的瘋狂抖動
            if (offset.sqrMagnitude < 0.25f)
            {
                return; 
            }

            Vector3 directionToTarget = offset.normalized;
            
            // 脫鎖機制：計算「導彈當前飛行方向」與「目標方向」的夾角
            float angleToTarget = Vector3.Angle(_currentVelocity.normalized, directionToTarget);
            
            // 如果玩家已經閃躲到導彈的側邊或背後（例如大於 60 度），放棄追蹤
            if (angleToTarget > 60f) 
            {
                return; 
            }

            // 仍在視野內且保持安全距離，進行平滑轉向
            Vector3 newDirection = Vector3.RotateTowards(
                _currentVelocity.normalized, 
                directionToTarget, 
                missileData.homingStrength * Mathf.Deg2Rad * Time.fixedDeltaTime, 
                0.0f
            );
            newDirection.z = 0f;
            
            // 防護機制：避免 newDirection 剛好歸零
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
            if (payload.TryGetComponent<EnemyAttackBase>(out var payloadAttack))
            {
                payloadAttack.Initialize(ownerBoss, missileData.damageMultiplier, missileData.ignoreInvincibility);
            }
        }
        // 觸發相機震動
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