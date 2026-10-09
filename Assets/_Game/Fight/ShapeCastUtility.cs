using UnityEngine;

public enum BulletShapeType { Circle, Box, Capsule }

[System.Serializable]
public struct CollisionShapeConfig
{
    public BulletShapeType shapeType;
    [Tooltip("圓形半徑 / 膠囊體半徑")] public float radius;
    [Tooltip("長方形尺寸 (X = 寬度, Y = 長度)")] public Vector2 boxSize;
    [Tooltip("長橢圓/膠囊體總長度 (沿著飛行方向)")] public float capsuleLength;

    [Header("★ 2D 寬容度設定")]
    [Tooltip("Z 軸的掃描厚度 (解決 2D 平面 Z 軸對不準的問題)")]
    public float zThickness;

    [Header("★ 幾何旋轉偏移")]
    [Tooltip("控制碰撞體在 XYZ 軸上的額外旋轉角度（相對於飛行方向）")]
    public Vector3 shapeRotationOffset;
    
    // ★ 新增：專屬的 Gizmo 繪製工具
    public void DrawGizmo(Vector3 position, Quaternion baseRotation, Color color)
    {
        Gizmos.color = color;
        
        // 算出最終旋轉角度
        Quaternion offsetRot = Quaternion.Euler(shapeRotationOffset);
        Quaternion finalRot = baseRotation * offsetRot;
        
        // 改變全域矩陣，這樣畫出來的基礎形狀就會自動帶有位置與旋轉
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(position, finalRot, Vector3.one);

        switch (shapeType)
        {
            case BulletShapeType.Circle:
                Gizmos.DrawWireSphere(Vector3.zero, radius);
                break;
                
            case BulletShapeType.Box:
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(boxSize.x, boxSize.y, zThickness > 0 ? zThickness : 0.5f));
                break;
                
            case BulletShapeType.Capsule:
                float halfLen = Mathf.Max(0f, (capsuleLength * 0.5f) - radius);
                Gizmos.DrawWireSphere(Vector3.up * halfLen, radius);
                Gizmos.DrawWireSphere(Vector3.down * halfLen, radius);
                Gizmos.DrawLine(Vector3.up * halfLen + Vector3.right * radius, Vector3.down * halfLen + Vector3.right * radius);
                Gizmos.DrawLine(Vector3.up * halfLen - Vector3.right * radius, Vector3.down * halfLen - Vector3.right * radius);
                break;
        }

        // 畫完務必還原矩陣，以免影響場景中其他 Gizmo
        Gizmos.matrix = oldMatrix;
    }
}

public static class ShapeOverlapUtility
{
    // ★ 專門用於靜態原地的範圍檢測，傳入 baseRotation 而不是 direction
    public static int PerformOverlap(
        Vector3 origin, Quaternion baseRotation, 
        Collider[] buffer, CollisionShapeConfig config,
        LayerMask layerMask, bool enableDebug = false)
    {
        int hitCount = 0;

        // 計算最終旋轉：物件本身的旋轉 * 設定檔的偏移
        Quaternion offsetRot = Quaternion.Euler(config.shapeRotationOffset);
        Quaternion finalRot = baseRotation * offsetRot;

        switch (config.shapeType)
        {
            case BulletShapeType.Circle:
            {
                // 圓形/球體不需要旋轉
                hitCount = Physics.OverlapSphereNonAlloc(origin, config.radius, buffer, layerMask);
                break;
            }

            case BulletShapeType.Box:
            {
                // Z 軸厚度在此處被視為 Box 的深度
                Vector3 halfExtents = new Vector3(
                    config.boxSize.x * 0.5f,
                    config.boxSize.y * 0.5f,
                    config.zThickness * 0.5f);

                hitCount = Physics.OverlapBoxNonAlloc(origin, halfExtents, buffer, finalRot, layerMask);
                break;
            }

            case BulletShapeType.Capsule:
            {
                float halfLen = Mathf.Max(0f, (config.capsuleLength * 0.5f) - config.radius);
                Vector3 axis = finalRot * Vector3.up; // 沿著旋轉後的 Y 軸展開
                Vector3 p1 = origin - axis * halfLen;
                Vector3 p2 = origin + axis * halfLen;

                hitCount = Physics.OverlapCapsuleNonAlloc(p1, p2, config.radius, buffer, layerMask);
                break;
            }
        }

        if (enableDebug)
        {
            // Debug 繪製邏輯可依需求補充
            Debug.Log($"[Overlap 掃描] 偵測到 {hitCount} 個目標。");
        }

        return hitCount;
    }
}

public static class ShapeCastUtility
{
    public static int PerformCast(
        Vector3 origin, Vector3 direction, float distance,
        RaycastHit[] buffer, CollisionShapeConfig config,
        LayerMask layerMask, bool enableDebug = false)
    {
        if (direction == Vector3.zero) return 0;

        int hitCount = 0;

        // ★ 把使用者的偏移轉成四元數（只算一次）
        Quaternion offsetRot = Quaternion.Euler(config.shapeRotationOffset);

        // ★ 計算「朝向飛行方向」的基礎旋轉
        //   LookRotation(forward, up) → 讓 Y 軸指向 direction
        Quaternion baseRot = Quaternion.LookRotation(Vector3.forward, direction);

        // ★ 最終旋轉：基礎 * 偏移（順序很重要，別顛倒）
        Quaternion finalRot = baseRot * offsetRot;

        switch (config.shapeType)
        {
            case BulletShapeType.Circle:
            {
                // 沿著（旋轉後的）Z 軸延伸，模擬有厚度的 2D 圓形
                Vector3 zAxis = finalRot * Vector3.forward;
                Vector3 p1 = origin + zAxis * (config.zThickness * 0.5f);
                Vector3 p2 = origin - zAxis * (config.zThickness * 0.5f);
                hitCount = Physics.CapsuleCastNonAlloc(p1, p2, config.radius, direction, buffer, distance, layerMask);
                break;
            }

            case BulletShapeType.Box:
            {
                Vector3 halfExtents = new Vector3(
                    config.boxSize.x * 0.5f,
                    config.boxSize.y * 0.5f,
                    config.zThickness * 0.5f);

                hitCount = Physics.BoxCastNonAlloc(
                    origin, halfExtents, direction, buffer,
                    finalRot, distance, layerMask);
                break;
            }

            case BulletShapeType.Capsule:
            {
                float halfLen = Mathf.Max(0f, (config.capsuleLength * 0.5f) - config.radius);

                // ★ 膠囊軸線 = 飛行方向經過旋轉偏移
                Vector3 axis = finalRot * Vector3.up;
                Vector3 cp1 = origin - axis * halfLen;
                Vector3 cp2 = origin + axis * halfLen;

                hitCount = Physics.CapsuleCastNonAlloc(cp1, cp2, config.radius, direction, buffer, distance, layerMask);
                break;
            }
        }

        if (enableDebug)
        {
            Debug.DrawRay(origin, direction * distance, hitCount > 0 ? Color.red : Color.green, 2.0f);
            if (hitCount > 0)
            {
                for (int i = 0; i < hitCount; i++)
                {
                    if (buffer[i].collider != null)
                        Debug.Log($"[掃描命中] 距離: {buffer[i].distance:F3} | 物件: {buffer[i].collider.name} | Tag: {buffer[i].collider.tag} | Layer: {LayerMask.LayerToName(buffer[i].collider.gameObject.layer)}");
                }
            }
            else
            {
                Debug.Log($"[掃描落空] 射線長度: {distance:F3} 範圍內無任何符合 LayerMask 的物體。");
            }
        }

        return hitCount;
    }
}