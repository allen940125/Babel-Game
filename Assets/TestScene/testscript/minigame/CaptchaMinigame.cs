using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class CaptchaOption
{
    [Tooltip("沒有圖片時顯示在色塊上的文字（例如：人類、狗、扭曲的人）")]
    public string label;
    [Tooltip("有素材就拖進來；留空則用下面的色塊 + 文字佔位")]
    public Sprite sprite;
    public Color color = new Color(0.8f, 0.8f, 0.8f);
}

[Serializable]
public class CaptchaRound
{
    [TextArea] public string prompt = "請點擊您認為是人類的東西";
    [Tooltip("答對後顯示的訊息（會取代題目文字，停留 Success Hold Seconds 秒）。\n用 {{ }} 包起來的文字會變成不停變化的亂碼，例如：[恭喜您，平行現實座標已確立，{{■★◇}}周目 即將開啟]（括號裡有幾個字就會是幾個亂碼字）")]
    [TextArea] public string successMessage;

    [Tooltip("正確答案的候選（每次洗牌會從中抽 1 個放進九宮格）")]
    public List<CaptchaOption> correctOptions = new List<CaptchaOption>();
    [Tooltip("錯誤選項的候選（每次洗牌會抽 8 個填滿其餘格子）")]
    public List<CaptchaOption> wrongOptions = new List<CaptchaOption>();

    [Tooltip("勾選後，這一輪兩側黑畫面會出現像人又不像人的東西（黑羊），記錄玩家有沒有去點它")]
    public bool showBlackSheepOnSides;

    [Tooltip("這一輪的逼近時間（秒）。0 = 使用 CaptchaMinigame 上的預設值。每一輪都會重新計時。")]
    public float timeLimitSeconds = 0f;
}

/// <summary>
/// 前導小遊戲：機器人檢查（九宮格 CAPTCHA）。
/// 每一輪九宮格裡有 1 個正確答案，點對就進下一輪；點錯會抖動、重新洗牌再試。
/// 每一輪都會各自重新計時，九宮格會隨時間越來越大（越來越逼近鏡頭），時間到會觸發 On Time Up
/// （jumpscare 之後掛在這個事件上）。
///
/// 放法：掛在 Canvas 底下一個「永遠啟用」的空物件上；Root 要指向它的子物件（會被開關）。
/// </summary>
public class CaptchaMinigame : MonoBehaviour
{
    [Header("版面")]
    [Tooltip("整個小遊戲的根物件（全螢幕）。不能是掛這支腳本的同一個物件。")]
    [SerializeField] private GameObject root;
    [Tooltip("會隨時間放大的區塊（題目 + 九宮格 + 背景）。Pivot 請設在中心。")]
    [SerializeField] private RectTransform approachRoot;
    [SerializeField] private TextMeshProUGUI promptText;
    [Tooltip("選填：答對訊息顯示在這個文字上，可以擺在跟題目不同的位置。留空則沿用 Prompt Text。")]
    [SerializeField] private TextMeshProUGUI successText;
    [Tooltip("有設定 Success Text 時，答對後是否隱藏題目文字")]
    [SerializeField] private bool hidePromptOnSuccess = true;
    [Tooltip("放九宮格的容器，上面要有 Grid Layout Group（3 欄）")]
    [SerializeField] private RectTransform gridRoot;
    [SerializeField] private CaptchaCell cellPrefab;
    [SerializeField] private int cellCount = 9;

    [Header("逼近鏡頭（越來越近）")]
    [Tooltip("勾選：每一輪各自重新計時，第一輪花多久都不影響第二輪。\n取消勾選：所有輪共用同一個計時，第一輪花越久，第二輪剩下的時間越少（此時只看下面的預設時間，Rounds 裡個別的時間會被忽略）。")]
    [SerializeField] private bool independentTimerPerRound = true;
    [Tooltip("每一輪的預設時間（秒），每一輪都從頭計時。可在 Rounds 的個別題目裡覆寫。0 = 不逼近、不倒數")]
    [SerializeField] private float timeLimitSeconds = 60f;
    [SerializeField] private float startScale = 0.6f;
    [SerializeField] private float endScale = 2.2f;
    [Tooltip("橫軸 = 時間進度 0~1，縱軸 = 放大進度 0~1。想要越到後面越快，就拉成往上彎的曲線")]
    [SerializeField] private AnimationCurve approachCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("時間到（jumpscare 預留）")]
    [SerializeField] private UnityEvent onTimeUp;

    [Header("兩側黑畫面（黑羊）")]
    [SerializeField] private GameObject sideBlackRoot;
    [Tooltip("兩側黑畫面裡那些『像人又不像人』的按鈕")]
    [SerializeField] private Button[] blackSheepButtons;
    [SerializeField] private UnityEvent onBlackSheepClicked;

    [Header("回饋")]
    [SerializeField] private Color correctColor = new Color(0.4f, 1f, 0.4f);
    [SerializeField] private Color wrongColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private float wrongShakeSeconds = 0.35f;
    [SerializeField] private float shakeStrength = 12f;
    [Tooltip("答對訊息『打完字之後』再停留幾秒")]
    [SerializeField] private float successHoldSeconds = 1.5f;

    [Header("答對訊息（打字機）")]
    [Tooltip("答對訊息每個字出現的間隔（秒）")]
    [SerializeField] private float successCharInterval = 0.08f;
    [Tooltip("答對訊息收起後，等幾秒才出現下一輪的題目（只在有設定 Success Text 時生效）")]
    [SerializeField] private float nextPromptDelay = 0.3f;

    [Header("亂碼文字（訊息裡用 {{ }} 包起來的部分）")]
    [Tooltip("亂碼會從這些字元裡隨機抽。請確認 TextMeshPro 的字型有這些字，否則會顯示成方框。\n建議只放寬度相近的全形符號（■□▲△●○◆◇★☆※）；英文字母、數字、# @ % & 比較窄，就算固定間距也會看起來有空隙。")]
    [SerializeField] private string scrambleCharacters = "■□▲△●○◆◇★☆※";
    [Tooltip("亂碼每個字元固定佔的寬度（單位：字高的倍數，em）。全形字約 1。0 = 不固定（文字會隨字元變寬變窄而晃動）。")]
    [SerializeField] private float scrambleCharWidthEm = 1f;
    [Tooltip("亂碼每隔幾秒換一次")]
    [SerializeField] private float scrambleInterval = 0.06f;
    [SerializeField] private UnityEvent onWrongClick;

    [Header("題目")]
    [SerializeField] private List<CaptchaRound> rounds = new List<CaptchaRound>();

    [Header("流程")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float startDelay = 0.5f;
    [Tooltip("全部通過後要切去的場景。留空就只觸發 On Completed。")]
    [SerializeField] private string nextSceneName;
    [SerializeField] private UnityEvent onCompleted;

    public bool TimedOut { get; private set; }
    public bool BlackSheepClicked { get; private set; }
    public int WrongClickCount { get; private set; }

    private readonly List<CaptchaCell> cells = new List<CaptchaCell>();
    private UniTaskCompletionSource<CaptchaCell> pendingClick;
    private Color promptBaseColor = Color.white;
    private bool timerRunning;
    private bool sharedTimerStarted;
    private bool blackSheepActive;
    private float elapsed;
    private float currentTimeLimit;

    private void Awake()
    {
        if (promptText != null) promptBaseColor = promptText.color;
        if (successText != null) successText.gameObject.SetActive(false);

        for (int i = 0; i < cellCount; i++)
        {
            var cell = Instantiate(cellPrefab, gridRoot);
            cell.name = $"Cell {i + 1}";
            cell.Init();
            cell.Clicked += OnCellClicked;
            cells.Add(cell);
        }

        foreach (var b in blackSheepButtons)
        {
            if (b == null) continue;
            var captured = b;
            b.onClick.AddListener(() => OnBlackSheepButton(captured));
        }

        root.SetActive(false);
        if (sideBlackRoot != null) sideBlackRoot.SetActive(false);
    }

    private void Start()
    {
        if (playOnStart) StartAfterDelay().Forget();
    }

    private async UniTaskVoid StartAfterDelay()
    {
        var token = this.GetCancellationTokenOnDestroy();
        await UniTask.Delay(TimeSpan.FromSeconds(startDelay), cancellationToken: token);
        await Play();
    }

    private void Update()
    {
        if (!timerRunning) return;

        elapsed += Time.deltaTime;

        if (currentTimeLimit > 0f)
        {
            float t = Mathf.Clamp01(elapsed / currentTimeLimit);
            float scale = Mathf.LerpUnclamped(startScale, endScale, approachCurve.Evaluate(t));
            approachRoot.localScale = new Vector3(scale, scale, 1f);

            if (!TimedOut && elapsed >= currentTimeLimit)
            {
                TimedOut = true;
                Debug.Log("[CaptchaMinigame] 時間到（jumpscare 預留點）");
                onTimeUp?.Invoke();
            }
        }
    }

    /// <summary>開始小遊戲。可以由別的流程呼叫並 await，通過全部題目後才會結束。</summary>
    public async UniTask Play()
    {
        var token = this.GetCancellationTokenOnDestroy();

        // 小遊戲需要滑鼠
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        TimedOut = false;
        BlackSheepClicked = false;
        WrongClickCount = 0;
        timerRunning = false;
        sharedTimerStarted = false;
        approachRoot.localScale = new Vector3(startScale, startScale, 1f);

        root.SetActive(true);
        await UniTask.Yield(token); // 等剛啟用的物件完成 OnEnable

        foreach (var round in rounds)
            await RunRound(round, token);

        timerRunning = false;
        root.SetActive(false);
        if (sideBlackRoot != null) sideBlackRoot.SetActive(false);

        onCompleted?.Invoke();

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    private async UniTask RunRound(CaptchaRound round, CancellationToken token)
    {
        promptText.gameObject.SetActive(true);
        promptText.maxVisibleCharacters = 99999; // 還原打字機造成的字數限制
        if (successText != null) successText.gameObject.SetActive(false);
        promptText.color = promptBaseColor;
        promptText.text = round.prompt;
        SetBlackSheep(round.showBlackSheepOnSides);
        StartRoundTimer(round);

        while (true)
        {
            if (!BuildGrid(round)) return; // 題目資料不完整，略過這一輪

            SetCellsInteractable(true);
            pendingClick = new UniTaskCompletionSource<CaptchaCell>();
            var clicked = await pendingClick.Task.AttachExternalCancellation(token);
            SetCellsInteractable(false);

            if (clicked.IsCorrect)
            {
                // 獨立計時：答對後暫停，成功訊息停留期間不會繼續放大。共用計時：照常往下走。
                if (independentTimerPerRound) timerRunning = false;
                clicked.SetTint(correctColor);
                await ShowSuccessMessage(round.successMessage, token);

                if (successText != null)
                {
                    successText.gameObject.SetActive(false);
                    if (nextPromptDelay > 0f)
                        await UniTask.Delay(TimeSpan.FromSeconds(nextPromptDelay), cancellationToken: token);
                }
                break;
            }

            // 點錯：標紅、抖動、重新洗牌再試
            WrongClickCount++;
            clicked.SetTint(wrongColor);
            onWrongClick?.Invoke();
            await ShakeGrid(token);
        }

        SetBlackSheep(false);
    }

    /// <summary>
    /// 顯示答對訊息：先以打字機效果一個字一個字打出來，打完再停留 successHoldSeconds 秒。
    /// 訊息裡用 {{ }} 包起來的部分會不停變成隨機字元（亂碼效果），打字與亂碼可以同時進行。
    /// 這裡用 TextMeshPro 的 maxVisibleCharacters 自己做打字機，
    /// 因為亂碼需要每隔一小段時間重設文字，會讓 Text Animator 的打字機從頭重打。
    /// </summary>
    private async UniTask ShowSuccessMessage(string message, CancellationToken token)
    {
        if (string.IsNullOrEmpty(message))
        {
            await UniTask.Delay(TimeSpan.FromSeconds(successHoldSeconds), cancellationToken: token);
            return;
        }

        // 有獨立的 Success Text 就顯示在那裡，並先關掉題目；否則沿用題目文字
        TextMeshProUGUI target = promptText;
        if (successText != null)
        {
            target = successText;
            if (hidePromptOnSuccess) promptText.gameObject.SetActive(false);
            successText.gameObject.SetActive(true);
        }

        bool hasScramble = message.Contains("{{");

        target.maxVisibleCharacters = 0;
        target.text = BuildScrambledMessage(message);
        target.ForceMeshUpdate();
        int totalChars = target.textInfo.characterCount;

        int visible = 0;
        float typeTimer = 0f;
        float scrambleTimer = 0f;
        float holdTimer = 0f;

        while (true)
        {
            float dt = Time.deltaTime;

            // 打字機：每隔 successCharInterval 多顯示一個字
            if (visible < totalChars)
            {
                typeTimer += dt;
                float interval = Mathf.Max(0.01f, successCharInterval);
                while (visible < totalChars && typeTimer >= interval)
                {
                    visible++;
                    typeTimer -= interval;
                }
            }
            else
            {
                // 全部打完後才開始算停留時間
                holdTimer += dt;
                if (holdTimer >= successHoldSeconds) break;
            }

            // 亂碼：每隔 scrambleInterval 換一次字元（長度不變，所以打字進度不受影響）
            if (hasScramble)
            {
                scrambleTimer += dt;
                if (scrambleTimer >= Mathf.Max(0.01f, scrambleInterval))
                {
                    scrambleTimer = 0f;
                    target.text = BuildScrambledMessage(message);
                }
            }

            target.maxVisibleCharacters = visible;
            await UniTask.Yield(token);
        }

        target.maxVisibleCharacters = 99999;
    }

    private string BuildScrambledMessage(string message)
    {
        string pool = string.IsNullOrEmpty(scrambleCharacters) ? "■" : scrambleCharacters;
        var sb = new System.Text.StringBuilder();
        int i = 0;

        while (i < message.Length)
        {
            int start = message.IndexOf("{{", i, StringComparison.Ordinal);
            if (start < 0) { sb.Append(message, i, message.Length - i); break; }

            int end = message.IndexOf("}}", start + 2, StringComparison.Ordinal);
            if (end < 0) { sb.Append(message, i, message.Length - i); break; }

            sb.Append(message, i, start - i);

            int length = end - (start + 2);

            // 用 TextMeshPro 的 <mspace> 讓亂碼每個字元佔一樣的寬度，
            // 這樣字元怎麼換，整句話的長度都不會變。標籤本身不算可見字元，不影響打字機。
            bool fixedWidth = scrambleCharWidthEm > 0f;
            if (fixedWidth)
                sb.Append("<mspace=").Append(scrambleCharWidthEm.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)).Append("em>");

            for (int k = 0; k < length; k++)
                sb.Append(pool[UnityEngine.Random.Range(0, pool.Length)]);

            if (fixedWidth) sb.Append("</mspace>");

            i = end + 2;
        }
        return sb.ToString();
    }

    /// <summary>每一輪開始時：計時歸零、畫面縮回起始大小、重新套用這一輪的時間。</summary>
    private void StartRoundTimer(CaptchaRound round)
    {
        if (!independentTimerPerRound)
        {
            // 共用計時：只在第一輪開始時啟動，之後的輪次接著算
            if (sharedTimerStarted) return;
            sharedTimerStarted = true;

            elapsed = 0f;
            TimedOut = false;
            currentTimeLimit = timeLimitSeconds;
            approachRoot.localScale = new Vector3(startScale, startScale, 1f);
            timerRunning = true;
            return;
        }

        elapsed = 0f;
        TimedOut = false;
        currentTimeLimit = round.timeLimitSeconds > 0f ? round.timeLimitSeconds : timeLimitSeconds;
        approachRoot.localScale = new Vector3(startScale, startScale, 1f);
        timerRunning = true;
    }

    // ───────────── 九宮格 ─────────────

    private bool BuildGrid(CaptchaRound round)
    {
        if (round.correctOptions == null || round.correctOptions.Count == 0 ||
            round.wrongOptions == null || round.wrongOptions.Count == 0)
        {
            Debug.LogError("[CaptchaMinigame] 這一輪的正確選項或錯誤選項是空的，請在 Inspector 補上。");
            return false;
        }

        var entries = new List<(CaptchaOption option, bool correct)>();

        // 1 個正確答案
        var correct = round.correctOptions[UnityEngine.Random.Range(0, round.correctOptions.Count)];
        entries.Add((correct, true));

        // 其餘填錯誤選項（不夠就循環使用）
        var wrongPool = new List<CaptchaOption>(round.wrongOptions);
        Shuffle(wrongPool);
        for (int i = 0; i < cells.Count - 1; i++)
            entries.Add((wrongPool[i % wrongPool.Count], false));

        Shuffle(entries);

        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].Setup(entries[i].option, entries[i].correct);
            cells[i].ResetTint();
        }
        return true;
    }

    private void SetCellsInteractable(bool value)
    {
        foreach (var c in cells) c.SetInteractable(value);
    }

    private void OnCellClicked(CaptchaCell cell)
    {
        if (pendingClick == null) return;
        var p = pendingClick;
        pendingClick = null;
        p.TrySetResult(cell);
    }

    private async UniTask ShakeGrid(CancellationToken token)
    {
        Vector2 origin = gridRoot.anchoredPosition;
        float t = 0f;
        while (t < wrongShakeSeconds)
        {
            t += Time.deltaTime;
            gridRoot.anchoredPosition = origin + UnityEngine.Random.insideUnitCircle * shakeStrength;
            await UniTask.Yield(token);
        }
        gridRoot.anchoredPosition = origin;
    }

    // ───────────── 黑羊 ─────────────

    private void SetBlackSheep(bool show)
    {
        blackSheepActive = show;
        if (sideBlackRoot == null) return;

        sideBlackRoot.SetActive(show);
        if (!show) return;

        foreach (var b in blackSheepButtons)
            if (b != null) b.gameObject.SetActive(true);
    }

    private void OnBlackSheepButton(Button button)
    {
        if (!blackSheepActive) return;

        BlackSheepClicked = true;
        button.gameObject.SetActive(false); // 被點到就消失
        Debug.Log("[CaptchaMinigame] 玩家點了黑羊");
        onBlackSheepClicked?.Invoke();
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}