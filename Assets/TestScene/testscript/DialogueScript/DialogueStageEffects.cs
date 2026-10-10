using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class RelicEntry
{
    public string key;
    public Sprite sprite;
}

/// <summary>
/// 對話視窗的「舞台效果」：全黑底 + 道具（神祗遺物）置中展示。
/// 掛在對話視窗 prefab（Dialogueshow 所在的那個）底下，由 Dialogueshow 讀到 Ink tag 時呼叫。
///
/// Ink 用法：
///   # bg: black        → 全黑（立繪、場景都被蓋住，只剩對話框與 UI 裝飾框）
///   # bg: none         → 黑底淡出
///   # relic: relic1    → 用 ShowcaseOverlay 展示 Relic Entries 裡 key = relic1 的圖
///                         （壓暗背景、圖置中，點擊或時間到消失）
///
/// Hierarchy：只需要在對話 prefab 最底層放一個全螢幕黑色 Image（BlackCover）。
/// 大立繪的 Canvas / 壓暗層由 ShowcaseOverlay 自動建立，不用手動做。
/// </summary>
public class DialogueStageEffects : MonoBehaviour
{
    [Header("全黑底")]
    [Tooltip("全螢幕黑色 Image。放在立繪後面、最底層。")]
    [SerializeField] private Image blackCover;
    [SerializeField] private float blackFadeSeconds = 0.4f;

    [Header("道具展示（交給通用的 ShowcaseOverlay）")]
    [SerializeField] private System.Collections.Generic.List<RelicEntry> relicEntries = new System.Collections.Generic.List<RelicEntry>();
    [Tooltip("道具消失後，是否順便把全黑底淡出（神祗遺物出現 = 開始出現視覺畫面）。")]
    [SerializeField] private bool revealAfterRelic = true;
    [Tooltip("-1 = 用 ShowcaseOverlay 的預設；0 = 只能點擊；>0 = 幾秒後自動消失")]
    [SerializeField] private float relicAutoHideSeconds = -1f;

    /// <summary>目前是否正在展示道具（Dialogueshow 用來暫停對話推進）。</summary>
    public bool IsBusy => ShowcaseOverlay.IsShowing;

    private int blackVersion;

    // ───────────── 全黑底 ─────────────

    public void SetBlack(bool on, bool instant = false)
    {
        if (blackCover == null) return;
        FadeBlack(on, instant ? 0f : blackFadeSeconds).Forget();
    }

    private async UniTaskVoid FadeBlack(bool on, float seconds)
    {
        int version = ++blackVersion;
        float target = on ? 1f : 0f;

        if (on) blackCover.gameObject.SetActive(true);

        float from = blackCover.color.a;
        float t = 0f;
        while (seconds > 0f && t < seconds)
        {
            if (version != blackVersion) return;
            t += Time.unscaledDeltaTime;
            SetAlpha(blackCover, Mathf.Lerp(from, target, t / seconds));
            await UniTask.Yield();
        }

        if (version != blackVersion) return;
        SetAlpha(blackCover, target);
        if (!on) blackCover.gameObject.SetActive(false);
    }

    // ───────────── 道具展示 ─────────────

    public async UniTask ShowRelic(string key)
    {
        Sprite sprite = null;
        foreach (var e in relicEntries)
            if (e.key == key) { sprite = e.sprite; break; }

        if (sprite == null)
        {
            Debug.LogWarning($"[DialogueStageEffects] Relic Entries 裡找不到 key：{key}");
            return;
        }

        await ShowcaseOverlay.Play(sprite, relicAutoHideSeconds);
        if (revealAfterRelic) SetBlack(false);
    }

    private static void SetAlpha(Graphic g, float a)
    {
        var c = g.color;
        c.a = a;
        g.color = c;
    }
}
