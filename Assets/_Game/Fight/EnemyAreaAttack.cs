using UnityEngine;
using Gamemanager;

public class EnemyDelayedAoE : EnemyAttackBase
{
    [Header("★ 攻擊資料配置")]
    [SerializeField] private DelayedAoEDataSO attackData;

    [Header("★ 嚴格傷害追蹤 (Debug)")]
    [Tooltip("開啟後，會印出從『掃描』到『扣血』的完整過程，幫你抓出誰沒吃到傷害")]
    [SerializeField] private bool enableDamageDebug = false;

    private readonly Collider[] _hitColliders = new Collider[16];
    private bool _hasExploded = false;

    public override void Initialize(BossStateMachine boss, float damageMultiplier = 1f, bool ignoreInvincibility = false)
    {
        float finalMultiplier = (attackData != null) ? attackData.damageMultiplier * damageMultiplier : damageMultiplier;
        bool finalIgnore = (attackData != null && attackData.ignoreInvincibility) || ignoreInvincibility;
        base.Initialize(boss, finalMultiplier, finalIgnore);
    }

    // ★ 提供給 Animator Event 呼叫：只算傷害，不銷毀物件
    public void Explode()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        if (attackData == null) return;

        // 如果你有另外獨立的 Hit 特效，可以在這裡生成
        if (attackData.hitEffectPrefab != null)
        {
            Instantiate(attackData.hitEffectPrefab, transform.position, Quaternion.identity);
        }

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
    }

    // ★ 提供給 Animator Event 呼叫：動畫播完後，負責清理記憶體
    public void DestroySelf()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        if (attackData != null)
        {
            attackData.damageShape.DrawGizmo(transform.position, transform.rotation, new Color(1f, 0.5f, 0f, 0.5f));
        }
    }
}