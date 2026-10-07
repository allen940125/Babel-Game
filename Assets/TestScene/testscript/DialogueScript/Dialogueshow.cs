using Cysharp.Threading.Tasks;
using Febucci.TextAnimatorForUnity;
using Game.UI;
using Ink.Runtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class Dialogueshow : BasePanel
{
    [Header("Text")]
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TypewriterComponent typewriter;

    [Header("Choices")]
    [SerializeField] private GameObject[] choiceButtons;
    [SerializeField] private TextMeshProUGUI[] choiceTexts;

    [Header("Portraits")]
    [SerializeField] private Image leftPortrait;
    [SerializeField] private Image rightPortrait;
    [SerializeField] private Image CenterPortrait;
    [SerializeField] private List<PortraitEntry> portraitDatabase;
    private Dictionary<string, Sprite> portraitMap;

    [Header("Skip")]
    [SerializeField] private Button skipButton;
    [SerializeField] private GameObject synopsisPopup;
    [SerializeField] private TextMeshProUGUI synopsisText;
    [SerializeField] private Button synopsisConfirmButton;
    [SerializeField] private Button synopsisCancelButton;

    [Header("立繪效果")]
    [SerializeField] private Color portraitDimColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private float portraitFadeOutDuration = 0.25f;
    [SerializeField] private float portraitFadeInDuration = 0.25f;

    [Header("節奏設定")]
    [SerializeField] private float readingPauseSeconds = 1.5f;
    [Tooltip("最後一句打完字後，玩家按一下（F / 左鍵）就關閉；或停這麼多秒後自動關閉，先到的先算。0 = 只能按鍵才關閉。")]
    [SerializeField] private float lastLinePauseSeconds = 3f;

    [Header("舞台效果")]
    [SerializeField] private DialogueStageEffects stage;
    private string pendingRelicKey;

    private bool waitingForChoice;
    private bool synopsisPopupOpen;

    private List<Choice> pendingChoices;

    private Image pendingPortraitToBrighten;

    private Image lastActivePortrait;

    private readonly Dictionary<Image, int> portraitFadeVersion = new();
    public void Init()
    {
        Debug.Log("[Dialogue] Init() 被呼叫");

        portraitMap = new Dictionary<string, Sprite>();
        foreach (var entry in portraitDatabase)
            portraitMap[entry.key] = entry.sprite;

        HideChoices();
        synopsisPopup.SetActive(false);
        synopsisPopupOpen = false;

        skipButton.onClick.RemoveAllListeners();
        skipButton.onClick.AddListener(OnSkipClicked);
        synopsisConfirmButton.onClick.RemoveAllListeners();
        synopsisConfirmButton.onClick.AddListener(OnSkipConfirmed);
        synopsisCancelButton.onClick.RemoveAllListeners();
        synopsisCancelButton.onClick.AddListener(OnSkipCancelled);
        typewriter.onTextShowed.AddListener(OnTypewriterFinished);

        leftPortrait.gameObject.SetActive(false);
        rightPortrait.gameObject.SetActive(false);
        CenterPortrait.gameObject.SetActive(false);

        ContinueStory();
        //初始化
    }

    private void Update()
    {
        if (!Dialoguecontroller.Instance.DialogueIsPlaying) return;
        if (stage != null && stage.IsBusy) return; // 道具展示中，不處理跳過打字
        if (waitingForChoice || synopsisPopupOpen) return;
        if (Keyboard.current == null && Mouse.current == null) return;

        if (!ClickedThisFrame()) return;

        if (typewriter.IsShowingText)//爭測跳過
            typewriter.SkipTypewriter();
        // 不再需要手動呼叫 ContinueStory()，交給 OnTypewriterFinished -> WaitThenAdvance 自動接手
    }

    private static bool ClickedThisFrame()
    {
        return (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
    }

    private void ContinueStory()
    {
        var story = Dialoguecontroller.Instance.CurrentStory;
        if (story == null) return;

        HideChoices();//拿INK

        if (story.canContinue)
        {
            string line = "";
            while (story.canContinue)
            {
                line = story.Continue();
                HandleTags(story.currentTags);
                if (!string.IsNullOrWhiteSpace(line))
                    break;//防止讀到INK的#行
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                typewriter.ShowText(line);
                // 立繪淡入變亮的時機交給 onTextShowed 統一處理，這裡先不做
            }
            else
            {
                FinishAdvancingToChoicesOrEnd();
            }
        }
        else
        {
            FinishAdvancingToChoicesOrEnd();
        }
        //打字機
    }

    private void OnTypewriterFinished()
    {
        var story = Dialoguecontroller.Instance.CurrentStory;
        if (story == null) return;

        if (pendingPortraitToBrighten != null)
        {
            FadePortraitIn(pendingPortraitToBrighten).Forget();
            pendingPortraitToBrighten = null;
        }

        // 道具展示要先開始（IsBusy 變 true），後面的等待才會知道要等它
        if (!string.IsNullOrEmpty(pendingRelicKey) && stage != null)
        {
            string key = pendingRelicKey;
            pendingRelicKey = null;
            stage.ShowRelic(key).Forget();
        }

        if (!story.canContinue)
        {
            if (story.currentChoices.Count > 0)
                FinishAdvancingToChoicesOrEnd();   // 有選項就直接顯示，不用等
            else
                EndAfterLastLine(story).Forget();  // 最後一句：等玩家按一下或停幾秒再關閉
        }
        else
        {
            WaitThenAdvance().Forget();
        }
    }

    private async UniTaskVoid WaitThenAdvance()
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds(readingPauseSeconds));

        // 玩家叫出大綱、或道具展示中：先等它們結束，再繼續（取消跳過後不會卡住）
        while (synopsisPopupOpen || (stage != null && stage.IsBusy))
            await UniTask.Yield();

        if (waitingForChoice) return;

        ContinueStory();
    }

    /// <summary>最後一句打完字後：等玩家按一下（F / 左鍵）或停 lastLinePauseSeconds 秒，再關閉對話。</summary>
    private async UniTaskVoid EndAfterLastLine(Story story)
    {
        // 道具展示中先等它結束
        while (stage != null && stage.IsBusy)
            await UniTask.Yield();

        // 先等一幀，避免「跳過打字」的那一下點擊直接被當成關閉
        await UniTask.Yield();

        float waited = 0f;
        while (IsStillPlaying(story))
        {
            if (synopsisPopupOpen)
            {
                // 大綱視窗開著時暫停計時，也不接受按鍵
                await UniTask.Yield();
                continue;
            }

            waited += Time.deltaTime;
            if (lastLinePauseSeconds > 0f && waited >= lastLinePauseSeconds) break;
            if (ClickedThisFrame()) break;

            await UniTask.Yield();
        }

        // 等待期間對話可能已經被結束（跳過、ESC 等），就不要再重複結束
        if (!IsStillPlaying(story)) return;

        FinishAdvancingToChoicesOrEnd();
    }

    private static bool IsStillPlaying(Story story)
    {
        var dc = Dialoguecontroller.Instance;
        return dc != null && dc.DialogueIsPlaying && dc.CurrentStory == story;
    }

    private void FinishAdvancingToChoicesOrEnd()
    {
        var story = Dialoguecontroller.Instance.CurrentStory;

        if (pendingPortraitToBrighten != null)
        {
            FadePortraitIn(pendingPortraitToBrighten).Forget();
            pendingPortraitToBrighten = null;
        }

        if (story.currentChoices.Count > 0)
            DisplayChoices(story.currentChoices);
        else
            Dialoguecontroller.Instance.EndDialogue();

        //檢查是否全部對話&選項跑完，跑完就關對話框
    }


    private void DisplayChoices(List<Choice> currentChoices)
    {
        waitingForChoice = true;//擋update

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < currentChoices.Count)
            {
                int choiceIndex = i;
                choiceButtons[i].SetActive(true);
                choiceTexts[i].text = currentChoices[i].text;

                var btn = choiceButtons[i].GetComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnChoiceSelected(choiceIndex));
            }
            else
            {
                choiceButtons[i].SetActive(false);
            }
        }

        if (currentChoices.Count > choiceButtons.Length)
            Debug.LogWarning($"Ink 選項數量({currentChoices.Count})超過 UI 準備的按鈕數量({choiceButtons.Length}),請增加 choiceButtons 陣列大小。");
        //顯示選項
    }

    private void OnChoiceSelected(int index)
    {
        waitingForChoice = false;
        Dialoguecontroller.Instance.CurrentStory.ChooseChoiceIndex(index);
        ContinueStory();
        //玩家選選項
    }

    private void HideChoices()
    {
        waitingForChoice = false;
        foreach (var choice in choiceButtons)
            choice.SetActive(false);
        //關掉選項
    }

    private void HandleTags(List<string> tags)
    {
        foreach (var raw in tags)
        {
            string tag = raw.Trim();

            if (tag.StartsWith("speaker:"))
            {
                speakerNameText.text = tag.Substring("speaker:".Length).Trim();
            }
            else if (tag.StartsWith("portrait:"))
            {
                var parts = tag.Substring("portrait:".Length).Trim()
                    .Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    SetPortrait(parts[0], parts[1]);
            }
            else if (tag.StartsWith("bg:"))
            {
                stage?.SetBlack(tag.Substring("bg:".Length).Trim() == "black");
            }
            else if (tag.StartsWith("relic:"))
            {
                pendingRelicKey = tag.Substring("relic:".Length).Trim();
            }
        }
        //檢查INK的內容，是在說話還是立繪
    }

    private Image GetPortraitSlot(string side)
    {
        return side switch
        {
            "left" => leftPortrait,
            "right" => rightPortrait,
            "center" => CenterPortrait,
            _ => null
        };
        //讀INK裡對立繪位置的設定
    }

    private void SetPortrait(string side, string key)
    {
        Image target = GetPortraitSlot(side);
        if (target == null) return;

        if (key == "none")
        {
            int version = BumpFadeVersion(target);
            FadePortraitOut(target, version).Forget();
            if (lastActivePortrait == target) lastActivePortrait = null;
            return;
        }
        //立繪消失

        if (!portraitMap.TryGetValue(key, out var sprite))
        {
            Debug.LogWarning($"找不到立繪:{key}");
            return;
        }

        BumpFadeVersion(target);

        target.sprite = sprite;
        target.color = portraitDimColor;
        target.gameObject.SetActive(true);
        //如果有人在說話，就去找立繪，並壓暗

        if (lastActivePortrait != null && lastActivePortrait != target && lastActivePortrait.gameObject.activeSelf)
            lastActivePortrait.color = portraitDimColor;

        pendingPortraitToBrighten = target;
        lastActivePortrait = target;
        //如果上一個亮著(在說話)的立繪存在且不是同一個位置(換人說話)，且需要他仍然存在則壓暗。
    }

    // ---------- 跳過 + 大綱 ----------

    private void OnSkipClicked()
    {
        var story = Dialoguecontroller.Instance.CurrentStory;
        if (story == null) return;

        string synopsis = GetCurrentKnotSynopsis(story);
        synopsisText.text = string.IsNullOrEmpty(synopsis) ? "（這段劇情沒有提供大綱）" : synopsis;

        synopsisPopup.SetActive(true);
        synopsisPopupOpen = true;
        //跳出大綱
    }

    private string GetCurrentKnotSynopsis(Story story)
    {
        try
        {
            string currentPath = story.state.currentPathString;
            if (string.IsNullOrEmpty(currentPath)) return null;

            string knotName = currentPath.Split('.')[0];
            var tags = story.TagsForContentAtPath(knotName);
            if (tags == null) return null;

            foreach (var tag in tags)
            {
                if (tag.Trim().StartsWith("synopsis:"))
                    return tag.Trim().Substring("synopsis:".Length).Trim();
            }
        }
        catch
        {
            // 找不到對應路徑時忽略即可
        }
        return null;
        //找有沒有synopsis開頭的(大綱)
    }

    private void OnSkipConfirmed()
    {
        synopsisPopup.SetActive(false);
        synopsisPopupOpen = false;

        typewriter.SkipTypewriter();

        Dialoguecontroller.Instance.EndDialogue();

        //確認跳過
    }

    private void OnSkipCancelled()
    {
        synopsisPopup.SetActive(false);
        synopsisPopupOpen = false;
        //取消跳過
    }


    private async UniTaskVoid FadePortraitIn(Image target)
    {
        float elapsed = 0f;
        Color from = portraitDimColor;
        Color to = Color.white;

        while (elapsed < portraitFadeInDuration)
        {
            elapsed += Time.deltaTime;
            target.color = Color.Lerp(from, to, elapsed / portraitFadeInDuration);
            await UniTask.Yield();
        }
        target.color = to;
        //淡入設定
    }

    private int BumpFadeVersion(Image img)
    {
        int v = portraitFadeVersion.TryGetValue(img, out var cur) ? cur + 1 : 1;
        portraitFadeVersion[img] = v;
        return v;
        //讓還沒完全淡出的立繪變成舊版本
    }

    private async UniTaskVoid FadePortraitOut(Image target, int version)
    {
        if (!target.gameObject.activeSelf) return;

        float elapsed = 0f;
        Color from = target.color;
        Color to = new Color(from.r, from.g, from.b, 0f);

        while (elapsed < portraitFadeOutDuration)   // 改這裡
        {
            if (portraitFadeVersion.TryGetValue(target, out var cur) && cur != version)
                return;

            elapsed += Time.deltaTime;
            target.color = Color.Lerp(from, to, elapsed / portraitFadeOutDuration);   // 改這裡
            await UniTask.Yield();
        }

        if (portraitFadeVersion.TryGetValue(target, out var finalVersion) && finalVersion == version)
        {
            target.color = to;
            target.gameObject.SetActive(false);
        }
    }
    //淡出設定
}

[System.Serializable]
public class PortraitEntry
{
    public string key;
    public Sprite sprite;
    //資料結構

}