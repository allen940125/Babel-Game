using UnityEngine;
using System.Collections.Generic;

public class BossCleaner : BossBase
{
    [Header("★ Cleaner 專屬特殊攻擊設定")]
    [Tooltip("請拖入 Cleaner 的特殊大招 Prefab (例如：定時雷射塔、毒氣產生器、追蹤旋轉鋸齒)")]
    public GameObject specialAttackPrefab; 
    
    [Header("生成座標與防重疊設定")]
    [Tooltip("每次發動特殊攻擊時，要在畫面上隨機生成幾個大招物件？")]
    [SerializeField] private int spawnCount = 3;
    [Tooltip("生成時距離螢幕邊緣的安全距離")]
    public float spawnPadding = 1.0f;
    [Tooltip("物件之間的最小距離 (防重疊)")]
    public float minObjectDistance = 2.0f; 
    
    // ==========================================
    // ★ 實作父類別要求：在 Attacking 階段與普通子彈波次同時發動！
    // ==========================================
    protected override void SpawnSpecialMechanisms()
    {
        if (specialAttackPrefab == null)
        {
            Debug.LogWarning($"[{bossName}] 未綁定 specialAttackPrefab，略過特殊大招發射！");
            return;
        }

        // 1. 取得主攝影機邊界 (嚴格轉換為 3D 世界座標)
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindFirstObjectByType<Camera>();

        Vector3 minScreen = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
        Vector3 maxScreen = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));

        // 內縮安全區域 (Padding)
        float minX = minScreen.x + spawnPadding;
        float maxX = maxScreen.x - spawnPadding;
        float minY = minScreen.y + spawnPadding;
        float maxY = maxScreen.y - spawnPadding;

        List<Vector3> spawnedPositions = new List<Vector3>();

        Debug.Log($"<color=cyan>[{bossName}] 發動特殊大招！在場上生成 {spawnCount} 個脅迫物件！</color>");

        // 2. 迴圈生成指定數量的特殊攻擊物件
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 finalPos = transform.position;
            bool foundValidPosition = false;
            int maxAttempts = 20;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // ★ 嚴格物理規範：X/Y 隨機，但 Z 軸絕對鎖死在 0f！
                float randomX = Random.Range(minX, maxX);
                float randomY = Random.Range(minY, maxY);
                Vector3 candidatePos = new Vector3(randomX, randomY, 0f);

                bool isTooClose = false;

                // 防重疊 A：檢查與已生成大招之間的距離
                foreach (Vector3 existingPos in spawnedPositions)
                {
                    if (Vector3.Distance(candidatePos, existingPos) < minObjectDistance)
                    {
                        isTooClose = true;
                        break;
                    }
                }

                // 防重疊 B：檢查與 Boss 本體的距離 (避免大招直接疊在 Boss 臉上)
                if (!isTooClose && Vector3.Distance(candidatePos, transform.position) < minObjectDistance)
                {
                    isTooClose = true;
                }

                if (!isTooClose)
                {
                    finalPos = candidatePos;
                    foundValidPosition = true;
                    break;
                }
            }
            
            spawnedPositions.Add(finalPos);

            // 3. 生成特殊大招實體 (例如雷射塔、污染區)
            GameObject specialObj = Instantiate(specialAttackPrefab, finalPos, Quaternion.identity);

            // ★ 4. 關鍵解耦與收斂：
            // 直接把它註冊進父類別的 _activePatterns 清單裡！
            // 這樣當 15 秒一到，或 Boss 死亡時，中央的 ClearAllActiveProjectiles() 會自動把它們乾淨銷毀！
            RegisterActivePattern(specialObj);
            
            // 如果你這個特殊大招 Prefab 也有繼承 AttackPatternBase，甚至能直接叫它執行發射
            if (specialObj.TryGetComponent(out AttackPatternBase patternScript))
            {
                patternScript.Execute(this, 1.0f, true);
            }
        }
    }
}