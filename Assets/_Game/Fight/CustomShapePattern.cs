using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 空間自定義發射器：讀取預先擺設好的 Transform 點位來發射子彈。
/// </summary>
public class CustomShapePattern : AttackPatternBase
{
    public enum FireDirectionMode
    {
        UseChildRotation,  // 嚴格依照子物件本身的旋轉角度 (Transform.up)
        OutwardFromCenter, // 以父節點為中心，向外散射
        FixedDirection     // 強制覆蓋，所有點位朝同一絕對方向發射
    }

    [Header("自定義形狀設定")]
    public GameObject bulletPrefab;
    public float baseSpeed = 5f;

    [Tooltip("決定子彈飛行的方向")]
    public FireDirectionMode directionMode = FireDirectionMode.UseChildRotation;

    [Tooltip("若 directionMode 為 FixedDirection，則套用此方向向量")]
    public Vector2 fixedDirection = Vector2.down;

    [Header("生成點清單")]
    public List<Transform> spawnPoints = new List<Transform>();

    // --- 編輯器輔助功能 ---
    [ContextMenu("自動抓取子物件 (Auto Get Children)")]
    public void AutoGetChildren()
    {
        spawnPoints.Clear();
        foreach (Transform child in transform)
        {
            spawnPoints.Add(child);
        }
        Debug.Log($"已抓取 {spawnPoints.Count} 個生成點！");
    }

    // --- 編輯器視覺化：在 Scene 視窗畫出預覽點與方向線 ---
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
            {
                Gizmos.DrawSphere(point.position, 0.2f);
                Vector3 dir = GetDirection(point);
                Gizmos.DrawLine(point.position, point.position + dir * 1.5f);
            }
        }
    }

    protected override void OnExecute(BossStateMachine boss, float speedMultiplier, bool isAngry)
    {
        float finalSpeed = baseSpeed * speedMultiplier;
        if (isAngry) finalSpeed *= 1.5f;

        // ★ 修正：Prefab 防呆
        if (bulletPrefab == null)
        {
            Debug.LogError($"❌ [{name}] 沒放 Bullet Prefab！", this);
            FinishPattern();
            return;
        }

        // ★ 修正：脫離 Boss 子物件層級，避免 Boss 移動時生成點跟著亂跑
        transform.SetParent(null);

        foreach (Transform point in spawnPoints)
        {
            if (point == null) continue;

            // 1. 在指定的空間節點生成實體
            GameObject bullet = Instantiate(bulletPrefab, point.position, Quaternion.identity);

            // 2. 依據列舉模式計算方向
            Vector2 dir = GetDirection(point);

            // 3. 呼叫統一介面，啟動子彈物理與資料綁定
            EnemyProjectileBase script = bullet.GetComponent<EnemyProjectileBase>();
            if (script != null)
            {
                script.Initialize(dir, finalSpeed, boss);
            }
            
            if (boss != null) boss.RegisterActiveBullet(bullet);
        }

        // ★ 修正：執行完畢後統一結束（會依 destroyOnFinish 決定是否銷毀自己）
        FinishPattern();
    }

    // ==========================================
    // ★ 實作父類別的抽象方法：編輯器預覽
    // ==========================================
    protected override void OnGeneratePreview(Transform previewContainer)
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning($"[CustomShapePattern] {name} 沒有任何生成點，請先按「自動抓取子物件」。");
            return;
        }

        int validCount = 0;
        foreach (Transform point in spawnPoints)
        {
            if (point == null) continue;
            Vector3 dir = GetDirection(point);
            CreatePreviewDummy(previewContainer, point.position, dir);
            validCount++;
        }

        Debug.Log($"<color=cyan>[預覽成功]</color> CustomShapePattern 畫出 {validCount} 個發射點。");
    }

    // 依據 DirectionMode 裁決該點位的發射方向
    private Vector3 GetDirection(Transform point)
    {
        switch (directionMode)
        {
            case FireDirectionMode.UseChildRotation:
                return point.up; // 依賴 Transform 的綠色箭頭 Y 軸
            case FireDirectionMode.OutwardFromCenter:
                return (point.position - transform.position).normalized;
            case FireDirectionMode.FixedDirection:
                return ((Vector3)fixedDirection).normalized;
        }
        return Vector2.down;
    }
}