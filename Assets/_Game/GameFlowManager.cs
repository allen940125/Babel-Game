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

    // 這裡我們不需要被 Core 初始化，我們利用 Unity 的生命週期自己啟動
    private void Start()
    {
        // ★ 主動去 Core 的事件中心訂閱「戰鬥觸發事件」
        // Core 完全不知道 GameFlowManager 的存在，是 GameFlowManager 自己貼上去聽的！
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.SetSubscribe(
                GameManager.Instance.MainGameEvent.OnStartBattleEvent, 
                HandleStartBattle
            );
            Debug.Log("[GameFlowManager] 成功訂閱戰鬥事件。");
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
        Debug.Log($"[GameFlow] 準備切換至 {evt.BattleScene}，目標 Boss: {evt.EnemyData.Blueprint.name}");

        PendingBattleEnemyData = evt.EnemyData;

        // ★ 主動呼叫 Core 的轉場管理器
        // 假設你的 GameManager 有提供取得 SceneTransitionManager 的方法，或者它是單例
        GameManager.Instance.SceneTransitionManager.LoadScene(evt.BattleScene);
    }
}