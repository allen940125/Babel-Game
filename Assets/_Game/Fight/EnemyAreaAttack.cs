using UnityEngine;

[RequireComponent(typeof(DamageDealer))]
public class EnemyTurretAttack : MonoBehaviour
{
    [Header("範圍偵測設定")]
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private float radius = 3f;

    [Header("特效設定")]
    [SerializeField] private GameObject hitEffectPrefab;

    private DamageDealer _damageDealer;
    private readonly Collider[] _hitColliders = new Collider[16];
    private bool _hasExploded = false;

    private void Awake()
    {
        _damageDealer = GetComponent<DamageDealer>();
    }

    public void Initialize(BossStateMachine boss, float damageMultiplier = 1f)
    {
        if (boss != null)
        {
            var core = boss.GetComponent<EntityCore>();
            if (core != null && core.RuntimeData != null)
            {
                _damageDealer.InjectSnapshot(
                    core.RuntimeData.TotalAttackPower * damageMultiplier,
                    core.RuntimeData.TotalCritRate,
                    core.RuntimeData.TotalCritMultiplier,
                    false
                );
            }
        }
    }

    // 由 Animation Event 觸發
    public void Explode()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, radius, _hitColliders, targetLayer);
        for (int i = 0; i < hitCount; i++)
        {
            _damageDealer.DealDamageTo(_hitColliders[i].gameObject);
        }

        Destroy(gameObject);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f); // 半透明紅色
        Gizmos.DrawSphere(transform.position, radius);
    }
}