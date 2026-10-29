using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Febucci.TextAnimatorForUnity;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class NarrationWindow : BasePanel
{
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TypewriterComponent typewriter;

    [Header("節奏設定")]
    [SerializeField] private float readingPauseSeconds = 1.5f; // 每句顯示完後，自動接下一句前的停留時間

    [Header("循環音效（海浪等）")]
    [Tooltip("旁白結束時，是否把這段旁白中用 # loop: 開啟的循環音效全部淡出（保險，避免忘了寫 # loopstop:）。")]
    [SerializeField] private bool stopLoopsWhenEnded = true;

    public void Init()
    {
        typewriter.onTextShowed.AddListener(OnTypewriterFinished);
        StartFirstLine().Forget();
    }

    private async UniTaskVoid StartFirstLine()
    {
        // 保險：等面板完全啟用一幀之後再開始打字，避免 Typewriter 元件還沒 OnEnable 就被呼叫而沒反應
        await UniTask.Yield();
        NextLine();
    }

    private void Update()
    {
        if (!Dialoguecontroller.Instance.DialogueIsPlaying) return;
        if (Keyboard.current == null && Mouse.current == null) return;

        bool advance = (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                    || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        if (!advance) return;

        // 按鍵只負責「加速讓整句字顯示出來」，不負責跳下一句
        if (typewriter.IsShowingText)
            typewriter.SkipTypewriter();
    }

    private void NextLine()
    {
        var story = Dialoguecontroller.Instance.CurrentStory;
        if (story == null) return;

        if (story.canContinue)
        {
            string line = "";
            while (story.canContinue)
            {
                line = story.Continue();
                HandleTags(story.currentTags);   // 讀 Ink 的 # 標記（循環音效）
                if (!string.IsNullOrWhiteSpace(line)) break;
            }
            typewriter.ShowText(line);
        }
        else
        {
            EndNarration();
        }
    }

    // 不管是自然打完字，還是被玩家按鍵加速跳過，Febucci 的 Typewriter 都會呼叫這個事件
    private void OnTypewriterFinished()
    {
        WaitThenAdvance().Forget();
    }

    private async UniTaskVoid WaitThenAdvance()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(readingPauseSeconds));

        var story = Dialoguecontroller.Instance.CurrentStory;
        if (story == null) return;

        if (!story.canContinue)
            EndNarration();
        else
            NextLine();
    }

    private void EndNarration()
    {
        if (stopLoopsWhenEnded) SceneLoopSfx.StopAllLoops();
        Dialoguecontroller.Instance.EndDialogue();
    }

    // ---------- Ink 標記 ----------

    private void HandleTags(List<string> tags)
    {
        if (tags == null) return;
        foreach (var raw in tags)
        {
            string tag = raw.Trim();
            if (tag.StartsWith("sfx:"))
                SfxLibrary.PlayKey(tag.Substring("sfx:".Length).Trim());          // # sfx: phone
            else if (tag.StartsWith("loopstop:"))
                SceneLoopSfx.StopKey(tag.Substring("loopstop:".Length).Trim());   // # loopstop: wave
            else if (tag.StartsWith("loop:"))
                SceneLoopSfx.PlayKey(tag.Substring("loop:".Length).Trim());       // # loop: wave
        }
    }
}