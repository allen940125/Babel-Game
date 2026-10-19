using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 流暢切場景：淡出到黑 → 背景載入新場景 → 新場景完全載好後 → 淡入。
/// 黑幕放在一個跨場景保留的 Canvas 上，所以切換瞬間不會露出 Skybox / 空畫面。
///
/// 用法（任何腳本都能呼叫，不用在場景裡放任何東西）：
///     SceneTransition.Load("BScene");
///     await SceneTransition.Load("BScene");                 // 要等它做完也可以
///     SceneTransition.Load("BScene", fadeOut: 0.5f, fadeIn: 0.8f, minBlackSeconds: 0.3f);
///     SceneTransition.Reload();                              // 重載目前場景
/// 場景要先加入 Build Settings。
/// </summary>
public class SceneTransition : MonoBehaviour
{
    private static SceneTransition instance;
    private Image cover;
    private bool busy;

    public static bool IsBusy => instance != null && instance.busy;

    public static UniTask Load(string sceneName, float fadeOut = 0.4f, float fadeIn = 0.6f, float minBlackSeconds = 0.15f)
    {
        Ensure();
        return instance.Run(sceneName, -1, fadeOut, fadeIn, minBlackSeconds);
    }

    public static UniTask Load(int buildIndex, float fadeOut = 0.4f, float fadeIn = 0.6f, float minBlackSeconds = 0.15f)
    {
        Ensure();
        return instance.Run(null, buildIndex, fadeOut, fadeIn, minBlackSeconds);
    }

    public static UniTask Reload(float fadeOut = 0.4f, float fadeIn = 0.6f, float minBlackSeconds = 0.15f)
    {
        Ensure();
        return instance.Run(null, SceneManager.GetActiveScene().buildIndex, fadeOut, fadeIn, minBlackSeconds);
    }

    private static void Ensure()
    {
        if (instance != null) return;
        var go = new GameObject("SceneTransition");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SceneTransition>();
        instance.Build();
    }

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000; // 蓋在所有 UI 之上

        var imgGo = new GameObject("Cover", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imgGo.transform.SetParent(transform, false);
        cover = imgGo.GetComponent<Image>();
        cover.color = new Color(0f, 0f, 0f, 0f);
        cover.raycastTarget = false;
        var rt = cover.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private async UniTask Run(string sceneName, int buildIndex, float fadeOut, float fadeIn, float minBlack)
    {
        if (busy) return;
        busy = true;
        cover.raycastTarget = true; // 切換期間擋住點擊

        await Fade(cover.color.a, 1f, fadeOut);

        var op = sceneName != null
            ? SceneManager.LoadSceneAsync(sceneName)
            : SceneManager.LoadSceneAsync(buildIndex);

        if (op == null)
        {
            Debug.LogError($"[SceneTransition] 無法載入場景：{(sceneName ?? buildIndex.ToString())}（有加進 Build Settings 嗎？）");
            await Fade(1f, 0f, fadeIn);
            cover.raycastTarget = false;
            busy = false;
            return;
        }

        op.allowSceneActivation = true;
        await op;

        // 讓新場景的 Awake / Start 跑完、攝影機渲染過第一幀，再淡入
        await UniTask.Yield();
        await UniTask.Yield();
        if (minBlack > 0f) await UniTask.Delay(TimeSpan.FromSeconds(minBlack), ignoreTimeScale: true);

        await Fade(1f, 0f, fadeIn);
        cover.raycastTarget = false;
        busy = false;
    }

    private async UniTask Fade(float from, float to, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(t / seconds)));
            await UniTask.Yield();
        }
        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        var c = cover.color;
        c.a = a;
        cover.color = c;
    }
}
