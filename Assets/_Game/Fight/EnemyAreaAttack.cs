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
        // ★ 3. 呼叫自我展示功能，一行解決所有形狀的繪製
        if (attackData != null)
        {
            attackData.damageShape.DrawGizmo(transform.position, transform.rotation, new Color(1f, 0.5f, 0f, 0.5f));
        }
    }
}