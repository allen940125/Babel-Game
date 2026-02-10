using UnityEngine;
using System.Collections;
using TMPro;
using Yarn.Unity;
using System;

public class SuperLineView : DialogueViewBase
{
    [Header("UI 組件")]
    public TextMeshProUGUI lineText;       // 對話文字框
    public TextMeshProUGUI characterName;  // 名字框 (可選)
    public CanvasGroup canvasGroup;        // 用來控制淡入淡出

    [Header("打字機設定")]
    public float defaultSpeed = 0.05f;     // 預設每個字 0.05 秒

    private Coroutine typewriterRoutine;   // 紀錄目前的打字排程
    private bool isSkipping = false;       // 玩家是否按下了跳過

    public void Start() 
    {
        // 遊戲開始時先把對話框藏起來
        if (canvasGroup != null) 
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    // 當 Yarn Spinner 丟一句話過來時，會執行這裡
    public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
    {
        // 1. 顯示 UI
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // 2. 顯示名字 (如果有)
        if (characterName != null)
        {
            characterName.text = dialogueLine.CharacterName;
            characterName.gameObject.SetActive(!string.IsNullOrEmpty(dialogueLine.CharacterName));
        }

        // 3. 開始打字機效果
        // 這裡要先重置 Skip 狀態
        isSkipping = false; 
        typewriterRoutine = StartCoroutine(TypewriterEffect(dialogueLine, onDialogueLineFinished));
    }

    // 打字機的核心邏輯
    private IEnumerator TypewriterEffect(LocalizedLine line, Action onComplete)
    {
        lineText.text = line.Text.Text; // 把所有文字填進去
        lineText.maxVisibleCharacters = 0; // 先全部隱藏

        int totalChars = line.Text.Text.Length;
        float currentSpeed = defaultSpeed;

        for (int i = 0; i <= totalChars; i++)
        {
            // 如果玩家按下跳過，就瞬間顯示全部
            if (isSkipping)
            {
                lineText.maxVisibleCharacters = totalChars;
                yield break; // 結束打字，但先不呼叫 onComplete，等玩家再按一次
            }

            // --- 檢查 [speed] 標籤 ---
            // 檢查這句話有沒有 speed 屬性覆蓋目前的字
            var speedAttr = line.Text.Attributes.Find(a => 
                a.Name == "speed" && i >= a.Position && i < (a.Position + a.Length)
            );

            if (speedAttr != null)
            {
                // 讀取標籤裡的值，例如 [speed=0.5]
                if (float.TryParse(speedAttr.Properties["value"].ToString(), out float customSpeed))
                {
                    currentSpeed = customSpeed;
                }
            }
            else
            {
                currentSpeed = defaultSpeed;
            }

            // --- 檢查 [wait] 標籤 (模擬停頓) ---
            // 這是模擬舊版 wait 的邏輯，檢查當前字元是否有 wait 屬性
            var waitAttr = line.Text.Attributes.Find(a => 
                a.Name == "wait" && a.Position == i
            );
            
            if (waitAttr != null)
            {
                 if (float.TryParse(waitAttr.Properties["value"].ToString(), out float waitTime))
                 {
                     yield return new WaitForSeconds(waitTime);
                 }
            }

            // 顯示當前的字數
            lineText.maxVisibleCharacters = i;

            // 等待打字時間
            yield return new WaitForSeconds(currentSpeed);
        }
        
        // 打字結束，重置協程
        typewriterRoutine = null;
    }

    // 當玩家點擊滑鼠或按下空白鍵時，Yarn Spinner 會呼叫這個
    public override void UserRequestedViewAdvancement()
    {
        // 如果還在打字，就變成「跳過」
        if (typewriterRoutine != null)
        {
            isSkipping = true;
        }
        else
        {
            // 如果已經打完了，就通知系統「這句話結束了，下一句！」
            requestInterrupt?.Invoke(); 
        }
    }

    // 當這句話結束要消失時
    public override void DismissLine(Action onDismissalComplete)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        onDismissalComplete();
    }
}