using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

/// <summary>
/// 逃跑期間在畫面中央用打字機打字：依「開始後第幾秒」依序顯示每一句，打完停一下，淡出，再等下一句。
///
/// 用法：在 Canvas 上建一個 TextMeshPro 文字（放畫面中央，字型用你的遊戲字型），拖進 Text 欄位，
/// 在 Lines 填入每一句的出現時間與內容。FleeSceneController 會在開始時自動呼叫 Play()。
/// 句子是依序顯示的：如果上一句還沒結束，下一句會等它結束才開始。
/// </summary>
public class FleeTextSequence : MonoBehaviour
{
    [Serializable]
    public class Line
    {
        [Tooltip("開始後第幾秒出現。")]
        public float atSeconds;
        [TextArea] public string text;
        [Tooltip("每個字出現的間隔（秒）。")]
        public float charInterval = 0.07f;
        [Tooltip("打完字之後停留幾秒。")]
        public float holdSeconds = 2f;
    }

    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private List<Line> lines = new List<Line>();
    [Tooltip("每一句結束時淡出花幾秒。")]
    [SerializeField] private float fadeOutSeconds = 0.5f;
    [Tooltip("勾選：場景開始就自動播放（不需要 FleeSceneController 呼叫）。")]
    [SerializeField] private bool playOnStart = false;

    private int version;

    private void Start()
    {
        if (text != null) text.text = "";
        if (playOnStart) Play();
    }

    public void Play()
    {
        if (text == null)
        {
            Debug.LogWarning("[FleeTextSequence] 尚未指定 Text。", this);
            return;
        }
        Run(++version).Forget();
    }

    /// <summary>立刻顯示一句（打字機 → 停留 → 淡出），播完才結束。FleeSequence 的 Text 步驟用它。</summary>
    public async UniTask Show(string message, float charInterval = 0.07f, float holdSeconds = 2f)
    {
        if (text == null) return;

        var line = new Line { text = message, charInterval = charInterval, holdSeconds = holdSeconds };
        await ShowLine(line, version, this.GetCancellationTokenOnDestroy());
    }

    public void Stop()
    {
        version++;
        if (text != null) text.text = "";
    }

    private async UniTaskVoid Run(int v)
    {
        var token = this.GetCancellationTokenOnDestroy();
        float startTime = Time.time;

        foreach (var line in lines)
        {
            // 等到這一句該出現的時間
            while (v == version && Time.time - startTime < line.atSeconds)
                await UniTask.Yield(token);
            if (v != version) return;

            await ShowLine(line, v, token);
            if (v != version) return;
        }
    }

    private async UniTask ShowLine(Line line, int v, CancellationToken token)
    {
        text.alpha = 1f;
        text.text = line.text;
        text.maxVisibleCharacters = 0;
        text.ForceMeshUpdate();
        int total = text.textInfo.characterCount;

        var step = TimeSpan.FromSeconds(Mathf.Max(0.01f, line.charInterval));
        for (int i = 1; i <= total; i++)
        {
            if (v != version) return;
            text.maxVisibleCharacters = i;
            await UniTask.Delay(step, cancellationToken: token);
        }

        await UniTask.Delay(TimeSpan.FromSeconds(line.holdSeconds), cancellationToken: token);

        float t = 0f;
        while (t < fadeOutSeconds)
        {
            if (v != version) return;
            t += Time.deltaTime;
            text.alpha = 1f - Mathf.Clamp01(t / Mathf.Max(0.01f, fadeOutSeconds));
            await UniTask.Yield(token);
        }

        text.text = "";
        text.maxVisibleCharacters = int.MaxValue;
        text.alpha = 1f;
    }
}
