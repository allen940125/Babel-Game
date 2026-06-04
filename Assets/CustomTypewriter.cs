using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Yarn.Unity;
using Yarn.Markup; 
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
        if (Text == null)
        {
            Debug.LogError("CustomTypewriter: ❌ TextMeshPro 元件未設定！");
            return;
        }

        // 1. 初始化與強制解析 (解決 Rich Text 長度錯誤)
        Text.maxVisibleCharacters = 0;
        Text.text = line.Text; 
        Text.ForceMeshUpdate(); 
        
        int totalChars = Text.textInfo.characterCount; 

        if (enableLog) Debug.Log($"[Typewriter] ▶️ 開始打字: \"{line.Text}\" (可見總長度: {totalChars})");

        foreach (var h in ActionMarkupHandlers) h.OnLineDisplayBegin(line, Text);

        // 2. 預處理屬性 (解決 O(N*M) 效能問題)
        var waitEvents = new Dictionary<int, float>();
        var speedEvents = new Dictionary<int, float>();

        if (enableLog) Debug.Log($"[Typewriter] 🔎 正在檢查與提取屬性清單 (共 {line.Attributes.Count} 個)...");

        foreach (var attr in line.Attributes)
        {
            if (attr.Name == "wait" && (attr.Properties.TryGetValue("value", out var waitVal) || attr.Properties.TryGetValue("wait", out waitVal)))
            {
                if (float.TryParse(waitVal.ToString(), out float w)) 
                {
                    waitEvents[attr.Position] = w;
                    if (enableLog) Debug.Log($"   🔸 提取停頓屬性: Pos={attr.Position}, Wait={w}s");
                }
            }
            else if (attr.Name == "speed" && (attr.Properties.TryGetValue("value", out var speedVal) || attr.Properties.TryGetValue("speed", out speedVal)))
            {
                if (float.TryParse(speedVal.ToString(), out float s)) 
                {
                    speedEvents[attr.Position] = s;
                    if (enableLog) Debug.Log($"   🔸 提取速度屬性: Pos={attr.Position}, Speed={s}");
                }
            }
        }

        float currentSpeed = baseCharactersPerSecond;

        // 3. 逐字顯示迴圈 (加入 try-catch 攔截跳過事件)
        try
        {
            for (int i = 0; i < totalChars; i++)
            {
                // A. 檢查是否被取消 (玩家按下跳過)
                token.ThrowIfCancellationRequested(); 

                // B. 檢查速度變化
                if (speedEvents.TryGetValue(i, out float newSpeed))
                {
                    currentSpeed = newSpeed;
                }

                // C. 檢查停頓
                if (waitEvents.TryGetValue(i, out float waitSeconds))
                {
                    if (enableLog) Debug.Log($"[Typewriter] ✋ 觸發停頓: {waitSeconds} 秒 (位置: {i})");
                    await Task.Delay((int)(waitSeconds * 1000), token);
                    if (enableLog) Debug.Log($"[Typewriter] ▶️ 停頓結束，繼續打字...");
                }

                // D. 觸發 Yarn 事件 (角色頭像切換、音效等)
                foreach (var processor in ActionMarkupHandlers)
                {
                    await processor.OnCharacterWillAppear(i, line, token);
                }

                // 顯示字元
                Text.maxVisibleCharacters = i + 1;

                // E. 基礎打字延遲
                if (currentSpeed > 0)
                {
                    float delaySeconds = 1.0f / currentSpeed;
                    await Task.Delay((int)(delaySeconds * 1000), token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 成功捕捉到跳過事件，不會再引發死鎖
            if (enableLog) Debug.Log($"[Typewriter] ⏩ 玩家跳過！直接顯示全部。");
        }

        // 4. 收尾工作 (保證生命週期完整)
        Text.maxVisibleCharacters = totalChars;
        if (enableLog) Debug.Log($"[Typewriter] ✅ 對話顯示流程結束！");
        
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