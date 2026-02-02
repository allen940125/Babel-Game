using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CircleCollider2D))] // 必定要有圓形碰撞體
public class EnemyTurretAttack : MonoBehaviour
{
    [Header("攻擊設定")]
    [Tooltip("傷害數值")]
    public int damageAmount = 1;
    
    [Tooltip("要攻擊的目標圖層 (必須設定！例如 Player)")]
    public LayerMask targetLayer;

    [Header("特效設定")]
    [Tooltip("爆炸時生成的特效")]
    public GameObject hitEffectPrefab;

    private Animator _animator;
    private CircleCollider2D _myCollider;
    private bool _hasExploded = false;

    // 用來暫存被炸到的人，避免重複傷害 (雖然這腳本只炸一次)
    private List<Collider2D> _hitResults = new List<Collider2D>();

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _myCollider = GetComponent<CircleCollider2D>();

        // ★ 重點 1：剛出生的時候先把「傷害判定」關掉！
        // 這樣玩家站在上面也不會受傷，直到我們說可以炸
        _myCollider.enabled = false;
        
        // 確保它是 Trigger (雖然用 OverlapCollider 沒差，但設定好比較保險)
        _myCollider.isTrigger = true;
    }

    private void Update()
    {
        if (_hasExploded) return;

        CheckAnimationStatus();
    }

    private void CheckAnimationStatus()
    {
        // 檢查動畫是否播完 (normalizedTime >= 1.0f)
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.normalizedTime >= 1.0f)
        {
            Explode();
        }
    }

    // ★ 核心爆炸邏輯
    private void Explode()
    {
        _hasExploded = true;

        // 1. 生成視覺特效
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // 2. ★ 關鍵時刻：主動使用 Collider 進行判定
        // 我們不需要把 _myCollider.enabled 設為 true 也能用這個方法！
        // 這是直接拿這個形狀去問物理系統：「現在誰在這個圈圈裡？」
        
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(targetLayer); // 只偵測目標圖層
        filter.useTriggers = true; // 是否要偵測 Trigger (看你需求，通常 Player 是 Trigger 就要開)

        // 進行掃描，結果會存入 _hitResults
        int hitCount = _myCollider.Overlap(filter, _hitResults);

        if (hitCount > 0)
        {
            foreach (var hit in _hitResults)
            {
                // 嘗試找 PlayerController
                PlayerController2D player = hit.GetComponent<PlayerController2D>();
                if (player == null) player = hit.GetComponentInParent<PlayerController2D>();

                if (player != null)
                {
                    player.TakeDamage(damageAmount);
                    Debug.Log($"自走砲炸到了 {hit.name}");
                }
            }
        }

        // 3. 炸完收工，銷毀自己
        Destroy(gameObject);
    }
}