using UnityEngine;
using System.Collections;

public class ShockwaveEffect : MonoBehaviour
{
    [Header("爆炸設定")]
    public float explosionRadius = 15f;
    public float explosionForce = 30f;
    
    [Tooltip("只炸這個 Layer 的物件，提升效能")]
    public LayerMask targetLayer;

    [Header("時序設定")]
    [Tooltip("生成後等待幾秒才真正引爆？ (給玩家的反應時間)")]
    public float delayBeforeExplosion = 1.0f;

    public bool enableDebugLog = true;

    void Start()
    {
        // 技能一被生成，立刻開始執行倒數引爆的流程
        StartCoroutine(ExplosionRoutine());
    }

    private IEnumerator ExplosionRoutine()
    {
        // ==========================================
        // 階段 1: 蓄力與警告期 (等待時間)
        // 你可以在這裡加入播放音效、放大警告圈特效的邏輯
        // ==========================================
        
        yield return new WaitForSeconds(delayBeforeExplosion);

        // ==========================================
        // 階段 2: 瞬間引爆，結算物理推力
        // ==========================================
        Detonate();

        // ==========================================
        // 階段 3: 功成身退
        // 延遲一點點時間讓粒子特效播完，然後自我銷毀
        // ==========================================
        Destroy(gameObject, 2f); 
    }

    private void Detonate()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, targetLayer);
        
        foreach (Collider col in colliders)
        {
            Rigidbody rb = col.attachedRigidbody;

            if (rb != null && !rb.isKinematic)
            {
                Vector3 pushDirection = rb.position - transform.position;
                pushDirection.z = 0; 
                pushDirection = pushDirection.normalized;

                Vector3 finalForce = pushDirection * explosionForce;

                rb.linearVelocity = Vector3.zero; 
                rb.AddForce(finalForce, ForceMode.VelocityChange);

                if (enableDebugLog)
                {
                    Debug.Log($"<color=cyan>[純物理衝擊波] 成功炸飛: {rb.gameObject.name}</color>");
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
    }
}