using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[Serializable]
public class FontTestEntry
{
    [Tooltip("顯示用的名稱，例如：源雲明體 R")]
    public string label;
    public TMP_FontAsset font;
}

/// <summary>
/// 字體並排比較場景（不需要對話系統、不需要 Ink）：
///   全黑畫面，字體「三個一組並排」，分兩次顯示（Group Size 可改）。三種樣式各一輪：
///     純文字（黑底白字）→ 對話框（純白框 + 黑字）→ 旁白（黑底白字置中斜體）
///   6 種字體 = 每種樣式 2 頁，共 6 頁。
///   每頁停留 Page Seconds 秒，或按 F / 左鍵 / 空白鍵 / → 進下一頁，← 回上一頁。
///   最後一頁會一直停著。
///
/// 用法：新場景放一個空物件掛這支腳本，Fonts 填入要比較的字體即可，按 Play。
/// </summary>
public class FontTestScene : MonoBehaviour
{
    [Header("要比較的字體")]
    [SerializeField] private List<FontTestEntry> fonts = new List<FontTestEntry>();

    [Header("文字內容")]
    [SerializeField] private string speakerSample = "漁夫";
    [TextArea(2, 5)]
    [SerializeField] private string bodySample = "我愛這個世界。「人類」與神祗遺物，都在這裡。\n他說：『你還記得嗎？』……嗯，～ ABC abc 123";

    [Header("版面")]
    [Tooltip("每頁並排幾個字體（一組幾個）。6 種字體設 3 = 分兩次顯示。")]
    [SerializeField] private int groupSize = 3;
    [Tooltip("每個格子的高度（參考解析度 1080 高）。")]
    [SerializeField] private float cellHeight = 460f;
    [Tooltip("內文字級。想看實際遊戲大小，就填對話框內文的字級。")]
    [SerializeField] private float bodyFontSize = 34f;
    [SerializeField] private float speakerFontSize = 28f;
    [SerializeField] private float labelFontSize = 22f;
    [Header("對話框配色")]
    [SerializeField] private Color dialogueBoxColor = Color.white;
    [SerializeField] private Color dialogueBoxBorder = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color dialogueTextColor = Color.black;
    [SerializeField] private Color dialogueSpeakerColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    [Header("切換")]
    [Tooltip("每頁自動停留幾秒；0 = 只能手動切換。")]
    [SerializeField] private float pageSeconds = 12f;
    [SerializeField] private bool runOnStart = true;

    private Canvas canvas;
    private readonly List<GameObject> pages = new List<GameObject>();
    private int pageIndex;
    private float pageTimer;
    private bool running;

    private void Start()
    {
        if (runOnStart) Run();
    }

    [ContextMenu("Run Font Test")]
    private void Run()
    {
        var valid = fonts.FindAll(f => f != null && f.font != null);
        if (valid.Count == 0)
        {
            Debug.LogWarning("[FontTestScene] Fonts 清單是空的，請先填入要比較的字體。", this);
            return;
        }

        Build(valid);
        running = true;
        ShowPage(0);
    }

    private void Update()
    {
        if (!running || pages.Count == 0) return;

        pageTimer += Time.unscaledDeltaTime;

        bool next = (Keyboard.current != null && (Keyboard.current.fKey.wasPressedThisFrame
                                                  || Keyboard.current.spaceKey.wasPressedThisFrame
                                                  || Keyboard.current.rightArrowKey.wasPressedThisFrame))
                    || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        bool prev = Keyboard.current != null && Keyboard.current.leftArrowKey.wasPressedThisFrame;

        if (pageSeconds > 0f && pageTimer >= pageSeconds && pageIndex < pages.Count - 1) next = true;

        if (next && pageIndex < pages.Count - 1) ShowPage(pageIndex + 1);
        else if (prev && pageIndex > 0) ShowPage(pageIndex - 1);
    }

    private void ShowPage(int index)
    {
        pageIndex = index;
        pageTimer = 0f;
        for (int i = 0; i < pages.Count; i++) pages[i].SetActive(i == index);
    }

    // ───────────── 建 UI ─────────────

    private void Build(List<FontTestEntry> list)
    {
        if (canvas != null) Destroy(canvas.gameObject);
        pages.Clear();

        var go = new GameObject("FontTestCanvas", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 800;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var black = NewImage("Black", go.transform, Color.black);
        Stretch(black.rectTransform);

        int size = Mathf.Max(1, groupSize);
        foreach (PageStyle style in new[] { PageStyle.Plain, PageStyle.Dialogue, PageStyle.Narration })
        {
            int groupNo = 0;
            for (int start = 0; start < list.Count; start += size)
            {
                groupNo++;
                var group = list.GetRange(start, Mathf.Min(size, list.Count - start));
                pages.Add(BuildPage(go.transform, $"Page_{style}_{groupNo}", group, style));
            }
        }

        // 頁碼提示（用第一個字體）
        var hint = NewText("Hint", go.transform, 20f, TextAlignmentOptions.BottomRight);
        hint.font = list[0].font;
        hint.text = "← →　切換頁面";
        hint.color = new Color(1f, 1f, 1f, 0.35f);
        var hrt = hint.rectTransform;
        hrt.anchorMin = new Vector2(0f, 0f);
        hrt.anchorMax = new Vector2(1f, 0f);
        hrt.pivot = new Vector2(1f, 0f);
        hrt.anchoredPosition = new Vector2(-24f, 12f);
        hrt.sizeDelta = new Vector2(-48f, 36f);
    }

    private enum PageStyle { Plain, Dialogue, Narration }

    private GameObject BuildPage(Transform parent, string name, List<FontTestEntry> list, PageStyle style)
    {
        var page = new GameObject(name, typeof(RectTransform), typeof(GridLayoutGroup));
        page.transform.SetParent(parent, false);
        var prt = (RectTransform)page.transform;
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = new Vector2(50f, 60f);
        prt.offsetMax = new Vector2(-50f, -40f);

        int cols = Mathf.Max(1, list.Count);
        float spacing = 24f;
        float w = (1920f - 100f - spacing * (cols - 1)) / cols;
        float h = Mathf.Min(cellHeight, 980f);

        var grid = page.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(w, h);
        grid.spacing = new Vector2(spacing, spacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;
        grid.childAlignment = TextAnchor.MiddleCenter;

        foreach (var e in list) BuildCell(page.transform, e, style);
        return page;
    }

    private void BuildCell(Transform parent, FontTestEntry e, PageStyle style)
    {
        var cell = new GameObject("Cell_" + e.label, typeof(RectTransform));
        cell.transform.SetParent(parent, false);

        if (style == PageStyle.Dialogue)
        {
            var border = NewImage("Border", cell.transform, dialogueBoxBorder);
            Stretch(border.rectTransform);
            var box = NewImage("Box", cell.transform, dialogueBoxColor);
            Stretch(box.rectTransform);
            box.rectTransform.offsetMin = new Vector2(2f, 2f);
            box.rectTransform.offsetMax = new Vector2(-2f, -2f);
        }

        // 字體名稱小標籤
        var label = NewText("Label", cell.transform, labelFontSize, TextAlignmentOptions.TopRight);
        label.font = e.font;
        label.text = e.label;
        label.color = style == PageStyle.Dialogue ? new Color(0f, 0f, 0f, 0.45f) : new Color(1f, 1f, 1f, 0.45f);
        var lrt = label.rectTransform;
        lrt.anchorMin = new Vector2(0f, 1f);
        lrt.anchorMax = new Vector2(1f, 1f);
        lrt.pivot = new Vector2(1f, 1f);
        lrt.anchoredPosition = new Vector2(-14f, -8f);
        lrt.sizeDelta = new Vector2(-28f, 34f);

        float topOffset = 0f;

        if (style == PageStyle.Dialogue)
        {
            var speaker = NewText("Speaker", cell.transform, speakerFontSize, TextAlignmentOptions.TopLeft);
            speaker.font = e.font;
            speaker.text = speakerSample;
            speaker.color = dialogueSpeakerColor;
            var srt = speaker.rectTransform;
            srt.anchorMin = new Vector2(0f, 1f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot = new Vector2(0f, 1f);
            srt.anchoredPosition = new Vector2(24f, -16f);
            srt.sizeDelta = new Vector2(-48f, speakerFontSize + 16f);
            topOffset = speakerFontSize + 24f;
        }

        var align = style == PageStyle.Narration ? TextAlignmentOptions.Center
                   : style == PageStyle.Dialogue ? TextAlignmentOptions.TopLeft
                   : TextAlignmentOptions.Center;

        var body = NewText("Body", cell.transform, bodyFontSize, align);
        body.font = e.font;
        body.text = bodySample;
        body.textWrappingMode = TextWrappingModes.Normal;
        if (style == PageStyle.Dialogue) body.color = dialogueTextColor;
        if (style == PageStyle.Narration) body.fontStyle = FontStyles.Italic;
        var brt = body.rectTransform;
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(24f, 20f);
        brt.offsetMax = new Vector2(-24f, -(topOffset + 44f));
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI NewText(string name, Transform parent, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}