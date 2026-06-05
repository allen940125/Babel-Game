using UnityEngine;
using Yarn.Unity;

public class DialogueUIManager : MonoBehaviour
{
    [Header("依賴注入")]
    public DialogueRunner dialogueRunner;
    public GameObject dialogueCanvas; // 你的對話 UI 畫布

    void OnEnable()
    {
        if (dialogueRunner != null)
        {
            // 訂閱對話結束事件
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueFinished);
        }
    }

    void OnDisable()
    {
        if (dialogueRunner != null)
        {
            // 必須解除訂閱，防止記憶體洩漏 (Memory Leak) 或 NRE
            dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueFinished);
        }
    }

    // 當 Yarn Spinner 執行到沒有下一句、或遇到 <<stop>> 時，會自動觸發此方法
    private void OnDialogueFinished()
    {
        Debug.Log("系統: 對話已完全結束。");
        
        // 執行你的關閉邏輯
        dialogueCanvas.SetActive(false);

        BattleManager.Instance.ChangeState(BattleState.PlayerFight);

        // 例如：解除玩家的移動限制
        // PlayerController.EnableMovement();
    }
}