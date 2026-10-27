using System.Collections.Generic;
using UnityEngine;

// ==========================================
// 2. 飛行子彈專屬資料 (具備物理移動、反彈、碰撞形狀)
// ==========================================
[CreateAssetMenu(fileName = "New Bullet Data", menuName = "Combat/Bullet Data")]
public class BulletDataSO : AttackDataSO
{
    [Header("★ 反彈判定 (對牆壁)")]
    [Tooltip("通常與視覺圖像等大，確保不會穿模")]
    public CollisionShapeConfig bounceShape;
    public LayerMask bounceLayer;
    
    [Header("★ 飛行與反彈行為")]
    public float baseSpeed = 10f;
    public int maxBounces = 3;
    public float maxPredictionDistance = 50f;

    [Header("★ 反彈擾動 (Jitter)")]
    [Range(0f, 60f)] public float maxBounceAngleJitter = 0f;
    public bool jitterFirstBounceOnly = false;
    
    // ★ 實作預測軌跡 (把你原本寫在 TrajectoryVisualizer 裡面的長串迴圈搬過來)
    public override Vector3[] CalculatePredictedTrajectory(Vector3 startPos, Vector3 direction)
    {
        List<Vector3> points = new List<Vector3>();
        points.Add(startPos);
        
        float remainingDistance = maxPredictionDistance;
        Vector3 currentPos = startPos;
        Vector3 currentDir = direction.normalized;
        
        // 為了效能，我們在這裡宣告一個暫存的 HitBuffer
        RaycastHit[] tempBuffer = new RaycastHit[4]; 

        for (int i = 0; i <= maxBounces; i++)
        {
            if (remainingDistance <= 0.0001f) break;

            int hitCount = ShapeCastUtility.PerformCast(
                currentPos, currentDir, remainingDistance,
                tempBuffer, bounceShape, bounceLayer, false
            );

            RaycastHit nearestHit = default;
            bool foundWall = false;

            if (hitCount > 0)
            {
                System.Array.Sort(tempBuffer, 0, hitCount, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));
                for (int j = 0; j < hitCount; j++)
                {
                    if (tempBuffer[j].collider != null && tempBuffer[j].distance > 0.0001f)
                    {
                        nearestHit = tempBuffer[j];
                        foundWall = true;
                        break;
                    }
                }
            }

            if (!foundWall)
            {
                Vector3 endPoint = currentPos + currentDir * remainingDistance;
                endPoint.z = 0f;
                points.Add(endPoint);
                break;
            }

            Vector3 hitPoint = currentPos + currentDir * nearestHit.distance;
            hitPoint.z = 0f;
            points.Add(hitPoint);
            remainingDistance -= nearestHit.distance;

            Vector3 flatNormal = new Vector3(nearestHit.normal.x, nearestHit.normal.y, 0f).normalized;
            currentDir = Vector3.Reflect(currentDir, flatNormal).normalized;
            currentPos = hitPoint + flatNormal * 0.01f;
        }

        return points.ToArray();
    }
    
    // 寫在 BulletDataSO 裡面
#if UNITY_EDITOR
    [ContextMenu("★ 從場景中選取的物件讀取 Collider 數值")]
    public void SyncFromSelectedObject()
    {
        GameObject selectedObj = UnityEditor.Selection.activeGameObject;
        if (selectedObj == null)
        {
            Debug.LogWarning("請先在場景或 Hierarchy 中選取一個帶有 Collider 的物件！");
            return;
        }

        Collider col = selectedObj.GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning("選取的物件身上沒有 Collider！");
            return;
        }

        UnityEditor.Undo.RecordObject(this, "Sync Collider Data");

        if (col is SphereCollider sc)
        {
            bounceShape.shapeType = BulletShapeType.Circle;
            bounceShape.radius = sc.radius * Mathf.Max(selectedObj.transform.lossyScale.x, selectedObj.transform.lossyScale.y);
        }
        else if (col is BoxCollider bc)
        {
            bounceShape.shapeType = BulletShapeType.Box;
            bounceShape.boxSize = new Vector2(bc.size.x * selectedObj.transform.lossyScale.x, bc.size.y * selectedObj.transform.lossyScale.y);
            bounceShape.zThickness = bc.size.z * selectedObj.transform.lossyScale.z;
        }
        else if (col is CapsuleCollider cc)
        {
            bounceShape.shapeType = BulletShapeType.Capsule;
            bounceShape.radius = cc.radius * Mathf.Max(selectedObj.transform.lossyScale.x, selectedObj.transform.lossyScale.z);
            bounceShape.capsuleLength = cc.height * selectedObj.transform.lossyScale.y;
        }

        // 將 Damage 形狀也同步 (視你的需求而定)
        damageShape = bounceShape;

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"<color=#4EC9B0>[SO 更新成功]</color> 已成功從 {selectedObj.name} 擷取 Collider 資料寫入 SO！建議現在將該物件身上的 Collider 移除。");
    }
#endif
}