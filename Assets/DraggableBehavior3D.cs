using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DraggableBehavior3D : MonoBehaviour, IDragHandler3D
{
    [Header("狀態設定")]
    [SerializeField] private bool isDraggable = true;
    [SerializeField] private float dropDetectRadius = 0.5f;

    [Header("偵錯專區 (Inspector 操作)")]
    [SerializeField] private bool showDetectionSphere = true;
    [SerializeField] private Color debugSphereColor = new Color(1f, 0.5f, 0f, 0.4f);

    private Vector3 _offset;

    // ★ 嚴格生命週期同步：當組件被 enabled = false 時，強制同步關閉內部旗標！
    private void OnEnable() => isDraggable = true;
    private void OnDisable() => isDraggable = false;

    public void OnDragStart(Vector3 hitPoint)
    {
        // ★ 核心修復：同時檢查 !this.enabled 與 !isDraggable！
        if (!this.enabled || !isDraggable)
        {
            Debug.LogWarning($"[權限攔截] {gameObject.name} 被點擊，但組件已停用 (enabled={this.enabled}) 或 isDraggable=false，拒絕拖曳！");
            return;
        }
        _offset = transform.position - hitPoint;
    }

    public void OnDrag(Vector3 targetWorldPosition)
    {
        // ★ 核心修復：任何一者關閉，立即中斷位移！
        if (!this.enabled || !isDraggable) return;
        transform.position = targetWorldPosition + _offset;
    }

    public void OnDragEnd()
    {
        if (!this.enabled || !isDraggable) return;
        CheckDropCollision3D();
    }
    
    private void CheckDropCollision3D()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, dropDetectRadius);
        Debug.Log($"[物理偵測] {gameObject.name} 放開，偵測範圍內共有 {hits.Length} 個 Collider。");

        foreach (var hit in hits)
        {
            if (hit.gameObject == this.gameObject) continue;

            BossSpecialMechanism mechanism = hit.GetComponentInParent<BossSpecialMechanism>();
            if (mechanism != null)
            {
                Debug.Log($"<color=green>[成功觸發]</color> 找到機關：{mechanism.name}，發送手動觸發訊號！");
                mechanism.ManualTrigger(this.gameObject);
                return;
            }
            else
            {
                Debug.Log($"[忽略目標] 抓到 {hit.name}，但其父子層級中沒有 BossSpecialMechanism 組件。");
            }
        }
        Debug.LogWarning($"[觸發落空] {gameObject.name} 範圍內沒有任何有效的 BossSpecialMechanism。");
    }

    public void SetDraggable(bool state)
    {
        isDraggable = state;
        Debug.Log($"[狀態變更] {gameObject.name} 的 isDraggable 被切換為: {state}");
    }

    // --- 右鍵選單強制執行工具 ---
    [ContextMenu("偵錯：強制執行放下偵測 (Manual Check)")]
    public void DebugManualDropCheck()
    {
        Debug.Log("=== 執行 Inspector 強制放下偵測 ===");
        CheckDropCollision3D();
    }

    [ContextMenu("偵錯：切換拖曳權限 (Toggle Draggable)")]
    public void DebugToggleDraggable()
    {
        SetDraggable(!isDraggable);
    }

    // --- Scene View 視覺化 ---
    private void OnDrawGizmosSelected()
    {
        if (!showDetectionSphere) return;

        Gizmos.color = debugSphereColor;
        Gizmos.DrawSphere(transform.position, dropDetectRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, dropDetectRadius);
    }
}