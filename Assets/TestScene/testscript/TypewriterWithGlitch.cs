using Febucci.TextAnimatorForUnity;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class TypewriterWithGlitch : MonoBehaviour
{
    [SerializeField] private TextAnimatorComponentBase textAnimator;

    [Header("Typing Settings")]
    [SerializeField] private float typingDelay = 0.1f;

    [Header("Glitch Settings")]
    [SerializeField] private float glitchCycleInterval = 0.05f;
    [SerializeField] private string charPool = "!@#$%^&*ABCXYZ0123456789";

    public UnityEvent onTextShowed;

    private readonly StringBuilder sb = new();
    private Coroutine activeCoroutine;
    private bool isTyping = false;

    public bool IsTyping => isTyping;

    private TMP_Text tmpText;

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
    }

    /// <summary>
    /// 播放帶有「依打字進度觸發亂碼」的文字
    /// </summary>
    /// <param name="text">富文本內容</param>
    /// <param name="glitchStartVisibleIndex">要產生連鎖亂碼的起始字元 Index（不含 Tag）</param>
    /// <param name="glitchLength">亂碼字元長度（例如 YOU 長度就是 3）</param>
    public void PlayTypewriterWithDynamicGlitch(string text, int glitchStartVisibleIndex = -1, int glitchLength = 0)
    {
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(Run(text, glitchStartVisibleIndex, glitchLength));
    }

    private IEnumerator Run(string text, int glitchStartIndex, int glitchLength)
    {
        isTyping = true;
        int totalVisibleChars = CountVisibleChars(text);
        int revealedCount = 0;

        while (revealedCount <= totalVisibleChars)
        {
            string displayText = BuildText(text, revealedCount, glitchStartIndex, glitchLength);
            textAnimator.SetText(displayText);

            yield return new WaitForSeconds(typingDelay);
            revealedCount++;
        }

        isTyping = false;
        onTextShowed?.Invoke();

        // 打字結束後，繼續維持亂碼持續跳動的 Frame
        if (glitchStartIndex >= 0 && glitchLength > 0)
        {
            while (true)
            {
                string displayText = BuildText(text, totalVisibleChars, glitchStartIndex, glitchLength);
                textAnimator.SetText(displayText);
                yield return new WaitForSeconds(glitchCycleInterval);
            }
        }
    }

    private string BuildText(string text, int revealedCount, int glitchStartIndex, int glitchLength)
    {
        sb.Clear();
        int visibleIndex = 0;
        bool inTag = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            // 忽略 Rich Text 標籤，不佔用可見字元 Index
            if (c == '<' && !inTag) { inTag = true; sb.Append(c); continue; }
            if (c == '>' && inTag) { inTag = false; sb.Append(c); continue; }
            if (inTag) { sb.Append(c); continue; }

            if (!char.IsWhiteSpace(c))
            {
                // 還沒被打字機打到的字，直接略過
                if (visibleIndex >= revealedCount)
                {
                    visibleIndex++;
                    continue;
                }

                bool isGlitched = false;

                // 判斷是否符合你的動態 Glitch 條件
                if (glitchStartIndex >= 0 && glitchLength > 0)
                {
                    int glitchEndIndex = glitchStartIndex + glitchLength - 1;

                    // 當打字進度涵蓋到這個字元時
                    if (visibleIndex >= glitchStartIndex && visibleIndex <= glitchEndIndex)
                    {
                        // 關鍵邏輯：目前打字機顯示總數(revealedCount)比當前字元大，代表「下一個字已經印出」-> 變成亂碼！
                        if (revealedCount > visibleIndex + 1)
                        {
                            isGlitched = true;
                        }
                    }
                }

                if (isGlitched)
                {
                    char randomChar = charPool[Random.Range(0, charPool.Length)];

                    // 強制這個亂碼字元使用「原本字元」的寬度
                    sb.Append($"<mspace={GetCharacterWidth(c):F3}em>{randomChar}</mspace>");
                }
                else
                {
                    sb.Append(c);
                }

                visibleIndex++;
            }
            else
            {
                sb.Append(c); // 保留空格
            }
        }

        return sb.ToString();
    }

    private int CountVisibleChars(string text)
    {
        int count = 0;
        bool inTag = false;
        foreach (char c in text)
        {
            if (c == '<' && !inTag) { inTag = true; continue; }
            if (c == '>' && inTag) { inTag = false; continue; }
            if (inTag) continue;
            if (!char.IsWhiteSpace(c)) count++;
        }
        return count;
    }

    private void OnDisable()
    {
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
    }

    private float GetCharacterWidth(char c)
    {
        if (tmpText == null || tmpText.font == null)
            return 1f;

        TMP_FontAsset fontAsset = tmpText.font;

        if (fontAsset.characterLookupTable.TryGetValue(c, out TMP_Character character))
        {
            if (fontAsset.glyphLookupTable.TryGetValue(
                character.glyph.index,
                out UnityEngine.TextCore.Glyph glyph))
            {
                // 轉換成 em 單位
                return glyph.metrics.horizontalAdvance / fontAsset.faceInfo.pointSize;
            }
        }

        // 找不到字元時使用預設值
        return 1f;
    }
}
