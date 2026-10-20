using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 驚嚇演出（Jumpscare）：
///   1. 整個畫面黑掉
///   2. 螢幕中央出現一張小到看不清的圖，慢慢靠近、慢慢浮現
///   3. 玩家開始注意它的時候（隨機多等一小段時間），圖突然衝到鏡頭前 + 音效 + 畫面震動 + 閃白
///   4. 停留一下後結束，執行你選的後續動作
///
/// 不需要事先做任何 UI：第一次播放時會自動建立自己的 Canvas（蓋在所有東西上面）。
/// 放在場景任何一個物件上，填圖與音效，再從別的腳本呼叫 Play()（CaptchaMinigame 的 Jumpscare 欄位會自動呼叫）。
/// 編輯器測試：按 Test Key（預設 J）就會播放。
/// </summary>
public class JumpscareSequence : MonoBehaviour
{
    public enum AfterAction
    {
        Nothing,      // 什麼都不做，畫面停在黑色（由 On Finished 事件接手）
        ReloadScene,  // 重新載入目前場景（重來）
        LoadScene     // 載入下面指定的場景
    }

    [Header("圖片與音效")]
    [SerializeField] private Sprite scareSprite;
    [Tooltip("慢慢靠近期間的背景聲（低鳴、心跳聲之類，會循環）。可留空。")]
    [SerializeField] private AudioClip creepClip;
    [SerializeField, Range(0f, 1f)] private float creepVolume = 0.4f;
    [Tooltip("衝到鏡頭前那一刻的驚嚇音效。")]
    [SerializeField] private AudioClip scareClip;
    [SerializeField, Range(0f, 1f)] private float scareVolume = 1f;

    [Header("全黑")]
    [SerializeField] private float blackoutFadeSeconds = 0.3f;
    [Tooltip("全黑後先停一下才開始出現圖片。")]
    [SerializeField] private float blackHoldSeconds = 1f;

    [Header("慢慢靠近（看不清 → 勉強看得到）")]
    [SerializeField] private float creepSeconds = 6f;
    [Tooltip("起始大小（相對於圖片佔螢幕 90% 的完整大小）。越小越看不清。")]
    [SerializeField] private float creepStartScale = 0.02f;
    [Tooltip("靠近結束時的大小。大約 0.15~0.25 是『好像看得到』的程度。")]
    [SerializeField] private float creepEndScale = 0.2f;
    [SerializeField] private AnimationCurve creepCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("前面這個比例的時間內圖片從全透明慢慢浮現（0~1）。")]
    [SerializeField, Range(0.05f, 1f)] private float fadeInFraction = 0.4f;
    [Tooltip("靠近期間圖片微微顫抖的最大幅度（像素，越到後面越明顯）。0 = 不顫抖。")]
    [SerializeField] private float creepJitterPixels = 2f;

    [Header("突然衝過來")]
    [Tooltip("靠近結束後，再隨機多等這個範圍內的秒數才衝過來（讓玩家猜不到時間點）。")]
    [SerializeField] private float rushDelayMin = 0.4f;
    [SerializeField] private float rushDelayMax = 1.5f;
    [SerializeField] private float rushSeconds = 0.1f;
    [Tooltip("衝到鏡頭前的最終大小（1 = 圖片佔螢幕 90%，3 以上會衝出螢幕）。")]
    [SerializeField] private float rushEndScale = 3.5f;
    [SerializeField] private bool flashOnRush = true;
    [SerializeField] private float shakeSeconds = 0.6f;
    [Tooltip("畫面震動幅度（像素）。")]
    [SerializeField] private float shakeStrength = 40f;
    [Tooltip("衝過來之後畫面停留多久（秒），不得小於震動時間。")]
    [SerializeField] private float holdAfterRushSeconds = 1.2f;

    [Header("結束後")]
    [SerializeField] private AfterAction afterAction = AfterAction.ReloadScene;
    [SerializeField] private string sceneName;
    [Tooltip("演出播完後觸發（在 After Action 之前）。")]
    [SerializeField] private UnityEvent onFinished;
    [SerializeField] private bool hideCursorDuringScare = true;

    [Header("測試")]
    [Tooltip("遊戲執行中按這個鍵直接播放演出（只在編輯器有效）。None = 停用。")]
    [SerializeField] private Key testKey = Key.J;

    public bool IsPlaying { get; private set; }

    private Canvas canvas;
    private Image blackImage;
    private Image scareImage;
    private Image whiteImage;
    private AudioSource creepSource;
    private AudioSource scareSource;

#if UNITY_EDITOR
    private void Update()
    {
        if (testKey != Key.None && Keyboard.current != null && Keyboard.current[testKey].wasPressedThisFrame)
            Play().Forget();
    }
#endif

    [ContextMenu("Play Jumpscare")]
    private void PlayFromMenu() => Play().Forget();

    public async UniTask Play()
    {
        if (IsPlaying) return;
        if (scareSprite == null)
        {
            Debug.LogWarning("[JumpscareSequence] 尚未指定 Scare Sprite。", this);
            return;
        }

        IsPlaying = true;
        var token = this.GetCancellationTokenOnDestroy();

        BuildUI();
        if (hideCursorDuringScare) Cursor.visible = false;

        canvas.gameObject.SetActive(true);
        SetAlpha(blackImage, 0f);
        SetAlpha(scareImage, 0f);
        SetAlpha(whiteImage, 0f);
        scareImage.rectTransform.localScale = Vector3.one * creepStartScale;
        scareImage.rectTransform.anchoredPosition = Vector2.zero;
        canvas.transform.localPosition = Vector3.zero;

        // 1. 全黑
        await FadeAlpha(blackImage, 0f, 1f, blackoutFadeSeconds, token);
        await Wait(blackHoldSeconds, token);

        // 2. 慢慢靠近
        if (creepClip != null)
        {
            creepSource.clip = creepClip;
            creepSource.volume = creepVolume;
            creepSource.loop = true;
            creepSource.Play();
        }

        float elapsed = 0f;
        while (elapsed < creepSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, creepSeconds));
            ApplyCreep(t);
            await UniTask.Yield(token);
        }
        ApplyCreep(1f);

        // 隨機再等一下：等玩家的注意力集中在那個點上
        float extra = UnityEngine.Random.Range(Mathf.Min(rushDelayMin, rushDelayMax), Mathf.Max(rushDelayMin, rushDelayMax));
        float waited = 0f;
        while (waited < extra)
        {
            waited += Time.unscaledDeltaTime;
            ApplyCreep(1f);
            await UniTask.Yield(token);
        }

        // 3. 突然衝到鏡頭前
        creepSource.Stop();
        SetAlpha(scareImage, 1f);
        scareImage.rectTransform.anchoredPosition = Vector2.zero;

        if (scareClip != null)
        {
            scareSource.volume = scareVolume;
            scareSource.PlayOneShot(scareClip, 1f);
        }
        if (flashOnRush) FlashWhite(token).Forget();

        float rush = 0f;
        float from = creepEndScale;
        while (rush < rushSeconds)
        {
            rush += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(rush / Mathf.Max(0.01f, rushSeconds));
            float eased = t * t; // 越接近越快
            scareImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(from, rushEndScale, eased);
            await UniTask.Yield(token);
        }
        scareImage.rectTransform.localScale = Vector3.one * rushEndScale;

        // 震動 + 停留
        float hold = Mathf.Max(holdAfterRushSeconds, shakeSeconds);
        float held = 0f;
        while (held < hold)
        {
            held += Time.unscaledDeltaTime;
            if (held < shakeSeconds)
            {
                float decay = 1f - held / Mathf.Max(0.01f, shakeSeconds);
                Vector2 offset = UnityEngine.Random.insideUnitCircle * (shakeStrength * decay);
                canvas.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
            }
            else
            {
                canvas.transform.localPosition = Vector3.zero;
            }
            await UniTask.Yield(token);
        }
        canvas.transform.localPosition = Vector3.zero;

        // 4. 結束：切黑，執行後續
        SetAlpha(scareImage, 0f);
        SetAlpha(blackImage, 1f);
        onFinished?.Invoke();

        switch (afterAction)
        {
            case AfterAction.ReloadScene:
                SceneTransition.Reload().Forget();
                break;
            case AfterAction.LoadScene:
                if (!string.IsNullOrEmpty(sceneName)) SceneTransition.Load(sceneName).Forget();
                else Debug.LogWarning("[JumpscareSequence] After Action 選了 LoadScene，但 Scene Name 是空的。", this);
                break;
        }

        if (hideCursorDuringScare) Cursor.visible = true;
        IsPlaying = false;
    }

    private void ApplyCreep(float t)
    {
        float scale = Mathf.Lerp(creepStartScale, creepEndScale, creepCurve.Evaluate(t));
        scareImage.rectTransform.localScale = Vector3.one * scale;
        SetAlpha(scareImage, Mathf.Clamp01(t / Mathf.Max(0.01f, fadeInFraction)));

        if (creepJitterPixels > 0f)
            scareImage.rectTransform.anchoredPosition = UnityEngine.Random.insideUnitCircle * (creepJitterPixels * t);
    }

    private async UniTaskVoid FlashWhite(CancellationToken token)
    {
        SetAlpha(whiteImage, 0.9f);
        await FadeAlpha(whiteImage, 0.9f, 0f, 0.15f, token);
    }

    private static async UniTask Wait(float seconds, CancellationToken token)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            await UniTask.Yield(token);
        }
    }

    private static async UniTask FadeAlpha(Image img, float from, float to, float seconds, CancellationToken token)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(img, Mathf.Lerp(from, to, Mathf.Clamp01(t / Mathf.Max(0.01f, seconds))));
            await UniTask.Yield(token);
        }
        SetAlpha(img, to);
    }

    private static void SetAlpha(Graphic g, float a)
    {
        var c = g.color;
        c.a = a;
        g.color = c;
    }

    private void BuildUI()
    {
        if (canvas != null)
        {
            scareImage.sprite = scareSprite;
            return;
        }

        var canvasGo = new GameObject("JumpscareCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);

        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 黑底：全螢幕，同時擋住下面所有點擊
        blackImage = CreateImage("Black", canvasGo.transform, Color.black);
        Stretch(blackImage.rectTransform);
        blackImage.raycastTarget = true;

        // 驚嚇圖：置中，原始大小約佔螢幕 90%，用 localScale 控制遠近
        scareImage = CreateImage("Scare", canvasGo.transform, Color.white);
        var rt = scareImage.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1920f * 0.9f, 1080f * 0.9f);
        scareImage.sprite = scareSprite;
        scareImage.preserveAspect = true;
        scareImage.raycastTarget = false;

        // 閃白層
        whiteImage = CreateImage("Flash", canvasGo.transform, Color.white);
        Stretch(whiteImage.rectTransform);
        whiteImage.raycastTarget = false;

        creepSource = gameObject.AddComponent<AudioSource>();
        creepSource.playOnAwake = false;
        creepSource.spatialBlend = 0f;
        scareSource = gameObject.AddComponent<AudioSource>();
        scareSource.playOnAwake = false;
        scareSource.spatialBlend = 0f;

        canvasGo.SetActive(false);
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}