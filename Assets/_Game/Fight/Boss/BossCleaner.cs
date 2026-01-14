using UnityEngine;
using System.Collections.Generic;

public class BossCleaner : BossBase
{
    [Header("Cleaner 專屬設定")]
    public GameObject SpecialPrefab; // 必須掛有 BossSpecialMechanism 腳本
    
    [Header("生成設定")]
    [Tooltip("生成時距離螢幕邊緣的安全距離")]
    public float spawnPadding = 1.0f;
    [Tooltip("物件之間的最小距離 (防重疊)")]
    public float minObjectDistance = 2.0f; // 建議設為 Prefab 的直徑或更大一點
    
    private List<GameObject> _activeBullets = new List<GameObject>();

    // 1. 實作發射子彈 (參數由父類別傳入)
    protected override void FireBulletInstance(GameObject prefab, float speed, Color color)
    {
        // 生成子彈
        GameObject bulletObj = Instantiate(prefab, firePosition.position, Quaternion.identity);
        
        // 設定顏色
        var sprite = bulletObj.GetComponentInChildren<SpriteRenderer>();
        if(sprite) sprite.color = color;
        
        // 設定物理/邏輯
        EnemyBullet bulletScript = bulletObj.GetComponent<EnemyBullet>();
        if (bulletScript != null)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            // 初始化 (使用傳入的速度)
            bulletScript.Initialize(randomDir, speed); 
        }

        _activeBullets.Add(bulletObj);
    }

    // 2. 實作生成特殊機關
    protected override void EnterSpecialPhase()
    {
        // 1. 計算螢幕邊界 (世界座標)
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindFirstObjectByType<Camera>();

        Vector2 minScreen = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector2 maxScreen = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));

        // 內縮邊界 (Padding)
        minScreen += new Vector2(spawnPadding, spawnPadding);
        maxScreen -= new Vector2(spawnPadding, spawnPadding);

        List<Vector2> spawnedPositions = new List<Vector2>();

        for (int i = 0; i < 3; i++)
        {
            Vector2 finalPos = transform.position;
            bool foundValidPosition = false;
            int maxAttempts = 20;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // --- 修改重點 A: 改成全螢幕隨機 ---
                // 不再參考 transform.position，而是直接在螢幕範圍內隨機
                float randomX = Random.Range(minScreen.x, maxScreen.x);
                float randomY = Random.Range(minScreen.y, maxScreen.y);
                Vector2 candidatePos = new Vector2(randomX, randomY);

                // --- 修改重點 B: 防重疊檢查 ---
                bool isTooClose = false;

                // 1. 檢查有沒有跟「其他機關」太近
                foreach (Vector2 existingPos in spawnedPositions)
                {
                    if (Vector2.Distance(candidatePos, existingPos) < minObjectDistance)
                    {
                        isTooClose = true;
                        break;
                    }
                }

                // 2. (選用) 檢查有沒有跟「Boss 本體」太近？
                // 如果你不希望機關直接生在 Boss 臉上，這段很重要
                if (!isTooClose)
                {
                     if (Vector2.Distance(candidatePos, transform.position) < minObjectDistance)
                     {
                         isTooClose = true;
                     }
                }

                // 合法位置確認
                if (!isTooClose)
                {
                    finalPos = candidatePos;
                    foundValidPosition = true;
                    break;
                }
            }

            // 萬一隨機 20 次都失敗 (極低機率)，還是要生出來，就用最後一次算出的點
            // (雖然這裡 finalPos 預設是 Boss 位置，但理論上 randomX/Y 會覆蓋它，除非 min>max)
            
            spawnedPositions.Add(finalPos);

            // 生成物件
            GameObject obj = Instantiate(SpecialPrefab, finalPos, Quaternion.identity);

            var mechanism = obj.GetComponent<BossSpecialMechanism>();
            if (mechanism != null)
            {
                _activeSpecialMechanisms.Add(mechanism);
            }
            else
            {
                Debug.LogError("SpecialPrefab 缺少 BossSpecialMechanism 腳本！");
            }
        }
    }

    // 3. 實作檢查子彈是否清空
    protected override bool CheckBulletsCleared()
    {
        _activeBullets.RemoveAll(item => item == null);
        return _activeBullets.Count == 0;
    }
}