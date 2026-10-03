using Gamemanager;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyHomingMissile : EnemyAttackBase, IProjectile
{
    [Header("★ 導彈資料配置")]
    [SerializeField] private HomingMissileDataSO missileData;

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
        // 1. 移動向量計算 (分為 第一階段外拋 / 第二階段追蹤)
        // ==========================================
        if (_timer < missileData.homingDelay && missileData.launchMode == HomingMissileDataSO.LaunchMode.BezierArc)
        {
            // --- 第一階段：貝茲曲線 (等速飛行版) ---
            // ★ 利用設定的速度去推進 t 值，徹底擺脫 homingDelay 對速度的綁架
            _bezierT += (missileData.speed * Time.fixedDeltaTime) / _curveLength;
            
            // 確保 t 不會超過 1
            float t = Mathf.Clamp01(_bezierT);
            float u = 1f - t;
            Vector3 nextPos = (u * u * _p0) + (2f * u * t * _p1) + (t * t * _p2);
            
            Vector3 moveDelta = nextPos - _lastPosition;
            if (Time.fixedDeltaTime > 0) _currentVelocity = moveDelta / Time.fixedDeltaTime;
        }
        else
        {
            // --- 第二階段：進入正常飛行與動態追蹤 ---
            if (_target != null)
            {
                Vector3 targetPos = new Vector3(_target.position.x, _target.position.y, 0f);
                Vector3 myPos = new Vector3(transform.position.x, transform.position.y, 0f);
                Vector3 directionToTarget = (targetPos - myPos).normalized;
                
                Vector3 newDirection = Vector3.RotateTowards(_currentVelocity.normalized, directionToTarget, missileData.homingStrength * Mathf.Deg2Rad * Time.fixedDeltaTime, 0.0f);
                newDirection.z = 0f;
                _currentVelocity = newDirection.normalized * missileData.speed;
            }
        }

        // ==========================================
        // 2. 實體撞擊掃描 (沿用基類的 damageShape，不碰觸爆炸邏輯)
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
        // 3. 實際位移與旋轉
        // ==========================================
        transform.position += _currentVelocity * Time.fixedDeltaTime;
        _lastPosition = transform.position;

        if (_currentVelocity.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(_currentVelocity.y, _currentVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
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