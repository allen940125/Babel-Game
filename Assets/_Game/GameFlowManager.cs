using UnityEngine;
using Gamemanager; // 引入 Core 的 Event 系統
using Game.SceneManagement; // 引入 Core 的 Scene 系統

/// <summary>
/// 屬於 Gameplay 層級的流程大腦
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    // 跨場景暫存區 (Gameplay 專用)
    public static EntityRuntime PendingBattleEnemyData { get; private set; }

    private void Start()
    {
        Debug.Log("<color=yellow>[GameFlowManager] 啟動！準備訂閱戰鬥事件...</color>");
        
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.SetSubscribe(
                GameManager.Instance.MainGameEvent.OnStartBattleEvent, 
                HandleStartBattle
            );
            Debug.Log("<color=yellow>[GameFlowManager] 成功訂閱戰鬥事件！等待碰撞發生...</color>");
        }
        else
        {
            Debug.LogError("<color=red>[GameFlowManager] 致命錯誤：找不到 GameManager 或 MainGameEvent，訂閱失敗！</color>");
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.Unsubscribe<StartBattleEvent>(HandleStartBattle);
        }
    }

    private void HandleStartBattle(StartBattleEvent evt)
    {
        string bossName = (evt.EnemyData != null && evt.EnemyData.Blueprint != null) 
            ? evt.EnemyData.Blueprint.name 
            : "未知(Blueprint為Null)";
            
        Debug.Log($"<color=cyan>[GameFlow] 收到 StartBattleEvent！準備切換至 {evt.BattleScene}，目標 Boss: {bossName}</color>");

        PendingBattleEnemyData = evt.EnemyData;

        // ★ 主動呼叫 Core 的轉場管理器
        GameManager.Instance.SceneTransitionManager.LoadScene(evt.BattleScene);
    }
}