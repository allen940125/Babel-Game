using UnityEngine;
using System.Collections.Generic;

// ★ 要求必須與 BossStateMachine 掛載在同一個物件上
[RequireComponent(typeof(BossStateMachine))]
public class BossAction_SpawnMechanisms : BossPhaseAction
{
    [Header("機關設定")]
    [Tooltip("要生成的特殊機關 Prefab (將 Cleaner 或 Celeste 的機關拖入此處)")]
    public GameObject mechanismPrefab; 
    
    [Tooltip("每次觸發要生成的數量")]
    public int spawnCount = 3;
    
    [Header("生成座標與防重疊設定")]
    [Tooltip("距離螢幕邊緣的安全距離")]
    public float spawnPadding = 1.0f;
    
    [Tooltip("機關之間、以及機關與Boss之間的最小距離")]
    public float minObjectDistance = 2.0f; 

    // ★ 內部資料：負責記錄自己這個模組生成的物件，避免與其他模組衝突
    private List<GameObject> _spawnedMechanisms = new List<GameObject>();

    protected override void ExecuteAction()
    {
        if (mechanismPrefab == null)
        {
            Debug.LogError($"[邏輯錯誤] {gameObject.name} 的 BossAction_SpawnMechanisms 未綁定 mechanismPrefab！");
            return;
        }

        // 1. 確保生成前，場上沒有上一波殘留的本模組機關
        ClearMechanisms();

        // 2. 計算螢幕可視範圍的絕對安全區域
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindFirstObjectByType<Camera>();

        Vector3 minScreen = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
        Vector3 maxScreen = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));

        float minX = minScreen.x + spawnPadding;
        float maxX = maxScreen.x - spawnPadding;
        float minY = minScreen.y + spawnPadding;
        float maxY = maxScreen.y - spawnPadding;

        List<Vector3> spawnedPositions = new List<Vector3>();
        
        // 取得 Boss 資料核心，準備賦予給機關
        EntityCore core = stateMachine.GetComponent<EntityCore>();

        Debug.Log($"<color=cyan>[行為觸發] {gameObject.name} 在狀態 {targetPhase} 生成了 {spawnCount} 個機關。</color>");

        // 3. 執行防重疊生成邏輯
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 finalPos = transform.position; // 防呆預設值：Boss位置
            bool foundValidPosition = false;
            int maxAttempts = 20;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float randomX = Random.Range(minX, maxX);
                float randomY = Random.Range(minY, maxY);
                Vector3 candidatePos = new Vector3(randomX, randomY, 0f); // 嚴格鎖死 Z = 0

                bool isTooClose = false;

                // 檢查是否與已生成的機關重疊
                foreach (Vector3 existingPos in spawnedPositions)
                {
                    if (Vector3.Distance(candidatePos, existingPos) < minObjectDistance)
                    {
                        isTooClose = true; 
                        break;
                    }
                }

                // 檢查是否離 Boss 本體太近
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

            if (!foundValidPosition)
            {
                Debug.LogWarning($"[空間警告] 第 {i} 個機關在 {maxAttempts} 次隨機中找不到安全距離，強制生成。");
            }
            
            spawnedPositions.Add(finalPos);

            // 4. 實例化與初始化
            GameObject specialObj = Instantiate(mechanismPrefab, finalPos, Quaternion.identity);
            _spawnedMechanisms.Add(specialObj);
            
            // ★ 可選：如果你的 StateMachine 還有其他需要統一尋找物件的邏輯，可以註冊。
            // 否則光靠此腳本的 ClearMechanisms() 就足夠管理生命週期了。
            stateMachine.RegisterMapMechanism(specialObj);
            
            // ★ 綁定資料 (將 Boss 的 EntityRuntime 傳給機關)
            if (core != null && specialObj.TryGetComponent(out BossSpecialMechanism mechanism))
            {
                mechanism.InitializeMechanism(core.RuntimeData);
            }
            else if (core == null)
            {
                Debug.LogError($"[致命錯誤] {gameObject.name} 缺少 EntityCore，無法傳遞資料給機關！");
            }
        }
    }

    // ★ 重寫停止行為：當 Boss 離開 targetPhase (例如結束 Attacking 進入 WaitingForBullets) 時自動觸發
    protected override void StopAction()
    {
        ClearMechanisms();
    }

    // 專屬於本模組的清理邏輯
    private void ClearMechanisms()
    {
        for (int i = _spawnedMechanisms.Count - 1; i >= 0; i--)
        {
            if (_spawnedMechanisms[i] != null)
            {
                _spawnedMechanisms[i].SetActive(false);
                Destroy(_spawnedMechanisms[i]);
            }
        }
        _spawnedMechanisms.Clear();
    }
}