using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 空間自定義發射器：讀取預先擺設好的 Transform 點位來發射子彈。
/// </summary>
public class CustomShapePattern : BulletSpawnerBase // ★ 修正：改繼承 BulletSpawnerBase
{
    public enum FireDirectionMode
    {
        UseChildRotation,  // 嚴格依照子物件本身的旋轉角度 (Transform.up)
        OutwardFromCenter, // 以父節點為中心，向外散射
        FixedDirection     // 強制覆蓋，所有點位朝同一絕對方向發射
    }

    [Header("自定義形狀設定")]
    // ❌ 刪除了 bulletPrefab 和 baseSpeed，因為父類別 (BulletSpawnerBase) 已經有了！

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

    // ==========================================
    // ★ 實作 BulletSpawnerBase 的抽象方法：只算數學！
    // ==========================================
    protected override List<SpawnData> CalculateAllSpawnData()
    {
        List<SpawnData> list = new List<SpawnData>();

        foreach (Transform point in spawnPoints)
        {
            if (point == null) continue;

            // 把計算好的點位與方向塞進名單，剩下的生成與預警交給基底類別處理
            list.Add(new SpawnData { 
                position = point.position, 
                direction = GetDirection(point) 
            });
        }

        return list;
    }

    // ==========================================
    // ★ 編輯器預覽：複用 CalculateAllSpawnData 確保與實戰一致
    // ==========================================
    protected override void OnGeneratePreview(Transform previewContainer)
    {
        var dataList = CalculateAllSpawnData();
        if (dataList.Count == 0)
        {
            Debug.LogWarning($"[CustomShapePattern] {name} 沒有任何生成點，請先按「自動抓取子物件」。");
            return;
        }

        foreach (var data in dataList)
        {
            CreatePreviewDummy(previewContainer, data.position, data.direction);
        }

        Debug.Log($"<color=cyan>[預覽成功]</color> CustomShapePattern 畫出 {dataList.Count} 個發射點。");
    }

    // ==========================================
    // Debug 測試：直接呼叫基底 Execute
    // ==========================================
    [ContextMenu("👉 測試發射 (Debug Test)")]
    public void DebugTest()
    {
        if (!Application.isPlaying) { Debug.LogError("⛔ 請先按 Play！"); return; }
        // 走完整流程（含預警、逐發、結束）
        Execute(null, 1f, false);
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