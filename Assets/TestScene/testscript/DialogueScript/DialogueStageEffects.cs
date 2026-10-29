using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[Serializable]
public class RelicEntry
{
    public string key;
    public Sprite sprite;

    [Tooltip("大小倍率（1 = 預設大小）。在展示框內與全螢幕展示兩種模式都有效。")]
    public float scale = 1f;

    [Header("只在『沒有指定 Item Area』（全螢幕展示）時有效")]
    [Tooltip("勾選：這個物品用下面指定的位置，不用 ShowcaseOverlay 的預設。")]
    public bool overridePosition;
    [Tooltip("物品中心在畫面上的位置（比例座標）。(0.5, 0.5) = 正中央，Y 越大越上面。")]
    public Vector2 center = new Vector2(0.5f, 0.58f);
}

/// <summary>
/// 對話視窗的「舞台效果」：全黑底 + 道具（神祗遺物）展示。
/// 掛在對話視窗 prefab（Dialogueshow 所在的那個）底下，由 Dialogueshow 讀到 Ink tag 時呼叫。
///
/// Ink 用法：
///   # bg: black        → 全黑（立繪、場景都被蓋住，只剩對話框與 UI 裝飾框）
///   # bg: none         → 黑底淡出
///   # relic: relic1    → 展示 Relic Entries 裡 key = relic1 的圖
///
/// 兩種展示模式：
///   1. 展示框內展示（建議）：指定 Item Area。展示框是對話視窗 prefab 裡的一個固定物件，
///      對話有出現它就一直在；物品只會出現在 Item Area 裡（對話框以上、展示框以內）。
///   2. 全螢幕展示：Item Area 留空，改用通用的 ShowcaseOverlay（壓暗背景、置中顯示）。
///
/// Hierarchy 建議（對話視窗 prefab 底下，由上到下 = 越下面越在前面）：
///   BlackCover        全螢幕黑色 Image（立繪後面、最底層）
///   ……立繪、對話框……
///   ItemArea          空的 RectTransform，拉成「對話框上緣以上、展示框內緣以內」的範圍（物品只會出現在這裡）
///   ShowcaseFrame     展示框的圖（Raycast Target 關掉），放在 ItemArea 後面，邊框才會蓋在物品邊緣上
/// </summary>
public class DialogueStageEffects : MonoBehaviour
{
    [Header("全黑底")]
    [Tooltip("全螢幕黑色 Image。放在立繪後面、最底層。")]
    [SerializeField] private Image blackCover;
    [SerializeField] private float blackFadeSeconds = 0.4f;

    [Header("展示框內展示（建議）")]
    [Tooltip("物品出現的範圍（空的 RectTransform）。在 Scene 視窗把它拉成「對話框上緣以上、展示框內緣以內」。留空 = 改用全螢幕展示。")]
    [SerializeField] private RectTransform itemArea;
    [Tooltip("物品要用的 Image。留空 = 會在 Item Area 底下自動建立一個。")]
    [SerializeField] private Image itemImage;
    [Tooltip("物品最多佔 Item Area 的比例（1 = 撐滿，0.9 = 留一點邊）。")]
    [SerializeField, Range(0.2f, 1f)] private float itemFill = 0.9f;
    [SerializeField] private float itemFadeInSeconds = 0.35f;
    [SerializeField] private float itemFadeOutSeconds = 0.3f;
    [Tooltip("預設多久後自動消失（秒）。0 = 只能點擊（F / 滑鼠左鍵）才消失。")]
    [SerializeField] private float itemHoldSeconds = 4f;
    [Tooltip("剛出現後這段時間內點擊無效，避免推進對話的那一下點擊直接把它關掉。")]
    [SerializeField] private float clickIgnoreSeconds = 0.5f;

    [Header("道具表")]
    [SerializeField] private List<RelicEntry> relicEntries = new List<RelicEntry>();
    [Tooltip("道具消失後，是否順便把全黑底淡出（神祗遺物出現 = 開始出現視覺畫面）。")]
    [SerializeField] private bool revealAfterRelic = true;
    [Tooltip("-1 = 用預設（展示框內用 Item Hold Seconds；全螢幕用 ShowcaseOverlay 的預設）；0 = 只能點擊；>0 = 幾秒後自動消失")]
    [SerializeField] private float relicAutoHideSeconds = -1f;

    /// <summary>目前是否正在展示道具（Dialogueshow 用來暫停對話推進）。</summary>
    public bool IsBusy => inlineBusy || ShowcaseOverlay.IsShowing;

    private int blackVersion;
    private bool inlineBusy;

    private void Awake()
    {
        if (itemArea != null && itemImage == null)
        {
            var go = new GameObject("ItemImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(itemArea, false);
            itemImage = go.GetComponent<Image>();
        }

        if (itemImage != null)
        {
            itemImage.preserveAspect = true;
            itemImage.raycastTarget = false;
            itemImage.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        // 對話視窗被關掉時，物品也要收掉
        inlineBusy = false;
        if (itemImage != null) itemImage.gameObject.SetActive(false);
    }

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

        // 對話框自己的黑底已經完全蓋上了 → 放開前一個階段（例如開場音效）的黑幕
        if (on && ScreenBlackout.IsHolding) ScreenBlackout.Release();
    }

    // ───────────── 道具展示 ─────────────

    public async UniTask ShowRelic(string key)
    {
        Sprite sprite = null;
        RelicEntry entry = null;
        foreach (var e in relicEntries)
            if (e.key == key) { sprite = e.sprite; entry = e; break; }

        if (sprite == null)
        {
            Debug.LogWarning($"[DialogueStageEffects] Relic Entries 裡找不到 key：{key}");
            return;
        }

        float scale = entry != null ? entry.scale : 1f;

        if (itemArea != null && itemImage != null)
        {
            await ShowInArea(sprite, scale);
        }
        else if (entry != null && entry.overridePosition)
        {
            await ShowcaseOverlay.Play(sprite, relicAutoHideSeconds, entry.center, scale);
        }
        else
        {
            await ShowcaseOverlay.Play(sprite, relicAutoHideSeconds, null, scale);
        }

        if (revealAfterRelic) SetBlack(false);
    }

    /// <summary>在 Item Area（展示框內、對話框以上）裡展示物品。注意：IsBusy 要在第一個 await 之前就變 true。</summary>
    private async UniTask ShowInArea(Sprite sprite, float scale)
    {
        if (inlineBusy) return;
        inlineBusy = true;

        try
        {
            var token = this.GetCancellationTokenOnDestroy();
            float hold = relicAutoHideSeconds >= 0f ? relicAutoHideSeconds : itemHoldSeconds;

            // 讓物品在 Item Area 裡置中，最多佔 itemFill × scale（不超出範圍）
            float f = Mathf.Clamp(itemFill * scale, 0.05f, 1f);
            var rt = itemImage.rectTransform;
            rt.anchorMin = new Vector2(0.5f - f * 0.5f, 0.5f - f * 0.5f);
            rt.anchorMax = new Vector2(0.5f + f * 0.5f, 0.5f + f * 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            itemImage.sprite = sprite;
            SetAlpha(itemImage, 0f);
            itemImage.gameObject.SetActive(true);

            await FadeItem(0f, 1f, itemFadeInSeconds);

            float waited = 0f;
            while (true)
            {
                await UniTask.Yield(token);
                if (!isActiveAndEnabled) return; // 對話視窗被關掉了（OnDisable 已收掉物品）
                waited += Time.unscaledDeltaTime;
                if (hold > 0f && waited >= hold) break;
                if (waited >= clickIgnoreSeconds && ClickedThisFrame()) break;
            }

            await FadeItem(1f, 0f, itemFadeOutSeconds);
            if (itemImage != null) itemImage.gameObject.SetActive(false);
        }
        finally
        {
            inlineBusy = false;
        }
    }

    private async UniTask FadeItem(float from, float to, float seconds)
    {
        var token = this.GetCancellationTokenOnDestroy();
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(itemImage, Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds)));
            await UniTask.Yield(token);
        }
        SetAlpha(itemImage, to);
    }

    private static bool ClickedThisFrame()
    {
        return (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
    }

    private static void SetAlpha(Graphic g, float a)
    {
        var c = g.color;
        c.a = a;
        g.color = c;
    }
}