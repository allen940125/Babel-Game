using UnityEngine;

public class ShockwaveEffect : MonoBehaviour
{
    [Header("爆炸設定")]
    public float explosionRadius = 15f;
    public float explosionForce = 30f;
    
    [Tooltip("只炸這個 Layer 的物件，提升效能")]
    public LayerMask targetLayer;

    public bool enableDebugLog = true;

    void Start()
    {
        // ★ 核心防呆：給事件中心一點點時間 (0.05秒) 
        // 確保所有按鈕都已經收到 Transition 事件並解開 isKinematic，然後再引爆
        Invoke(nameof(Detonate), 0.05f);
    }

    private void Detonate()
    {
        // 1. 透過 Layer 過濾，找出範圍內的所有 Collider
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, targetLayer);
        
        foreach (Collider col in colliders)
        {
            // 2. ★ 完全解耦：不看腳本，只找有沒有 Rigidbody！
            // 使用 attachedRigidbody 可以自動找到父節點上的剛體，非常安全
            Rigidbody rb = col.attachedRigidbody;

            if (rb != null)
            {
                // 3. 防呆檢查：確保它真的已經被事件中心解鎖了
                if (rb.isKinematic)
                {
                    if (enableDebugLog) Debug.LogWarning($"<color=orange>[衝擊波] {rb.gameObject.name} 還是 Kinematic，推不動！可能是事件慢了一步。</color>");
                    continue; 
                }

                // 4. 計算 2.5D 平面外推方向
                Vector3 pushDirection = rb.position - transform.position;
                pushDirection.z = 0; 
                pushDirection = pushDirection.normalized;

                Vector3 finalForce = pushDirection * explosionForce;

                // 5. 暴力推開 (無視質量)
                rb.linearVelocity = Vector3.zero; // 推之前先煞車
                rb.AddForce(finalForce, ForceMode.VelocityChange);

                if (enableDebugLog)
                {
                    Debug.Log($"<color=cyan>[純物理衝擊波] 成功炸飛: {rb.gameObject.name}</color>");
                    Debug.DrawRay(rb.position, finalForce, Color.red, 2f);
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