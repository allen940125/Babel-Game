using UnityEngine;
using System.Collections;
using TMPro;
using Yarn.Unity;
using System;
using System.Threading.Tasks; 
using System.Collections.Generic; // 確保引用 List

public class SuperLineView : DialoguePresenterBase
{
    [Header("UI 組件")]
    public TextMeshProUGUI lineText;       
    public TextMeshProUGUI characterName;  
    public CanvasGroup canvasGroup;        

    [Header("打字機設定")]
    public float defaultSpeed = 0.05f;     

    public void Start() 
    {
        if (canvasGroup != null) 
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    // 1. 對話開始
    public override YarnTask OnDialogueStartedAsync()
    {
        return YarnTask.CompletedTask;
    }

    // 2. 對話結束
    public override YarnTask OnDialogueCompleteAsync()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        return YarnTask.CompletedTask;
    }

    // 3. 核心：跑一行台詞
    public override async YarnTask RunLineAsync(LocalizedLine dialogueLine, LineCancellationToken token)
    {
        // 顯示 UI
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // 顯示名字
        if (characterName != null)
        {
            characterName.text = dialogueLine.CharacterName;
            characterName.gameObject.SetActive(!string.IsNullOrEmpty(dialogueLine.CharacterName));
        }

        // 準備文字
        lineText.text = dialogueLine.Text.Text;
        lineText.maxVisibleCharacters = 0;

        int totalChars = dialogueLine.Text.Text.Length;
        float currentSpeed = defaultSpeed;

        // 取得所有標籤 (避免反覆存取)
        var attributes = dialogueLine.Text.Attributes;

        for (int i = 0; i <= totalChars; i++)
        {
            // 檢查是否被取消 (玩家按跳過)
            if (token.HurryUpToken.IsCancellationRequested)
            {
                lineText.maxVisibleCharacters = totalChars;
                return; // 直接結束打字
            }

            // --- 修正 1: 改用 foreach 迴圈來找 [speed] 標籤 ---
            // 因為 MarkupAttribute 是 Struct，不能用 Find 找 null，直接跑迴圈最穩
            foreach (var attr in attributes)
            {
                // 檢查這個標籤是否叫 speed，且目前字數 i 落在它的範圍內
                if (attr.Name == "speed" && i >= attr.Position && i < (attr.Position + attr.Length))
                {
                    // 嘗試讀取數值
                    if (attr.Properties.TryGetValue("value", out var valueProp))
                    {
                        if (float.TryParse(valueProp.ToString(), out float customSpeed))
                        {
                            currentSpeed = customSpeed;
                        }
                    }
                }
            }

            // --- 修正 2: 檢查 [wait] 標籤 ---
            foreach (var attr in attributes)
            {
                // wait 標籤通常是一個點 (Length=0)，檢查位置是否剛好是 i
                if (attr.Name == "wait" && attr.Position == i)
                {
                    if (attr.Properties.TryGetValue("value", out var valueProp))
                    {
                        if (float.TryParse(valueProp.ToString(), out float waitTime))
                        {
                            // 呼叫我們自己寫的安全等待函式
                            await SafeWait(waitTime, token);
                        }
                    }
                }
            }

            // 更新顯示
            lineText.maxVisibleCharacters = i;

            // 打字間隔等待
            if (currentSpeed > 0)
            {
                await SafeWait(currentSpeed, token);
            }
        }

        // 確保最後顯示完整
        lineText.maxVisibleCharacters = totalChars;

        // 等待玩家按下繼續
        await YarnTask.WaitUntilCanceled(token.NextContentToken);
    }

    // --- 修正 3: 自己寫一個安全的等待函式，取代 SuppressCancellationThrow ---
    private async Task SafeWait(float seconds, LineCancellationToken token)
    {
        try 
        {
            // 單位換算：秒 -> 毫秒
            await Task.Delay((int)(seconds * 1000), token.HurryUpToken);
        }
        catch (OperationCanceledException)
        {
            // 捕捉取消例外，什麼都不做，讓程式繼續往下跑
            // 這樣玩家按跳過時就不會報錯
        }
    }
}