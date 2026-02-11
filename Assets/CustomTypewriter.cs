using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Yarn.Unity;
using Yarn.Markup; // 這是 MarkupValue 需要的命名空間
using TMPro;

public class CustomTypewriter : MonoBehaviour, IAsyncTypewriter
{
    // --- 介面要求的基本欄位 ---
    public TMP_Text Text;
    
    public List<IActionMarkupHandler> ActionMarkupHandlers { get; set; } = new List<IActionMarkupHandler>();

    [Tooltip("每秒顯示幾個字")]
    public float baseCharactersPerSecond = 30f;

    [Header("除錯設定")]
    [Tooltip("勾選這個，Console 會顯示打字機的運作細節")]
    public bool enableLog = true;

    // ✨ 自動抓取 TMP (防呆機制)
    void Awake()
    {
        if (Text == null)
        {
            Text = GetComponent<TMP_Text>();
            if (Text != null && enableLog) Debug.Log("Autofill: 成功自動抓取到 TextMeshPro 組件");
        }
    }

    // --- 核心打字邏輯 ---
    public async YarnTask RunTypewriter(MarkupParseResult line, CancellationToken token)
    {
        // 1. 安全檢查
        if (Text == null)
        {
            Debug.LogError("CustomTypewriter: ❌ TextMeshPro 元件未設定！請手動拖曳或確認物件上有 TMP。");
            return;
        }

        if (enableLog) Debug.Log($"[Typewriter] ▶️ 開始打字: \"{line.Text}\" (總長度: {line.Text.Length})");

        // 查勤：列印屬性
        if (enableLog)
        {
            Debug.Log($"[Typewriter] 🔎 正在檢查屬性清單 (共 {line.Attributes.Count} 個)...");
            foreach (var attr in line.Attributes)
            {
                string props = "";
                foreach (var key in attr.Properties.Keys)
                {
                    props += $"[{key}={attr.Properties[key]}] ";
                }
                Debug.Log($"   🔸 發現屬性: Name='{attr.Name}', Pos={attr.Position}, Len={attr.Length}, Props={props}");
            }
        }

        // 2. 初始化
        Text.maxVisibleCharacters = 0;
        Text.text = line.Text; 

        foreach (var h in ActionMarkupHandlers) h.OnLineDisplayBegin(line, Text);

        int totalChars = line.Text.Length;
        float currentSpeed = baseCharactersPerSecond;

        // 為了避免重複讀取，先把屬性清單存起來
        var attributes = line.Attributes;

        // 3. 逐字顯示迴圈
        for (int i = 0; i < totalChars; i++)
        {
            // --- A. 檢查是否被取消 ---
            if (token.IsCancellationRequested)
            {
                if (enableLog) Debug.Log($"[Typewriter] ⏩ 玩家跳過！直接顯示全部。");
                Text.maxVisibleCharacters = totalChars;
                return; 
            }

            // --- B. 檢查 [speed] 標籤 ---
            currentSpeed = baseCharactersPerSecond; // 預設回歸基礎速度

            foreach (var attr in attributes)
            {
                // 檢查範圍是否命中
                if (attr.Name == "speed" && i >= attr.Position && i < (attr.Position + attr.Length))
                {
                    // 🔥 修正點：使用 MarkupValue 來接收，而不是 object
                    MarkupValue val;
                    
                    // 嘗試抓取 "value" 屬性 (例如 [speed value=5]) 或 "speed" 屬性 (例如 [speed=5])
                    if (attr.Properties.TryGetValue("value", out val) || attr.Properties.TryGetValue("speed", out val))
                    {
                        // MarkupValue.ToString() 會自動轉成字串，我們再轉成 float
                        if (float.TryParse(val.ToString(), out float customVal))
                        {
                            currentSpeed = customVal;
                        }
                    }
                }
            }

            // --- C. 檢查 [wait] 標籤 ---
            foreach (var attr in attributes)
            {
                // 檢查位置是否命中
                if (attr.Name == "wait" && attr.Position == i)
                {
                    // 🔥 修正點：使用 MarkupValue 來接收
                    MarkupValue val;

                    // 嘗試抓取 "value" 屬性 或 "wait" 屬性
                    if (attr.Properties.TryGetValue("value", out val) || attr.Properties.TryGetValue("wait", out val))
                    {
                        if (float.TryParse(val.ToString(), out float waitSeconds))
                        {
                            if (enableLog) Debug.Log($"[Typewriter] ✋ 觸發停頓: {waitSeconds} 秒 (位置: {i})");
                            
                            // 執行停頓
                            await Task.Delay((int)(waitSeconds * 1000), token);
                            
                            if (enableLog) Debug.Log($"[Typewriter] ▶️ 停頓結束，繼續打字...");
                        }
                    }
                }
            }

            // --- D. 觸發 Yarn 事件 ---
            foreach (var processor in ActionMarkupHandlers)
            {
                try {
                    await processor.OnCharacterWillAppear(i, line, token);
                } catch (OperationCanceledException) { }
            }

            // 顯示字元
            Text.maxVisibleCharacters = i + 1;

            // --- E. 打字延遲 ---
            if (currentSpeed > 0)
            {
                float delaySeconds = 1.0f / currentSpeed;
                await Task.Delay((int)(delaySeconds * 1000), token);
            }
        }

        // 4. 結束
        Text.maxVisibleCharacters = totalChars;
        if (enableLog) Debug.Log($"[Typewriter] ✅ 打字完成！");
        
        foreach (var h in ActionMarkupHandlers) h.OnLineDisplayComplete();
    }

    // --- 初始化與清理 ---
    public void PrepareForContent(MarkupParseResult line)
    {
        if (Text == null) return;
        Text.maxVisibleCharacters = 0;
        Text.text = line.Text;
        foreach (var h in ActionMarkupHandlers) h.OnPrepareForLine(line, Text);
    }

    public void ContentWillDismiss()
    {
        foreach (var h in ActionMarkupHandlers) h.OnLineWillDismiss();
    }
}