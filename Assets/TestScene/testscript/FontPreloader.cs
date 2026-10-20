using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// 依照你的字表，把字預先補進「動態」TMP 字體資產，並回報哪些字所有字體都缺。
///
/// 用法：
///   1. 場景放一個空物件掛這支腳本（放在第一個場景，或常駐物件上）。
///   2. Character Table 拖入你的字表 txt（UTF-8）。
///   3. Font Assets 依優先順序加入動態字體資產（主力字體在最上面，備案在後面）。
///      每個資產的 Atlas Population Mode 必須是 Dynamic，靜態資產會被略過並提醒。
///   4. 執行遊戲（或在元件右上選單點 Preload Now），看 Console 的報告。
///
/// 原理：對每個字體資產，把「還沒補到的字」交給它 TryAddCharacters，
/// 它有的就補進圖集，沒有的回傳給下一個字體資產處理。最後剩下的就是全部字體都缺的字。
/// </summary>
public class FontPreloader : MonoBehaviour
{
    [Header("字表")]
    [Tooltip("UTF-8 純文字檔，內容是遊戲會用到的所有字。重複的字、空白、換行都會自動忽略。")]
    [SerializeField] private TextAsset characterTable;

    [Header("字體資產（優先順序由上到下，必須是 Dynamic）")]
    [SerializeField] private List<TMP_FontAsset> fontAssets = new List<TMP_FontAsset>();

    [Header("執行")]
    [SerializeField] private bool runOnStart = true;

    private void Start()
    {
        if (runOnStart) Preload();
    }

    [ContextMenu("Preload Now")]
    public void Preload()
    {
        if (characterTable == null)
        {
            Debug.LogWarning("[FontPreloader] 尚未指定 Character Table。", this);
            return;
        }

        string remaining = Clean(characterTable.text);
        int total = CountCodepoints(remaining);
        Debug.Log($"[FontPreloader] 字表共 {total} 個不重複的字。");

        foreach (var asset in fontAssets)
        {
            if (asset == null || remaining.Length == 0) continue;

            if (asset.atlasPopulationMode != AtlasPopulationMode.Dynamic)
            {
                Debug.LogWarning($"[FontPreloader] {asset.name} 不是 Dynamic，無法補字，已略過。" +
                                 "（靜態資產請改用 Font Asset Creator 的 Characters from File。）", asset);
                continue;
            }

            int before = CountCodepoints(remaining);
            asset.TryAddCharacters(remaining, out string missing);
            missing = missing ?? string.Empty;
            int added = before - CountCodepoints(missing);

            Debug.Log($"[FontPreloader] {asset.name}：補入 {added} 個字，這個字體沒有的有 {CountCodepoints(missing)} 個。", asset);
            remaining = missing;
        }

        if (remaining.Length > 0)
            Debug.LogWarning($"[FontPreloader] 所有字體都缺的字（{CountCodepoints(remaining)} 個）：{remaining}", this);
        else
            Debug.Log("[FontPreloader] 字表裡的字全部都有字體可以補。");
    }

    /// <summary>去掉空白、換行、控制字元與重複的字，保留代理對（罕用漢字）。</summary>
    private static string Clean(string raw)
    {
        var seen = new HashSet<int>();
        var sb = new StringBuilder();

        for (int i = 0; i < raw.Length; i++)
        {
            int codepoint;
            int length = 1;

            if (char.IsHighSurrogate(raw[i]) && i + 1 < raw.Length && char.IsLowSurrogate(raw[i + 1]))
            {
                codepoint = char.ConvertToUtf32(raw[i], raw[i + 1]);
                length = 2;
            }
            else
            {
                codepoint = raw[i];
            }

            if (codepoint == 0xFEFF) { i += length - 1; continue; } // BOM
            if (codepoint <= 0xFFFF && (char.IsWhiteSpace((char)codepoint) || char.IsControl((char)codepoint)))
            {
                i += length - 1;
                continue;
            }

            if (seen.Add(codepoint))
                sb.Append(raw, i, length);

            i += length - 1;
        }
        return sb.ToString();
    }

    private static int CountCodepoints(string s)
    {
        int count = 0;
        for (int i = 0; i < s.Length; i++)
        {
            if (char.IsHighSurrogate(s[i]) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1])) i++;
            count++;
        }
        return count;
    }
}
