using UnityEngine;
using System.Collections.Generic;

public class CustomShapePattern : AttackPatternBase
{
    public enum FireDirectionMode
    {
        UseChildRotation, // 跟隨子物件的旋轉方向 (最自由，想射哪就轉哪)
        OutwardFromCenter, // 從中心向外輻射 (像爆炸)
        FixedDirection    // 全部朝同一個方向 (例如全部向下)
    }

    [Header("自定義形狀設定")]
    public GameObject bulletPrefab;
    public float baseSpeed = 5f;

    [Tooltip("決定子彈飛行的方向")]
    public FireDirectionMode directionMode = FireDirectionMode.UseChildRotation;

    [Tooltip("如果選 FixedDirection，要朝哪個方向飛？(例如 0,-1 是向下)")]
    public Vector2 fixedDirection = Vector2.down;

    [Header("生成點清單")]
    [Tooltip("請把擺好位置的子物件拖進來，或是按右鍵選 '自動抓取子物件'")]
    public List<Transform> spawnPoints = new List<Transform>();

    // --- 右鍵選單功能：自動抓取所有子物件 ---
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

    // --- 視覺化輔助：在 Scene 視窗畫出子彈位置 ---
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
            {
                // 畫出生成點的位置
                Gizmos.DrawSphere(point.position, 0.2f);
                
                // 畫出預計飛行方向
                Vector3 dir = GetDirection(point);
                Gizmos.DrawLine(point.position, point.position + dir * 1.5f);
            }
        }
    }

    protected override void OnExecute(BossBase boss, float speedMultiplier, bool isAngry)
    {
        float finalSpeed = baseSpeed * speedMultiplier;
        if (isAngry) finalSpeed *= 1.5f;

        // 遍歷所有設定好的點，生成子彈
        foreach (Transform point in spawnPoints)
        {
            if (point == null) continue;

            // 1. 生成子彈
            GameObject bullet = Instantiate(bulletPrefab, point.position, Quaternion.identity);

            // 2. 計算方向
            Vector2 dir = GetDirection(point);

            // 3. 初始化並註冊
            EnemyProjectileBase script = bullet.GetComponent<EnemyProjectileBase>();
            if (script != null)
            {
                // 這裡可以選擇是否要穿透，或者讓 EnemyBullet 讀取自己的預設值
                script.Initialize(dir, finalSpeed);
            }
            
            boss.RegisterActiveBullet(bullet);
        }
    }

    // 輔助計算方向
    private Vector3 GetDirection(Transform point)
    {
        switch (directionMode)
        {
            case FireDirectionMode.UseChildRotation:
                // 使用子物件的紅色軸 (right) 或綠色軸 (up)
                // 假設你的子彈圖片頭朝右，就用 right；頭朝上，就用 up
                // 這裡假設子彈邏輯是朝向 Transform.right (0度) 或 up (90度)
                // 為了配合 EnemyBullet 的 Atan2 計算，通常用 Up 或 Right
                return point.up; // 或是 point.right，看你的習慣

            case FireDirectionMode.OutwardFromCenter:
                return (point.position - transform.position).normalized;

            case FireDirectionMode.FixedDirection:
                return fixedDirection.normalized;
        }
        return Vector2.down;
    }
}