using UnityEngine;

public enum BattleState { Dialogue, PlayerMove, PlayerFight, BossDecide, Win, Lose }

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance; // 單例模式方便大家存取
    public BattleState currentState;

    [Header("場景中的 Boss")]
    public BossBase currentBoss; // 這裡我們只存「基類」，不管他是 Cleaner 還是 Boomber

    void Awake() { Instance = this; }

    void Start()
    {
        ChangeState(BattleState.PlayerFight);
    }

    public void ChangeState(BattleState newState)
    {
        currentState = newState;
        switch (currentState)
        {
            case BattleState.Dialogue:
                // 顯示對話框...
                break;
            case BattleState.PlayerFight:
                // 通知 Boss 開始運作
                if(currentBoss != null) currentBoss.StartBattle();
                break;
            case BattleState.Win:
                Debug.Log("玩家獲勝！");
                break;
        }
    }
}