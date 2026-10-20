using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 跨流程的「全螢幕黑幕」：先蓋住畫面，等下一個階段準備好了再放開，避免交接時露出 Skybox / 其他畫面。
///
///   ScreenBlackout.Hold();      // 立刻全黑（蓋在所有 UI 之上，只在 SceneTransition 之下）
///   ScreenBlackout.Release();   // 放開（立刻消失；通常由下一個階段在自己的黑底就緒後呼叫）
///
/// 安全機制：Hold 之後如果 maxHoldSeconds 秒內沒有人 Release，會自動放開並印警告，避免卡在黑畫面。
/// 切換場景時不會被刪掉（DontDestroyOnLoad）。
/// </summary>
public class ScreenBlackout : MonoBehaviour
{
    private static ScreenBlackout instance;
    private Image cover;
    private int version;

    public static bool IsHolding => instance != null && instance.cover != null && instance.cover.gameObject.activeSelf;

    public static void Hold(float maxHoldSeconds = 20f)
    {
        Ensure();
        instance.DoHold(maxHoldSeconds);
    }

    public static void Release()
    {
        if (instance == null) return;
        instance.DoRelease();
    }

    private static void Ensure()
    {
        if (instance != null) return;
        var go = new GameObject("ScreenBlackout");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<ScreenBlackout>();
        instance.Build();
    }

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 31000;

        var imgGo = new GameObject("Cover", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imgGo.transform.SetParent(transform, false);
        cover = imgGo.GetComponent<Image>();
        cover.color = Color.black;
        cover.raycastTarget = false;
        var rt = cover.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        cover.gameObject.SetActive(false);
    }

    private void DoHold(float maxHoldSeconds)
    {
        int v = ++version;
        cover.gameObject.SetActive(true);
        if (maxHoldSeconds > 0f) AutoRelease(v, maxHoldSeconds).Forget();
    }

    private void DoRelease()
    {
        version++;
        if (cover != null) cover.gameObject.SetActive(false);
    }

    private async UniTaskVoid AutoRelease(int v, float seconds)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(seconds), ignoreTimeScale: true);
        if (v != version) return;
        Debug.LogWarning("[ScreenBlackout] Hold 超過時間沒有人 Release，已自動放開。");
        DoRelease();
    }
}
