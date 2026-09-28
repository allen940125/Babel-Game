using System;
using UnityEngine;
using Yarn.Unity;

public class DialogueUIManager : MonoBehaviour
{
    [Header("依賴注入")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private GameObject dialogueCanvas;

    // 對外開放的完成回呼，外部誰呼叫誰處理，UI 本身不干涉遊戲邏輯
    private Action _onDialogueCompleteCallback;

    private void OnEnable()
    {
        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueFinished);
        }
    }

    private void OnDisable()
    {
        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueFinished);
        }
    }

    /// <summary>
    /// 啟動對話的統一入口
    /// </summary>
    /// <param name="startNode">Yarn 的節點名稱</param>
    /// <param name="onComplete">對話完畢後的回呼</param>
    public void StartDialogue(string startNode, Action onComplete = null)
    {
        _onDialogueCompleteCallback = onComplete;

        if (dialogueCanvas != null) dialogueCanvas.SetActive(true);

        if (dialogueRunner != null && !string.IsNullOrEmpty(startNode))
        {
            dialogueRunner.StartDialogue(startNode);
        }
        else
        {
            // 防呆：若無節點或 Runner 遺失，直接結束並回呼，避免卡住流程
            OnDialogueFinished();
        }
    }

    private void OnDialogueFinished()
    {
        if (dialogueCanvas != null) dialogueCanvas.SetActive(false);

        // 觸發外部註冊的回呼（例如通知 BattleManager 開打）
        Action callback = _onDialogueCompleteCallback;
        _onDialogueCompleteCallback = null;
        callback?.Invoke();
    }
}