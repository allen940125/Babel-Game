using UnityEngine;
using System.Collections.Generic;

public class BossCleaner : BossBase
{
    [Header("★ Cleaner 專屬地圖道具設定")]
    [Tooltip("請拖入會在地上隨機出現、讓玩家靠近或用按鈕撞擊來扣秒的道具 Prefab")]
    public GameObject specialAttackPrefab; 
    
    [Header("生成座標與防重疊設定")]
    [SerializeField] private int spawnCount = 3;
    public float spawnPadding = 1.0f;
    public float minObjectDistance = 2.0f; 
    
    protected override void SpawnSpecialMechanisms()
    {
        if (specialAttackPrefab == null) return;

        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindFirstObjectByType<Camera>();

        Vector3 minScreen = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
        Vector3 maxScreen = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));

        float minX = minScreen.x + spawnPadding;
        float maxX = maxScreen.x - spawnPadding;
        float minY = minScreen.y + spawnPadding;
        float maxY = maxScreen.y - spawnPadding;

        List<Vector3> spawnedPositions = new List<Vector3>();

        Debug.Log($"<color=cyan>[{bossName}] 攻擊開始！在地圖上隨機配置 {spawnCount} 個互動道具！</color>");

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 finalPos = transform.position;
            bool foundValidPosition = false;
            int maxAttempts = 20;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float randomX = Random.Range(minX, maxX);
                float randomY = Random.Range(minY, maxY);
                Vector3 candidatePos = new Vector3(randomX, randomY, 0f); // 嚴格鎖死 Z = 0

                bool isTooClose = false;
                foreach (Vector3 existingPos in spawnedPositions)
                {
                    if (Vector3.Distance(candidatePos, existingPos) < minObjectDistance) { isTooClose = true; break; }
                }

                if (!isTooClose && Vector3.Distance(candidatePos, transform.position) < minObjectDistance)
                {
                    isTooClose = true;
                }

                if (!isTooClose) { finalPos = candidatePos; foundValidPosition = true; break; }
            }
            
            spawnedPositions.Add(finalPos);

            // 1. 生成地圖靜態道具
            GameObject specialObj = Instantiate(specialAttackPrefab, finalPos, Quaternion.identity);

            // ★ 2. 絕對正解：改為註冊進「地圖道具清單」，再也不會卡死子彈發射器！
            RegisterMapMechanism(specialObj);
            
            // ★ 3. 綁定 SO 減秒：如果這個道具上面有 BossSpecialMechanism，把 SO 傳給它
            if (specialObj.TryGetComponent(out BossSpecialMechanism mechanism))
            {
                mechanism.InitializeMechanism(this.bossSO);
            }
        }
    }
}