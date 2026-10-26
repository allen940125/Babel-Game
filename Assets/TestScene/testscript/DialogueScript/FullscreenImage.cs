using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 全螢幕 2D 圖片：用一張圖填滿整個畫面。不需要在場景裡放任何東西，第一次呼叫時自動建立。
///
///   await FullscreenImage.Show(sprite);                    // 淡入（圖會蓋住整個畫面）
///   await FullscreenImage.WaitForDismiss(5f);              // 等 5 秒或玩家點擊（0 = 只能點擊）
///   await FullscreenImage.Hide(0.6f);                      // 淡出（0 = 立刻消失）
///
/// 或一次做完：await FullscreenImage.ShowAndWait(sprite, 5f);
///
/// 填滿方式（FitMode）：
///   Cover   填滿整個畫面，比例不同時裁掉多出來的部分（預設，最像「圖填滿畫面」）
///   Fit     完整顯示整張圖，多出來的地方是黑邊
///   Stretch 強制拉伸成整個畫面（可能變形）
/// </summary>
public class FullscreenImage : MonoBehaviour
{
    public enum FitMode { Cover, Fit, Stretch }

    private static FullscreenImage instance;
    public static bool IsShowing => instance != null && instance.root != null && instance.root.gameObject.activeSelf;

    private CanvasGroup root;
    private Image picture;
    private AspectRatioFitter fitter;
    private int version;

    // ───────────── 公開 API ─────────────

    public static async UniTask Show(Sprite sprite, FitMode mode = FitMode.Cover, float fadeInSeconds = 0.6f)
    {
        if (sprite == null)
        {
            Debug.LogWarning("[FullscreenImage] Sprite 是空的。");
            return;
        }
        Ensure();
        await instance.DoShow(sprite, mode, fadeInSeconds);
    }

    /// <summary>等指定秒數，或玩家點擊（F / 滑鼠左鍵 / 空白鍵）。autoSeconds = 0 代表只能點擊。</summary>
    public static async UniTask WaitForDismiss(float autoSeconds, float clickIgnoreSeconds = 0.5f)
    {
        float t = 0f;
        while (autoSeconds <= 0f || t < autoSeconds)
        {
            t += Time.unscaledDeltaTime;
            if (t >= clickIgnoreSeconds && ClickedThisFrame()) break;
            await UniTask.Yield();
        }
    }

    public static async UniTask Hide(float fadeOutSeconds = 0.6f)
    {
        if (instance == null) return;
        await instance.DoHide(fadeOutSeconds);
    }

    public static async UniTask ShowAndWait(Sprite sprite, float autoSeconds = 0f, FitMode mode = FitMode.Cover,
                                            float fadeIn = 0.6f, float fadeOut = 0.6f)
    {
        await Show(sprite, mode, fadeIn);
        await WaitForDismiss(autoSeconds);
        await Hide(fadeOut);
    }

    // ───────────── 內部 ─────────────

    private static bool ClickedThisFrame()
    {
        return (Keyboard.current != null && (Keyboard.current.fKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
    }

    private static void Ensure()
    {
        if (instance != null) return;
        var go = new GameObject("FullscreenImage");
        instance = go.AddComponent<FullscreenImage>();
        instance.Build();
    }

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20000; // 蓋在一般 UI 之上，在 ScreenBlackout / SceneTransition 之下

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 根：CanvasGroup 控制整體淡入淡出；RectMask2D 裁掉 Cover 模式多出來的部分
        var rootGo = new GameObject("Root", typeof(RectTransform), typeof(CanvasGroup), typeof(RectMask2D), typeof(Image));
        rootGo.transform.SetParent(transform, false);
        Stretch((RectTransform)rootGo.transform);
        var bg = rootGo.GetComponent<Image>();
        bg.color = Color.black;
        bg.raycastTarget = true; // 顯示期間擋住下面的點擊
        root = rootGo.GetComponent<CanvasGroup>();

        var picGo = new GameObject("Picture", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
        picGo.transform.SetParent(rootGo.transform, false);
        picture = picGo.GetComponent<Image>();
        picture.raycastTarget = false;
        fitter = picGo.GetComponent<AspectRatioFitter>();

        rootGo.SetActive(false);
    }

    private async UniTask DoShow(Sprite sprite, FitMode mode, float fadeIn)
    {
        int v = ++version;
        picture.sprite = sprite;

        var rt = picture.rectTransform;
        if (mode == FitMode.Stretch)
        {
            fitter.enabled = false;
            Stretch(rt);
        }
        else
        {
            fitter.enabled = true;
            fitter.aspectMode = mode == FitMode.Cover ? AspectRatioFitter.AspectMode.EnvelopeParent
                                                       : AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
        }

        root.alpha = 0f;
        root.gameObject.SetActive(true);

        float t = 0f;
        while (t < fadeIn)
        {
            if (v != version) return;
            t += Time.unscaledDeltaTime;
            root.alpha = Mathf.Clamp01(t / fadeIn);
            await UniTask.Yield();
        }
        if (v == version) root.alpha = 1f;
    }

    private async UniTask DoHide(float fadeOut)
    {
        int v = ++version;
        float from = root.alpha;
        float t = 0f;
        while (t < fadeOut)
        {
            if (v != version) return;
            t += Time.unscaledDeltaTime;
            root.alpha = Mathf.Lerp(from, 0f, Mathf.Clamp01(t / fadeOut));
            await UniTask.Yield();
        }
        if (v != version) return;
        root.alpha = 0f;
        root.gameObject.SetActive(false);
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
