using UnityEngine;

public enum BattleState { Dialogue, PlayerMove, PlayerFight, BossDecide, Win, Lose }

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance; 
    public BattleState currentState;

    [Header("場景設定")]
    [Tooltip("Boss 應該生成在地圖的哪個位置？")]
    public Transform bossSpawnPoint;
    
    // ★ 維持私有，靠程式動態抓取
    private BossStateMachine currentBoss; 

    // ==========================================
    // ★ 測試專用擴充
    // ==========================================
    [Header("Debug / 單一場景測試設定")]
    [Tooltip("當直接執行此戰鬥場景 (無跨場景資料) 時，會使用此藍圖來生成 Boss。")]
    public EntityBlueprintSO debugBossBlueprint;

    void Awake() { Instance = this; }

    void Start()
    {
        InitializeBoss();
        ChangeState(BattleState.Dialogue);
    }

    private void InitializeBoss()
    {
        // 1. 嘗試讀取跨場景傳來的資料 (Production Mode)
        EntityRuntime incomingData = GameFlowManager.PendingBattleEnemyData;

        // ==========================================
        // ★ 核心防呆與 Debug 攔截邏輯
        // ==========================================
        if (incomingData == null)
        {
            if (debugBossBlueprint != null)
            {
                Debug.LogWarning($"<color=orange>[BattleManager Debug 模式] 偵測到無跨場景資料，正在使用測試藍圖: {debugBossBlueprint.name}</color>");
                
                // 動態偽造一份與大地圖傳過來一模一樣的活體資料
                incomingData = new EntityRuntime();
                incomingData.Initialize(debugBossBlueprint);
            }
            else
            {
                Debug.LogError("[BattleManager] 找不到跨場景 Boss 資料，且未設定 debugBossBlueprint！無法生成 Boss。");
                return;
            }
        }

        // 確保藍圖與 Prefab 存在
        if (incomingData.Blueprint == null || incomingData.Blueprint.battlePrefab == null)
        {
            Debug.LogError("[BattleManager] Boss Blueprint 缺少戰鬥 Prefab！");
            return;
        }

        Debug.Log($"[BattleManager] 正在生成 Boss: {incomingData.Blueprint.name}");

        // 2. 實體化真正的 3D 模型
        GameObject bossObj = Instantiate(incomingData.Blueprint.battlePrefab, bossSpawnPoint.position, bossSpawnPoint.rotation);

        // 3. 取得組件
        currentBoss = bossObj.GetComponent<BossStateMachine>();
        EntityCore bossCore = bossObj.GetComponent<EntityCore>();

        // 4. 核心注入 (無論是真實跨場景資料，還是 Debug 偽造資料，對這裡來說都一樣)
        if (bossCore != null)
        {
            bossCore.InjectRuntimeData(incomingData);
        }
        else
        {
            Debug.LogError("[BattleManager] 生成的 Boss Prefab 身上沒有 EntityCore！");
        }
    }
    
    public void ChangeState(BattleState newState)
    {
        currentState = newState;
        switch (currentState)
        {
            case BattleState.Dialogue:
                break;
            case BattleState.PlayerFight:
                if(currentBoss != null) currentBoss.StartBattle();
                break;
            case BattleState.Win:
                Debug.Log("玩家獲勝！");
                break;
        }
    }
}