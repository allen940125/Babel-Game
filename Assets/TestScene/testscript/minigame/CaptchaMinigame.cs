using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
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

/// <summary>點錯或時間到時要發生什麼。</summary>
public enum FailBehavior
{
    Jumpscare,   // 播放驚嚇演出（CaptchaMinigame 的 Jumpscare 欄位）
    ShowMessage  // 顯示失敗訊息（Fail Message），再依 After Fail 決定重來或繼續
}

/// <summary>失敗訊息顯示完之後。</summary>
public enum AfterFail
{
    Continue,  // 這一輪結束，進入下一輪（或全部結束）
    Retry      // 這一輪重來（重新洗牌、重新計時）
}

[Serializable]
public class CaptchaRound
{
    [Header("題目")]
    [Tooltip("題目文字。用 {{ }} 包起來的部分會不停閃爍變成亂碼，例如：請點擊您認為是 {{人類}} 的東西")]
    [TextArea] public string prompt = "請點擊您認為是人類的東西";
    [Tooltip("答對後顯示的訊息。用 {{ }} 包起來的文字會變成不停變化的亂碼。")]
    [TextArea] public string successMessage;

    [Header("九宮格")]
    [Tooltip("正確答案的候選（每次洗牌抽 1 個放進九宮格）")]
    public List<CaptchaOption> correctOptions = new List<CaptchaOption>();
    [Tooltip("錯誤選項的候選（每次洗牌抽 8 個填滿其餘格子）")]
    public List<CaptchaOption> wrongOptions = new List<CaptchaOption>();

    [Header("時間")]
    [Tooltip("這一輪的逼近時間（秒）。0 = 使用 CaptchaMinigame 上的預設值。每一輪都會重新計時。")]
    public float timeLimitSeconds = 0f;

    [Header("額外要點擊的東西（除了九宮格的正確答案以外）")]
    [Tooltip("勾選：題目裡 {{ }} 那個閃爍的字也要被點擊。順序不拘。")]
    public bool requirePromptClick;
    [Tooltip("勾選：右下角的確認按鈕（HumanCheck）也要被點擊。")]
    public bool requireHumanCheckClick;
    [Tooltip("這一輪右下角按鈕上的文字（例如：人類）。留空 = 維持原本的文字。")]
    public string humanCheckLabel;

    [Header("失敗處理")]
    [Tooltip("第幾次點錯才算失敗。1 = 第一次就失敗；2 = 第一次點錯只有懲罰，第二次才失敗。")]
    public int wrongClicksBeforeFail = 1;
    [Tooltip("還沒到失敗次數的點錯，會讓計時瞬間快轉這麼多秒（圖片瞬間逼近）。0 = 沒有懲罰。")]
    public float wrongClickPenaltySeconds = 0f;
    [Tooltip("達到失敗次數時")]
    public FailBehavior wrongClickBehavior = FailBehavior.Jumpscare;
    [Tooltip("時間到時")]
    public FailBehavior timeUpBehavior = FailBehavior.Jumpscare;
    [Tooltip("失敗時顯示的文字（只在選 ShowMessage 時使用）。用 {{ }} 包起來的會變亂碼。")]
    [TextArea] public string failMessage;
    [Tooltip("失敗訊息顯示完之後：Continue = 往下走；Retry = 這一輪重來")]
    public AfterFail afterFail = AfterFail.Continue;
}

/// <summary>
/// 前導小遊戲：機器人檢查（九宮格 CAPTCHA）。
///
/// 每一輪：九宮格有 1 個正確答案；可另外要求點「題目裡閃爍的字」與「右下角確認按鈕」，全部完成才算過。
/// 九宮格隨時間逼近（放大），每一輪各自計時。
/// 點錯：可設定幾次後才失敗，之前的點錯只會讓計時快轉；失敗與時間到各自可選驚嚇演出或顯示文字。
///
/// 放法：掛在 Canvas 底下一個「永遠啟用」的空物件上；Root 要指向它的子物件（會被開關）。
/// </summary>
public class CaptchaMinigame : MonoBehaviour
{
    [Header("版面")]
    [Tooltip("整個小遊戲的根物件。不能是掛這支腳本的同一個物件。")]
    [SerializeField] private GameObject root;
    [Tooltip("會隨時間放大的區塊（九宮格）。Pivot 請設在中心。")]
    [SerializeField] private RectTransform approachRoot;
    [Tooltip("放九宮格的容器，上面要有 Grid Layout Group（3 欄）")]
    [SerializeField] private RectTransform gridRoot;
    [SerializeField] private CaptchaCell cellPrefab;
    [SerializeField] private int cellCount = 9;
    [SerializeField] private TextMeshProUGUI promptText;
    [Tooltip("選填：答對／失敗訊息顯示在這個文字上（顯示時會隱藏題目）。留空則沿用 Prompt Text。")]
    [SerializeField] private TextMeshProUGUI successText;

    [Header("右下角確認按鈕（HumanCheck）")]
    [SerializeField] private Button humanCheckButton;
    [Tooltip("按鈕上的文字（HumanCheck 底下的 Text (TMP)）")]
    [SerializeField] private TextMeshProUGUI humanCheckLabel;

    [Header("逼近")]
    [Tooltip("每一輪的預設時間（秒）。可在 Rounds 的個別題目裡覆寫。0 = 不逼近、不倒數")]
    [SerializeField] private float timeLimitSeconds = 15f;
    [SerializeField] private float startScale = 0.6f;
    [SerializeField] private float endScale = 2.2f;
    [Tooltip("橫軸 = 時間進度 0~1，縱軸 = 放大進度 0~1")]
    [SerializeField] private AnimationCurve approachCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("驚嚇演出。只要有一輪選 Jumpscare 就必須指定。")]
    [SerializeField] private JumpscareSequence jumpscare;

    [Header("回饋")]
    [SerializeField] private Color correctColor = new Color(0.4f, 1f, 0.4f);
    [SerializeField] private Color wrongColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private float wrongShakeSeconds = 0.35f;
    [SerializeField] private float shakeStrength = 12f;

    [Header("訊息（打字機）")]
    [SerializeField] private float charInterval = 0.08f;
    [Tooltip("訊息『打完字之後』再停留幾秒")]
    [SerializeField] private float holdSeconds = 1.5f;
    [Tooltip("訊息收起後，等幾秒才出現下一輪的題目（只在有設定 Success Text 時生效）")]
    [SerializeField] private float nextPromptDelay = 0.3f;

    [Header("亂碼文字（{{ }} 包起來的部分）")]
    [Tooltip("亂碼從這些字元裡隨機抽。請確認字型有這些字。")]
    [SerializeField] private string scrambleCharacters = "■□▲△●○◆◇★☆※";
    [Tooltip("每個亂碼字元固定佔的寬度（em）。0 = 不固定。")]
    [SerializeField] private float scrambleCharWidthEm = 1f;
    [SerializeField] private float scrambleInterval = 0.06f;
    [Tooltip("題目裡閃爍的字，每次顯示『原本的字』的機率（0~1）。")]
    [SerializeField, Range(0f, 1f)] private float promptRealCharChance = 0.5f;

    [Header("題目")]
    [SerializeField] private List<CaptchaRound> rounds = new List<CaptchaRound>();

    [Header("流程")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float startDelay = 0.5f;
    [Tooltip("全部通過後要切去的場景。留空就只觸發 On Completed。")]
    [SerializeField] private string nextSceneName;

    [Header("事件")]
    [Tooltip("時間到時觸發（不論該輪選哪種失敗處理）")]
    [SerializeField] private UnityEvent onTimeUp;
    [Tooltip("點錯九宮格時觸發。可接 RandomSpriteSwapper.SwapNow。")]
    [SerializeField] private UnityEvent onWrongClick;
    [SerializeField] private UnityEvent onCompleted;

    public bool TimedOut { get; private set; }
    public int WrongClickCount { get; private set; }

    private enum ClickKind { Cell, Prompt, Check, TimeUp }
    private struct ClickEvent { public ClickKind kind; public CaptchaCell cell; }
    private enum AttemptResult { Passed, FailedContinue, Retry }

    private readonly List<CaptchaCell> cells = new List<CaptchaCell>();
    private readonly Queue<ClickEvent> events = new Queue<ClickEvent>();
    private readonly StringBuilder sb = new StringBuilder();
    private UniTaskCompletionSource signal;
    private bool acceptingInput;

    private CaptchaRound currentRound;
    private Color promptBaseColor = Color.white;
    private Color checkBaseColor = Color.white;
    private string checkOriginalLabel;
    private string mspaceOpenTag = "";

    private bool timerRunning;
    private bool pendingJumpscareDismiss; // 驚嚇演出（Resume）結束後，黑畫面還蓋著，等下一輪準備好再淡出
    private float elapsed;
    private float currentTimeLimit;
    private int promptScrambleVersion;

    // ───────────── 初始化 ─────────────

    private void Awake()
    {
        if (promptText != null) promptBaseColor = promptText.color;
        if (successText != null) successText.gameObject.SetActive(false);

        if (scrambleCharWidthEm > 0f)
            mspaceOpenTag = "<mspace=" + scrambleCharWidthEm.ToString("0.###", CultureInfo.InvariantCulture) + "em>";

        for (int i = 0; i < cellCount; i++)
        {
            var cell = Instantiate(cellPrefab, gridRoot);
            cell.name = $"Cell {i + 1}";
            cell.Init();
            cell.Clicked += c => Post(new ClickEvent { kind = ClickKind.Cell, cell = c });
            cells.Add(cell);
        }

        if (humanCheckButton != null)
        {
            if (humanCheckButton.targetGraphic != null) checkBaseColor = humanCheckButton.targetGraphic.color;
            humanCheckButton.onClick.AddListener(() => Post(new ClickEvent { kind = ClickKind.Check }));
        }
        if (humanCheckLabel != null) checkOriginalLabel = humanCheckLabel.text;

        root.SetActive(false);
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

    // ───────────── 每幀 ─────────────

    private void Update()
    {
        CheckPromptClick();
        if (timerRunning) TickApproach();
    }

    /// <summary>點擊題目裡閃爍的字（用 TMP 的 &lt;link&gt; 偵測）。</summary>
    private void CheckPromptClick()
    {
        if (!acceptingInput || currentRound == null || !currentRound.requirePromptClick) return;
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (!promptText.gameObject.activeInHierarchy) return;

        if (TMP_TextUtilities.FindIntersectingLink(promptText, Mouse.current.position.ReadValue(), null) != -1)
            Post(new ClickEvent { kind = ClickKind.Prompt });
    }

    private void TickApproach()
    {
        elapsed += Time.deltaTime;
        if (currentTimeLimit <= 0f) return;

        float t = Mathf.Clamp01(elapsed / currentTimeLimit);
        SetApproachScale(Mathf.LerpUnclamped(startScale, endScale, approachCurve.Evaluate(t)));

        if (TimedOut || elapsed < currentTimeLimit) return;

        TimedOut = true;
        timerRunning = false;
        onTimeUp?.Invoke();
        Post(new ClickEvent { kind = ClickKind.TimeUp }); // 交給 RunAttempt 依該輪的 Time Up Behavior 處理
    }

    private void SetApproachScale(float s) => approachRoot.localScale = new Vector3(s, s, 1f);

    // ───────────── 流程 ─────────────

    /// <summary>開始小遊戲，通過全部題目後才會結束。</summary>
    public async UniTask Play()
    {
        var token = this.GetCancellationTokenOnDestroy();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        TimedOut = false;
        WrongClickCount = 0;
        pendingJumpscareDismiss = false;
        timerRunning = false;
        acceptingInput = false;
        events.Clear();
        SetApproachScale(startScale);

        root.SetActive(true);
        await UniTask.Yield(token); // 等剛啟用的物件完成 OnEnable

        foreach (var round in rounds)
            await RunRound(round, token);

        if (pendingJumpscareDismiss) // 最後一輪失敗後沒有下一輪可以接，直接淡出
        {
            pendingJumpscareDismiss = false;
            jumpscare.Dismiss().Forget();
        }

        timerRunning = false;
        currentRound = null;
        root.SetActive(false);

        onCompleted?.Invoke();
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneTransition.Load(nextSceneName).Forget();
    }

    private async UniTask RunRound(CaptchaRound round, CancellationToken token)
    {
        currentRound = round;

        while (true)
        {
            PrepareRoundUI(round);

            var result = await RunAttempt(round, token);
            acceptingInput = false;
            promptScrambleVersion++;

            if (result == AttemptResult.Retry) continue;

            if (result == AttemptResult.Passed)
            {
                timerRunning = false; // 成功訊息停留期間不再繼續逼近
                await ShowMessage(round.successMessage, token);
            }
            return;
        }
    }

    /// <summary>每次（重）開始一輪時：還原題目、按鈕文字與顏色。</summary>
    private void PrepareRoundUI(CaptchaRound round)
    {
        SetApproachScale(startScale);
        promptText.gameObject.SetActive(true);
        promptText.maxVisibleCharacters = int.MaxValue;
        promptText.color = promptBaseColor;
        if (successText != null) successText.gameObject.SetActive(false);

        promptScrambleVersion++;
        bool scrambles = !string.IsNullOrEmpty(round.prompt) && round.prompt.Contains("{{");
        promptText.text = scrambles ? Scramble(round.prompt, promptRealCharChance, round.requirePromptClick) : round.prompt;
        if (scrambles) ScramblePromptLoop(round, promptScrambleVersion).Forget();

        if (humanCheckLabel != null)
            humanCheckLabel.text = string.IsNullOrEmpty(round.humanCheckLabel) ? checkOriginalLabel : round.humanCheckLabel;
        SetCheckTint(checkBaseColor);
    }

    private void StartRoundTimer(CaptchaRound round)
    {
        elapsed = 0f;
        TimedOut = false;
        currentTimeLimit = round.timeLimitSeconds > 0f ? round.timeLimitSeconds : timeLimitSeconds;
        SetApproachScale(startScale);
        timerRunning = true;
    }

    /// <summary>一次嘗試：九宮格答對 + 該輪要求的額外點擊都完成才算通過。</summary>
    private async UniTask<AttemptResult> RunAttempt(CaptchaRound round, CancellationToken token)
    {
        bool gridDone = false;
        bool promptDone = !round.requirePromptClick;
        bool checkDone = !round.requireHumanCheckClick;
        int wrongCount = 0;

        if (!BuildGrid(round)) return AttemptResult.FailedContinue;

        // 上一輪的驚嚇演出還蓋著黑畫面：這一輪的畫面都準備好後才淡出，淡出完才開始計時
        if (pendingJumpscareDismiss)
        {
            pendingJumpscareDismiss = false;
            await jumpscare.Dismiss();
        }

        StartRoundTimer(round);
        OpenInput();

        while (true)
        {
            var ev = await NextEvent(token);

            switch (ev.kind)
            {
                case ClickKind.Cell:
                    if (gridDone) break;

                    if (ev.cell.IsCorrect)
                    {
                        gridDone = true;
                        ev.cell.SetTint(correctColor);
                        SetCellsInteractable(false);
                        break;
                    }

                    // 點錯
                    WrongClickCount++;
                    wrongCount++;
                    ev.cell.SetTint(wrongColor);
                    onWrongClick?.Invoke();

                    // 選了 Jumpscare 卻沒指定演出時，永遠不會失敗，只有懲罰與重新洗牌
                    bool noJumpscare = round.wrongClickBehavior == FailBehavior.Jumpscare && jumpscare == null;
                    if (!noJumpscare && wrongCount >= Mathf.Max(1, round.wrongClicksBeforeFail))
                    {
                        // 顯示文字前先給點錯的回饋（紅色 + 抖動）；驚嚇演出則立刻開始
                        if (round.wrongClickBehavior == FailBehavior.ShowMessage)
                        {
                            acceptingInput = false;
                            await ShakeGrid(token);
                        }
                        return await HandleFail(round, round.wrongClickBehavior, token);
                    }

                    // 還沒到失敗次數：懲罰（計時快轉）+ 抖動 + 重新洗牌
                    ApplyWrongPenalty(round);
                    acceptingInput = false;
                    SetCellsInteractable(false);
                    await ShakeGrid(token);

                    if (!BuildGrid(round)) return AttemptResult.FailedContinue;
                    gridDone = false;
                    OpenInput(keepTimeUp: true);
                    continue;

                case ClickKind.Prompt:
                    if (promptDone) break;
                    promptDone = true;
                    promptScrambleVersion++; // 停止閃爍，顯示原本的字
                    promptText.text = round.prompt.Replace("{{", "").Replace("}}", "");
                    promptText.color = correctColor;
                    break;

                case ClickKind.Check:
                    if (checkDone) break;
                    checkDone = true;
                    SetCheckTint(correctColor);
                    break;

                case ClickKind.TimeUp:
                    return await HandleFail(round, round.timeUpBehavior, token);
            }

            if (gridDone && promptDone && checkDone)
                return AttemptResult.Passed;
        }
    }

    private async UniTask<AttemptResult> HandleFail(CaptchaRound round, FailBehavior behavior, CancellationToken token)
    {
        acceptingInput = false;
        SetCellsInteractable(false);

        if (behavior == FailBehavior.Jumpscare)
        {
            if (jumpscare == null)
            {
                Debug.LogWarning("[CaptchaMinigame] 選了 Jumpscare，但 Jumpscare 欄位是空的。", this);
                return AttemptResult.FailedContinue;
            }

            timerRunning = false;
            await jumpscare.Play();

            // After Action = Resume：演出結束後接下一輪；其他（重載、換場景）則停在這裡等它處理
            if (!jumpscare.ResumesGame) await WaitForever(token);

            pendingJumpscareDismiss = true;
            return AttemptResult.FailedContinue;
        }

        timerRunning = false;
        await ShowMessage(round.failMessage, token);
        return round.afterFail == AfterFail.Retry ? AttemptResult.Retry : AttemptResult.FailedContinue;
    }

    /// <summary>點錯的懲罰：計時瞬間快轉，九宮格立刻逼近。</summary>
    private void ApplyWrongPenalty(CaptchaRound round)
    {
        if (round.wrongClickPenaltySeconds > 0f && timerRunning && currentTimeLimit > 0f)
            elapsed += round.wrongClickPenaltySeconds;
    }

    private static UniTask WaitForever(CancellationToken token) =>
        UniTask.WaitUntil(() => false, cancellationToken: token);

    // ───────────── 點擊事件佇列 ─────────────

    /// <summary>開放點擊並清掉排隊中的事件（keepTimeUp：保留『時間到』，避免洗牌期間剛好時間到被吃掉）。</summary>
    private void OpenInput(bool keepTimeUp = false)
    {
        bool hadTimeUp = false;
        if (keepTimeUp)
            foreach (var e in events)
                if (e.kind == ClickKind.TimeUp) { hadTimeUp = true; break; }

        events.Clear();
        if (hadTimeUp) events.Enqueue(new ClickEvent { kind = ClickKind.TimeUp });

        acceptingInput = true;
        SetCellsInteractable(true);
    }

    private void Post(ClickEvent e)
    {
        if (e.kind != ClickKind.TimeUp && !acceptingInput) return;
        events.Enqueue(e);
        signal?.TrySetResult();
    }

    private async UniTask<ClickEvent> NextEvent(CancellationToken token)
    {
        while (events.Count == 0)
        {
            signal = new UniTaskCompletionSource();
            await signal.Task.AttachExternalCancellation(token);
            signal = null;
        }
        return events.Dequeue();
    }

    // ───────────── 文字 ─────────────

    private async UniTaskVoid ScramblePromptLoop(CaptchaRound round, int version)
    {
        var token = this.GetCancellationTokenOnDestroy();
        var delay = TimeSpan.FromSeconds(Mathf.Max(0.01f, scrambleInterval));
        try
        {
            while (true)
            {
                await UniTask.Delay(delay, cancellationToken: token);
                if (version != promptScrambleVersion) return;
                promptText.text = Scramble(round.prompt, promptRealCharChance, round.requirePromptClick);
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>顯示訊息（答對或失敗）：打字機，打完停留 holdSeconds 秒，再收起。</summary>
    private async UniTask ShowMessage(string message, CancellationToken token)
    {
        if (string.IsNullOrEmpty(message))
        {
            await UniTask.Delay(TimeSpan.FromSeconds(holdSeconds), cancellationToken: token);
            return;
        }

        var target = promptText;
        if (successText != null)
        {
            target = successText;
            promptText.gameObject.SetActive(false);
            successText.gameObject.SetActive(true);
        }

        bool hasScramble = message.Contains("{{");
        target.maxVisibleCharacters = 0;
        target.text = Scramble(message);
        target.ForceMeshUpdate();
        int totalChars = target.textInfo.characterCount;

        int visible = 0;
        float typeTimer = 0f, scrambleTimer = 0f, holdTimer = 0f;
        float typeStep = Mathf.Max(0.01f, charInterval);
        float scrambleStep = Mathf.Max(0.01f, scrambleInterval);

        // 這裡自己用 maxVisibleCharacters 做打字機，因為亂碼要不停重設文字，
        // 若用 Text Animator 的打字機會被一直打斷、從頭重打。
        while (holdTimer < holdSeconds)
        {
            float dt = Time.deltaTime;

            if (visible < totalChars)
            {
                typeTimer += dt;
                while (visible < totalChars && typeTimer >= typeStep) { visible++; typeTimer -= typeStep; }
            }
            else
            {
                holdTimer += dt; // 全部打完後才開始算停留時間
            }

            if (hasScramble)
            {
                scrambleTimer += dt;
                if (scrambleTimer >= scrambleStep)
                {
                    scrambleTimer = 0f;
                    target.text = Scramble(message); // 字元數不變，打字進度不受影響
                }
            }

            target.maxVisibleCharacters = visible;
            await UniTask.Yield(token);
        }

        target.maxVisibleCharacters = int.MaxValue;

        if (successText != null)
        {
            successText.gameObject.SetActive(false);
            if (nextPromptDelay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(nextPromptDelay), cancellationToken: token);
        }
    }

    /// <summary>
    /// 把 {{ }} 包起來的部分換成亂碼。
    /// realCharChance &gt; 0：每個字有機率顯示原本的字（題目用）。
    /// wrapLink：把亂碼段包進 &lt;link&gt;，讓 CheckPromptClick 能偵測點擊。
    /// </summary>
    private string Scramble(string message, float realCharChance = 0f, bool wrapLink = false)
    {
        string pool = string.IsNullOrEmpty(scrambleCharacters) ? "■" : scrambleCharacters;
        sb.Clear();
        int i = 0;

        while (i < message.Length)
        {
            int start = message.IndexOf("{{", i, StringComparison.Ordinal);
            int end = start < 0 ? -1 : message.IndexOf("}}", start + 2, StringComparison.Ordinal);
            if (end < 0) { sb.Append(message, i, message.Length - i); break; }

            sb.Append(message, i, start - i);
            if (wrapLink) sb.Append("<link=\"human\">");
            sb.Append(mspaceOpenTag); // 固定字寬，亂碼怎麼換整句長度都不變

            for (int k = start + 2; k < end; k++)
            {
                bool real = realCharChance > 0f && UnityEngine.Random.value < realCharChance;
                sb.Append(real ? message[k] : pool[UnityEngine.Random.Range(0, pool.Length)]);
            }

            if (mspaceOpenTag.Length > 0) sb.Append("</mspace>");
            if (wrapLink) sb.Append("</link>");
            i = end + 2;
        }
        return sb.ToString();
    }

    // ───────────── 九宮格與回饋 ─────────────

    private bool BuildGrid(CaptchaRound round)
    {
        if (round.correctOptions == null || round.correctOptions.Count == 0 ||
            round.wrongOptions == null || round.wrongOptions.Count == 0)
        {
            Debug.LogError("[CaptchaMinigame] 這一輪的正確選項或錯誤選項是空的，請在 Inspector 補上。", this);
            return false;
        }

        var entries = new List<(CaptchaOption option, bool correct)>(cells.Count)
        {
            (round.correctOptions[UnityEngine.Random.Range(0, round.correctOptions.Count)], true)
        };

        var wrongPool = new List<CaptchaOption>(round.wrongOptions);
        Shuffle(wrongPool);
        for (int i = 0; i < cells.Count - 1; i++)
            entries.Add((wrongPool[i % wrongPool.Count], false)); // 不夠就循環使用

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

    private void SetCheckTint(Color color)
    {
        if (humanCheckButton != null && humanCheckButton.targetGraphic != null)
            humanCheckButton.targetGraphic.color = color;
    }

    private async UniTask ShakeGrid(CancellationToken token)
    {
        Vector2 origin = gridRoot.anchoredPosition;
        for (float t = 0f; t < wrongShakeSeconds; t += Time.deltaTime)
        {
            gridRoot.anchoredPosition = origin + UnityEngine.Random.insideUnitCircle * shakeStrength;
            await UniTask.Yield(token);
        }
        gridRoot.anchoredPosition = origin;
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