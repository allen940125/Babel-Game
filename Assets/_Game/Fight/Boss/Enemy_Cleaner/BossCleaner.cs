using UnityEngine;
using System.Collections.Generic;

public class BossCleaner : BossBase
{
    [Header("Cleaner 專屬設定")]
    [Tooltip("必須掛有 BossSpecialMechanism 腳本")]
    public GameObject SpecialPrefab; 
    
    [Header("生成設定")]
    [Tooltip("生成時距離螢幕邊緣的安全距離")]
    public float spawnPadding = 1.0f;
    [Tooltip("物件之間的最小距離 (防重疊)")]
    public float minObjectDistance = 2.0f; 
    
    // --- 保留：這才是 Cleaner 獨有的特色 (生成特殊機關) ---
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

        // 用來記錄這一輪已經生成的座標 (防重疊)
        List<Vector2> spawnedPositions = new List<Vector2>();

        // 生成 3 個機關 (你可以把 3 改成變數)
        for (int i = 0; i < 3; i++)
        {
            Vector2 finalPos = transform.position;
            bool foundValidPosition = false;
            int maxAttempts = 20;

            // 嘗試尋找合法位置
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // A. 全螢幕隨機座標
                float randomX = Random.Range(minScreen.x, maxScreen.x);
                float randomY = Random.Range(minScreen.y, maxScreen.y);
                Vector2 candidatePos = new Vector2(randomX, randomY);

                // B. 防重疊檢查
                bool isTooClose = false;

                // 檢查跟其他機關的距離
                foreach (Vector2 existingPos in spawnedPositions)
                {
                    if (Vector2.Distance(candidatePos, existingPos) < minObjectDistance)
                    {
                        isTooClose = true;
                        break;
                    }
                }

                // 檢查跟 Boss 本體的距離 (選用，避免生在 Boss 臉上)
                if (!isTooClose)
                {
                     if (Vector2.Distance(candidatePos, transform.position) < minObjectDistance)
                     {
                         isTooClose = true;
                     }
                }

                // 合法確認
                if (!isTooClose)
                {
                    finalPos = candidatePos;
                    foundValidPosition = true;
                    break;
                }
            }
            
            // 記錄位置
            spawnedPositions.Add(finalPos);

            // 生成物件
            GameObject obj = Instantiate(SpecialPrefab, finalPos, Quaternion.identity);

            // 加入父類別清單
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
}