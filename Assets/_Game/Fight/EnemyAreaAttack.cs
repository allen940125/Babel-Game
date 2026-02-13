using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CircleCollider2D))]
// ★ 修改繼承：繼承自 EnemyAttackObject (爺爺)，而不是 EnemyProjectileBase (爸爸)
public class EnemyTurretAttack : EnemyAttackObject
{
    [Header("範圍偵測設定")]
    [Tooltip("要攻擊的目標圖層 (必須設定！例如 Player)")]
    public LayerMask targetLayer; 
    
    // ★ damageAmount 已經在父類別有了，這裡刪除！

    [Header("特效設定")]
    public GameObject hitEffectPrefab;

    private Animator _animator;
    private CircleCollider2D _myCollider;
    private bool _hasExploded = false;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _myCollider = GetComponent<CircleCollider2D>();
        
        // 關閉碰撞器，只用它的數據來做 OverlapCircle
        _myCollider.enabled = false;
        _myCollider.isTrigger = true;
    }

    private void Update()
    {
        if (_hasExploded) return;
        
        // 檢查動畫進度
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.normalizedTime >= 1.0f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        _hasExploded = true;

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // ★ 核心邏輯：使用 Physics2D.OverlapCircleAll 進行判定
        // 使用 Collider 的半徑 * Scale 來取得實際範圍
        float radius = _myCollider.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, targetLayer);

        foreach (var hit in hits)
        {
            // ★ 直接呼叫父類別方法！
            TryDealDamage(hit);
        }

        Destroy(gameObject);
    }
    
    // 這裡還可以加上 Gizmos 來方便在編輯器裡看範圍
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        if (GetComponent<CircleCollider2D>() != null)
        {
            float r = GetComponent<CircleCollider2D>().radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
            Gizmos.DrawSphere(transform.position, r);
        }
    }
}