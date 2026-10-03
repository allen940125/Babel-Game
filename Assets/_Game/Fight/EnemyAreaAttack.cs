using UnityEngine;
using Gamemanager;

public class EnemyDelayedAoE : EnemyAttackBase
{
    [Header("★ 攻擊資料配置")]
    [SerializeField] private DelayedAoEDataSO attackData;

    private readonly Collider[] _hitColliders = new Collider[16];
    private bool _hasExploded = false;

    public override void Initialize(BossStateMachine boss, float damageMultiplier = 1f, bool ignoreInvincibility = false)
    {
        float finalMultiplier = (attackData != null) ? attackData.damageMultiplier * damageMultiplier : damageMultiplier;
        bool finalIgnore = (attackData != null && attackData.ignoreInvincibility) || ignoreInvincibility;
        base.Initialize(boss, finalMultiplier, finalIgnore);
    }

    public void Explode()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        if (attackData == null) return;

        if (attackData.hitEffectPrefab != null)
        {
            Instantiate(attackData.hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // ★ 呼叫靜態重疊工具，傳入自身的 transform.rotation 作為基準
        int hitCount = ShapeOverlapUtility.PerformOverlap(
            transform.position, 
            transform.rotation, 
            _hitColliders, 
            attackData.damageShape, 
            attackData.damageLayer
        );

        for (int i = 0; i < hitCount; i++)
        {
            damageDealer.DealDamageTo(_hitColliders[i].gameObject);
        }

        if (attackData.enableCameraShake && GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.Send(new CameraShakeEvent { 
                Intensity = attackData.shakeIntensity, 
                Duration = attackData.shakeDuration 
            });
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        if (attackData == null) return;
        
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation * Quaternion.Euler(attackData.damageShape.shapeRotationOffset), Vector3.one);

        switch (attackData.damageShape.shapeType)
        {
            case BulletShapeType.Circle:
                Gizmos.DrawSphere(Vector3.zero, attackData.damageShape.radius);
                break;
            case BulletShapeType.Box:
                Gizmos.DrawCube(Vector3.zero, new Vector3(attackData.damageShape.boxSize.x, attackData.damageShape.boxSize.y, attackData.damageShape.zThickness));
                break;
            case BulletShapeType.Capsule:
                // 簡化的膠囊體視覺化
                float halfLen = Mathf.Max(0f, (attackData.damageShape.capsuleLength * 0.5f) - attackData.damageShape.radius);
                Gizmos.DrawWireSphere(Vector3.up * halfLen, attackData.damageShape.radius);
                Gizmos.DrawWireSphere(Vector3.down * halfLen, attackData.damageShape.radius);
                Gizmos.DrawLine(Vector3.up * halfLen, Vector3.down * halfLen);
                break;
        }
    }
}