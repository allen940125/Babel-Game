using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 通用「大立繪展示」：把一張圖置中放大顯示，周圍壓暗，玩家點擊或等一段時間後消失。
/// 任何地方都能用（對話中、探索中、戰鬥中），不依賴對話系統。
///
/// 最簡單的用法（程式）：
///     await ShowcaseOverlay.Play(sprite);                 // 預設設定
///     await ShowcaseOverlay.Play(sprite, autoHideSeconds: 0); // 只能點擊才消失
/// 不用事先在場景放東西：第一次呼叫時會自動建立自己的 Canvas。
/// 想在 Inspector 調預設值（壓暗程度、時間…），就在場景放一個空物件掛這支腳本。
///
/// 展示期間 ShowcaseOverlay.IsShowing 為 true，PlayerExplorationController 會自動鎖住玩家。
/// </summary>
public class ShowcaseOverlay : MonoBehaviour
{
    public static ShowcaseOverlay Instance { get; private set; }

    /// <summary>目前是否正在展示（玩家控制 / 對話推進可用它判斷要不要暫停）。</summary>
    public static bool IsShowing => Instance != null && Instance.busy;

    [Header("外觀")]
    [SerializeField, Range(0f, 1f)] private float dimAlpha = 0.85f;
    [Tooltip("圖片最多佔螢幕寬高的比例（保持原比例縮放）。")]
    [SerializeField, Range(0.2f, 1f)] private float maxScreenFraction = 0.8f;
    [SerializeField] private int sortingOrder = 500;

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
    private bool busy;

    // ───────────── 對外 API ─────────────

    /// <summary>展示一張圖。autoHideSeconds：-1 = 用預設值，0 = 只能點擊，>0 = 幾秒後自動消失。</summary>
    public static UniTask Play(Sprite sprite, float autoHideSeconds = -1f)
    {
        return GetOrCreate().Show(sprite, autoHideSeconds);
    }

    public async UniTask Show(Sprite sprite, float autoHideOverride = -1f)
    {
        if (busy || sprite == null) return;
        busy = true;

        try
        {
            var token = this.GetCancellationTokenOnDestroy();
            float hold = autoHideOverride >= 0f ? autoHideOverride : autoHideSeconds;

            picture.sprite = sprite;
            SetAlpha(dim, 0f);
            SetAlpha(picture, 0f);
            canvas.gameObject.SetActive(true);

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
            canvas.gameObject.SetActive(false);
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

        var found = FindFirstObjectByType<ShowcaseOverlay>();
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

        // 壓暗層：全螢幕黑色，同時擋住下面的點擊
        dim = CreateImage("Dim", canvasGo.transform);
        Stretch(dim.rectTransform, Vector2.zero, Vector2.one);
        dim.color = new Color(0f, 0f, 0f, 0f);
        dim.raycastTarget = true;

        // 展示圖：置中，保持比例，最多佔螢幕的 maxScreenFraction
        picture = CreateImage("Picture", canvasGo.transform);
        float margin = (1f - maxScreenFraction) * 0.5f;
        Stretch(picture.rectTransform, new Vector2(margin, margin), new Vector2(1f - margin, 1f - margin));
        picture.preserveAspect = true;
        picture.raycastTarget = false;
        picture.color = new Color(1f, 1f, 1f, 0f);

        canvasGo.SetActive(false);
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
            float k = Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds));
            SetAlpha(dim, k * dimAlpha);
            SetAlpha(picture, k);
            await UniTask.Yield(token);
        }
        SetAlpha(dim, to * dimAlpha);
        SetAlpha(picture, to);
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
