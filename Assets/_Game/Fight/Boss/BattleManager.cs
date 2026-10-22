using Gamemanager;
using UnityEngine;

public enum BattleState { Dialogue, PlayerMove, PlayerFight, BossDecide, Win, Lose }

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    public BattleState currentState;

    [Header("場景設定")]
    public Transform bossSpawnPoint;
    [SerializeField] private DialogueUIManager dialogueUI;

    [Header("流程開關")]
    [Tooltip("勾選後開場會走對話；未勾選則直接進入戰鬥")]
    [SerializeField] private bool enableOpeningDialogue = false;
    [Tooltip("Yarn Spinner 開場對話節點名稱")]
    [SerializeField] private string openingDialogueNode = "Boss_Opening";

    [Header("Debug / 單一場景測試設定")]
    public EntityBlueprintSO debugBossBlueprint;

    private BossStateMachine currentBoss;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitializeBoss();
        StartCoroutine(EvaluateOpeningFlowRoutine());
    }

    private System.Collections.IEnumerator EvaluateOpeningFlowRoutine()
    {
        // ★ 核心修復：在單一場景測試時，等待大管家的非同步資料載入完成！
        if (GameManager.Instance != null)
        {
            while (!GameManager.Instance.IsInitialized)
            {
                yield return null; // 如果資料還沒載完，就一直等
            }
        }

        // 確保場景上所有剛 Instantiate 出來的物件都跑完了 Start()
        yield return null;

        if (enableOpeningDialogue && dialogueUI != null)
        {
            ChangeState(BattleState.Dialogue);
            dialogueUI.StartDialogue(openingDialogueNode, onComplete: () =>
            {
                ChangeState(BattleState.PlayerFight);
            });
        }
        else
        {
            // 大家都準備好了，資料庫也有了，再正式開打
            ChangeState(BattleState.PlayerFight);
        }
    }

    /// <summary>
    /// 評估開場流程：決定進入對話狀態還是直接進入開打狀態
    /// </summary>
    private void EvaluateOpeningFlow()
    {
        if (enableOpeningDialogue && dialogueUI != null)
        {
            ChangeState(BattleState.Dialogue);
            dialogueUI.StartDialogue(openingDialogueNode, onComplete: () =>
            {
                // 對話結束後的回呼：正式切換到玩家開打狀態
                ChangeState(BattleState.PlayerFight);
            });
        }
        else
        {
            // 不需要對話，直接開打
            ChangeState(BattleState.PlayerFight);
        }
    }

    private void InitializeBoss()
    {
        EntityRuntime incomingData = GameFlowManager.PendingBattleEnemyData;

        if (incomingData == null)
        {
            if (debugBossBlueprint != null)
            {
                Debug.LogWarning($"<color=orange>[BattleManager Debug] 無跨場景資料，套用測試藍圖: {debugBossBlueprint.name}</color>");
                incomingData = new EntityRuntime();
                incomingData.Initialize(debugBossBlueprint);
            }
            else
            {
                Debug.LogError("[BattleManager] 找不到 Boss 資料，且未配置 debugBossBlueprint！");
                return;
            }
        }

        if (incomingData.Blueprint == null || incomingData.Blueprint.battlePrefab == null)
        {
            Debug.LogError("[BattleManager] Boss Blueprint 缺少戰鬥 Prefab！");
            return;
        }

        GameObject bossObj = Instantiate(incomingData.Blueprint.battlePrefab, bossSpawnPoint.position, bossSpawnPoint.rotation);
        currentBoss = bossObj.GetComponent<BossStateMachine>();
        
        EntityCore bossCore = bossObj.GetComponent<EntityCore>();
        if (bossCore != null)
        {
            bossCore.InjectRuntimeData(incomingData);
        }
        else
        {
            Debug.LogError("[BattleManager] 生成的 Boss Prefab 身上缺少 EntityCore！");
        }
    }

    public void ChangeState(BattleState newState)
    {
        Debug.Log("[BattleManager] ChangeState: " + newState);
        currentState = newState;
        switch (currentState)
        {
            case BattleState.Dialogue:
                // 可在此凍結玩家移動或 Boss 行為
                Debug.Log("開始對話");
                break;

            case BattleState.PlayerFight:
                Debug.Log("開始戰鬥" + currentBoss.bossName);
                // ★ 在這裡發送「開戰信號」
                if (GameManager.Instance?.MainGameEvent != null)
                {
                    GameManager.Instance.MainGameEvent.Send(new BattleInitiatedEvent());
                }
                if (currentBoss != null) currentBoss.StartBattle();
                Debug.Log("正式開始戰鬥" + currentBoss.bossName);
                break;

            case BattleState.Win:
                Debug.Log("玩家獲勝！");
                break;

            case BattleState.Lose:
                Debug.Log("戰鬥失敗！");
                break;
        }
    }
}