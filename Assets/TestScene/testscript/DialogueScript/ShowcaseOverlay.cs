using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 通用「物品／大立繪展示」：把一張圖放在畫面上指定的位置，周圍壓暗，後面墊一個展示框，
/// 玩家點擊或等一段時間後消失。展示框只在物品出現時存在，消失後整個 Canvas 都會關掉。
/// 任何地方都能用（對話中、探索中、戰鬥中），不依賴對話系統。
///
/// 最簡單的用法（程式）：
///     await ShowcaseOverlay.Play(sprite);                         // 預設位置與設定
///     await ShowcaseOverlay.Play(sprite, 0);                       // 只能點擊才消失
///     await ShowcaseOverlay.Play(sprite, -1, new Vector2(0.5f, 0.6f), 0.8f); // 指定位置與大小倍率
///
/// 位置用「畫面比例座標」：(0, 0) = 左下角，(0.5, 0.5) = 正中央，(1, 1) = 右上角。
/// 不用事先在場景放東西：第一次呼叫時會自動建立自己的 Canvas（用預設值）。
/// 想在 Inspector 調位置、展示框圖案、壓暗程度…，就在場景放一個空物件掛這支腳本。
///
/// 展示期間 ShowcaseOverlay.IsShowing 為 true，PlayerExplorationController 會自動鎖住玩家。
/// </summary>
public class ShowcaseOverlay : MonoBehaviour
{
    public static ShowcaseOverlay Instance { get; private set; }

    /// <summary>目前是否正在展示（玩家控制 / 對話推進可用它判斷要不要暫停）。</summary>
    public static bool IsShowing => Instance != null && Instance.busy;

    [Header("位置與大小")]
    [Tooltip("物品中心在畫面上的位置（比例座標）。(0.5, 0.5) = 正中央；Y 大於 0.5 = 往上，可避開下方的對話框。")]
    [SerializeField] private Vector2 pictureCenter = new Vector2(0.5f, 0.58f);
    [Tooltip("物品最多佔螢幕寬高的比例（保持原比例縮放）。")]
    [SerializeField, Range(0.1f, 1f)] private float maxScreenFraction = 0.55f;

    [Header("外觀")]
    [SerializeField, Range(0f, 1f)] private float dimAlpha = 0.85f;
    [SerializeField] private int sortingOrder = 500;

    [Header("展示框（只在物品出現時顯示）")]
    [SerializeField] private bool showFrame = true;
    [Tooltip("展示框的圖。可以是九宮格（9-slice）圖，會自動拉伸成剛好包住物品。留空 = 用下面的簡易邊框。")]
    [SerializeField] private Sprite frameSprite;
    [Tooltip("展示框比物品多出來的邊距（畫面單位，以 1920×1080 為基準）。")]
    [SerializeField] private float framePadding = 40f;
    [Tooltip("簡易邊框（沒有指定展示框圖時用）：邊框顏色。")]
    [SerializeField] private Color borderColor = new Color(0.78f, 0.65f, 0.4f, 1f);
    [Tooltip("簡易邊框：邊框粗細。")]
    [SerializeField] private float borderThickness = 6f;
    [Tooltip("簡易邊框：框內、物品後面的底色。")]
    [SerializeField] private Color panelColor = new Color(0.05f, 0.03f, 0.03f, 0.95f);
    [Tooltip("指定了展示框圖時，用這個顏色上色（白色 = 保持原色）。")]
    [SerializeField] private Color frameTint = Color.white;

    [Header("時間")]
    [SerializeField] private float fadeInSeconds = 0.4f;
    [SerializeField] private float fadeOutSeconds = 0.3f;
    [Tooltip("預設多久後自動消失（秒）。0 = 只能點擊才消失。")]
    [SerializeField] private float autoHideSeconds = 4f;
    [Tooltip("剛出現後這段時間內點擊無效，避免推進對話的那一下點擊直接把它關掉。")]
    [SerializeField] private float clickIgnoreSeconds = 0.5f;

    private Canvas canvas;
    private Image dim;
    private Image picture;
    private Image frameBorder;
    private Image framePanel;
    private Image frameArt;
    private bool busy;

    // 每個會淡入淡出的元件，以及它「完全顯示」時的透明度
    private readonly List<(Graphic graphic, float alpha)> fadeTargets = new List<(Graphic, float)>();

    // ───────────── 對外 API ─────────────

    /// <summary>
    /// 展示一張圖。
    /// autoHideSeconds：-1 = 用預設值，0 = 只能點擊，&gt;0 = 幾秒後自動消失。
    /// center：畫面比例座標，null = 用預設位置。scale：大小倍率（1 = 預設大小）。
    /// </summary>
    public static UniTask Play(Sprite sprite, float autoHideSeconds = -1f, Vector2? center = null, float scale = 1f)
    {
        return GetOrCreate().Show(sprite, autoHideSeconds, center, scale);
    }

    public async UniTask Show(Sprite sprite, float autoHideOverride = -1f, Vector2? center = null, float scale = 1f)
    {
        if (busy || sprite == null) return;
        busy = true;

        try
        {
            var token = this.GetCancellationTokenOnDestroy();
            float hold = autoHideOverride >= 0f ? autoHideOverride : autoHideSeconds;

            picture.sprite = sprite;
            SetFade(0f);
            canvas.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases(); // 先讓 Canvas 算出實際大小，再排版
            Layout(sprite, center ?? pictureCenter, scale);

            await Fade(0f, 1f, fadeInSeconds, token);

            float waited = 0f;
            while (true)
            {
                await UniTask.Yield(token);
                waited += Time.unscaledDeltaTime;
                if (hold > 0f && waited >= hold) break;
                if (waited >= clickIgnoreSeconds && ClickedThisFrame()) break;
            }

            await Fade(1f, 0f, fadeOutSeconds, token);
            canvas.gameObject.SetActive(false); // 展示框、壓暗層一起關掉
        }
        finally
        {
            busy = false;
        }
    }

    // ───────────── 內部 ─────────────

    private static ShowcaseOverlay GetOrCreate()
    {
        if (Instance != null) return Instance;

        var found = FindAnyObjectByType<ShowcaseOverlay>();
        if (found != null) return found; // Awake 會設定 Instance

        var go = new GameObject("ShowcaseOverlay");
        return go.AddComponent<ShowcaseOverlay>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildUI();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void BuildUI()
    {
        var canvasGo = new GameObject("ShowcaseCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);

        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 由下到上：壓暗層 → 展示框 → 物品

        // 壓暗層：全螢幕黑色，同時擋住下面的點擊
        dim = CreateImage("Dim", canvasGo.transform);
        Stretch(dim.rectTransform, Vector2.zero, Vector2.one);
        dim.color = new Color(0f, 0f, 0f, dimAlpha);
        dim.raycastTarget = true;
        fadeTargets.Add((dim, dimAlpha));

        // 展示框：有指定圖就用圖，否則用「邊框 + 底色」的簡易版
        frameBorder = CreateImage("FrameBorder", canvasGo.transform);
        frameBorder.color = borderColor;
        frameBorder.raycastTarget = false;
        fadeTargets.Add((frameBorder, borderColor.a));

        framePanel = CreateImage("FramePanel", canvasGo.transform);
        framePanel.color = panelColor;
        framePanel.raycastTarget = false;
        fadeTargets.Add((framePanel, panelColor.a));

        frameArt = CreateImage("FrameArt", canvasGo.transform);
        frameArt.sprite = frameSprite;
        frameArt.color = frameTint;
        frameArt.raycastTarget = false;
        if (frameSprite != null)
            frameArt.type = frameSprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
        fadeTargets.Add((frameArt, frameTint.a));

        // 物品
        picture = CreateImage("Picture", canvasGo.transform);
        picture.preserveAspect = true;
        picture.raycastTarget = false;
        picture.color = Color.white;
        fadeTargets.Add((picture, 1f));

        canvasGo.SetActive(false);
    }

    /// <summary>依物品圖的比例算出大小，放到指定位置，展示框跟著包住它。</summary>
    private void Layout(Sprite sprite, Vector2 center, float scale)
    {
        var canvasRt = (RectTransform)canvas.transform;
        Vector2 canvasSize = canvasRt.rect.size;

        Vector2 spriteSize = sprite.rect.size;
        float fit = Mathf.Min(
            canvasSize.x * maxScreenFraction * scale / spriteSize.x,
            canvasSize.y * maxScreenFraction * scale / spriteSize.y);
        Vector2 picSize = spriteSize * fit;

        Vector2 pos = new Vector2((center.x - 0.5f) * canvasSize.x, (center.y - 0.5f) * canvasSize.y);
        SetCentered(picture.rectTransform, pos, picSize);

        Vector2 frameSize = picSize + Vector2.one * (framePadding * 2f);
        bool useArt = showFrame && frameSprite != null;
        bool useSimple = showFrame && frameSprite == null;

        frameArt.gameObject.SetActive(useArt);
        frameBorder.gameObject.SetActive(useSimple);
        framePanel.gameObject.SetActive(useSimple);

        if (useArt)
        {
            SetCentered(frameArt.rectTransform, pos, frameSize);
        }
        else if (useSimple)
        {
            SetCentered(frameBorder.rectTransform, pos, frameSize);
            SetCentered(framePanel.rectTransform, pos, frameSize - Vector2.one * (borderThickness * 2f));
        }
    }

    private static void SetCentered(RectTransform rt, Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
    }

    private static Image CreateImage(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        return go.GetComponent<Image>();
    }

    private static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private async UniTask Fade(float from, float to, float seconds, CancellationToken token)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            SetFade(Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds)));
            await UniTask.Yield(token);
        }
        SetFade(to);
    }

    /// <summary>k = 0（全透明）到 1（完全顯示），所有元件一起淡入淡出。</summary>
    private void SetFade(float k)
    {
        foreach (var (g, a) in fadeTargets)
        {
            if (g == null) continue;
            var c = g.color;
            c.a = a * k;
            g.color = c;
        }
    }

    private static bool ClickedThisFrame()
    {
        return (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
    }
}