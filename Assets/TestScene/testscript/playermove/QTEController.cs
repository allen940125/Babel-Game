using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 限時按鍵 QTE：畫面顯示要按的鍵與倒數圓環，時間內按對就成功，
/// 按錯鍵或時間到就失敗。呼叫 RunQTE() 並 await 結果即可。
/// 移動用的按鍵（WASD、方向鍵）放在 Ignored Keys，按了不算按錯。
/// </summary>
public class QTEController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject qteRoot;
    [SerializeField] private TextMeshProUGUI keyLabel;
    [Tooltip("Image Type 設為 Filled、Fill Method 設為 Radial 360")]
    [SerializeField] private Image timerRing;

    [Header("回饋")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color successColor = new Color(0.4f, 1f, 0.4f);
    [SerializeField] private Color failColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private float resultShowSeconds = 0.35f;

    [Header("判定")]
    [Tooltip("這些鍵按了不算按錯（用來放移動鍵，避免閃避時誤判失敗）")]
    [SerializeField]
    private Key[] ignoredKeys =
    {
        Key.W, Key.A, Key.S, Key.D,
        Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow
    };

    public int SuccessCount { get; private set; }
    public int FailCount { get; private set; }

    /// <summary>每次 QTE 結束時觸發（true = 成功）。想加音效或特效可以訂閱這個。</summary>
    public event Action<bool> OnQTEFinished;

    private void Awake()
    {
        if (qteRoot != null) qteRoot.SetActive(false);
    }

    public async UniTask<bool> RunQTE(Key key, float timeLimit)
    {
        var token = this.GetCancellationTokenOnDestroy();

        qteRoot.SetActive(true);
        await UniTask.Yield(token); // 等剛啟用的物件完成 OnEnable

        keyLabel.text = key.ToString();
        SetColor(normalColor);
        timerRing.fillAmount = 1f;

        bool success = false;
        float remaining = timeLimit;

        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            timerRing.fillAmount = Mathf.Clamp01(remaining / timeLimit);

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard[key].wasPressedThisFrame)
                {
                    success = true;
                    break;
                }

                // 按了「不是目標、也不在忽略清單」的鍵 → 直接失敗
                bool wrongKey = false;
                foreach (var k in keyboard.allKeys)
                {
                    if (!k.wasPressedThisFrame) continue;
                    if (k.keyCode == key || IsIgnored(k.keyCode)) continue;
                    wrongKey = true;
                    break;
                }
                if (wrongKey) break;
            }

            await UniTask.Yield(token);
        }

        if (success) SuccessCount++; else FailCount++;
        SetColor(success ? successColor : failColor);

        await UniTask.Delay(TimeSpan.FromSeconds(resultShowSeconds), cancellationToken: token);
        qteRoot.SetActive(false);

        OnQTEFinished?.Invoke(success);
        return success;
    }

    private bool IsIgnored(Key key)
    {
        if (ignoredKeys == null) return false;
        foreach (var ignored in ignoredKeys)
            if (ignored == key) return true;
        return false;
    }

    private void SetColor(Color c)
    {
        timerRing.color = c;
        keyLabel.color = c;
    }
}