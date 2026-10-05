using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Febucci.TextAnimatorForUnity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum RunStepType
{
    Run,          // 純等待一段時間（玩家可以自由走動，不算進度）
    Text,         // 飄字（無對話框），玩家可以繼續走動
    QTE,          // 限時按鍵（期間主角停下）
    LockControl,  // 主角停下、無法再被操控
    Interrupt,    // 畫面中斷演出
    Walk          // 等玩家按住 W 走夠指定距離或時間，才進入下一步
}

[Serializable]
public class RunSequenceStep
{
    public RunStepType type;

    [Tooltip("Run：等待幾秒；Text：打完字後停留幾秒；其他類型不使用")]
    public float seconds = 1.5f;

    [TextArea] public string text;

    public Key qteKey = Key.E;
    public float qteTimeLimit = 1.5f;

    [Tooltip("Walk：往前走滿這個距離就進入下一步（0 = 不用距離判斷）")]
    public float walkDistance = 10f;
    [Tooltip("Walk：累計按住 W 這麼多秒就進入下一步（0 = 不用時間判斷）。距離與時間都填時，先到的先算。")]
    public float walkSeconds = 0f;
}

/// <summary>
/// 黑羊戰（一周目尾）流程：玩家按住 W 前進 → 走夠距離或時間後跑飄字 / QTE → 畫面中斷 → 切場景。
/// 整個流程由 Inspector 裡的 Steps 清單決定，不用改程式就能調順序與內容。
/// 這支腳本不依賴 Dialoguecontroller / UIManager，放進任何場景就能跑。
/// </summary>
public class BlackSheepRunSequence : MonoBehaviour
{
    [Header("角色與移動")]
    [SerializeField] private RunnerMover runner;
    [Tooltip("流程開始時要停用的其他控制腳本（例如同事的 PlayerAdventureController），避免跟這裡的移動打架")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;

    [Header("操作提示（可選）")]
    [Tooltip("Walk 步驟期間，玩家沒有按 W 時顯示的提示物件（例如寫著「W」的圖示）。留空就不顯示。")]
    [SerializeField] private GameObject walkHintRoot;

    [Header("飄字（無對話框）")]
    [SerializeField] private GameObject floatingTextRoot;
    [SerializeField] private TypewriterComponent typewriter;

    [Header("QTE")]
    [SerializeField] private QTEController qte;

    [Header("畫面中斷演出")]
    [SerializeField] private Image interruptImage;
    [SerializeField] private int flashCount = 3;
    [SerializeField] private float flashInterval = 0.06f;
    [SerializeField] private float blackHoldSeconds = 1f;

    [Header("流程")]
    [SerializeField] private float startDelay = 1f;
    [SerializeField] private List<RunSequenceStep> steps = new List<RunSequenceStep>();
    [SerializeField] private string nextSceneName;

    private void Start()
    {
        Play().Forget();
    }

    private async UniTaskVoid Play()
    {
        var token = this.GetCancellationTokenOnDestroy();

        foreach (var script in scriptsToDisable)
            if (script != null) script.enabled = false;

        floatingTextRoot.SetActive(false);
        if (interruptImage != null) interruptImage.gameObject.SetActive(false);
        if (walkHintRoot != null) walkHintRoot.SetActive(false);

        await UniTask.Delay(TimeSpan.FromSeconds(startDelay), cancellationToken: token);

        runner.StartRun(); // 開始接受玩家操控（不會自動前進，要按住 W）

        foreach (var step in steps)
            await ExecuteStep(step, token);

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    private async UniTask ExecuteStep(RunSequenceStep step, CancellationToken token)
    {
        switch (step.type)
        {
            case RunStepType.Run:
                await UniTask.Delay(TimeSpan.FromSeconds(step.seconds), cancellationToken: token);
                break;

            case RunStepType.Walk:
                await WaitForWalk(step, token);
                break;

            case RunStepType.Text:
                await ShowFloatingText(step.text, step.seconds, token);
                break;

            case RunStepType.QTE:
                runner.StopRun(); // QTE 期間主角停下（WASD 也一併失效）
                await qte.RunQTE(step.qteKey, step.qteTimeLimit);
                runner.ResumeRun(); // QTE 結束後恢復操控
                break;

            case RunStepType.LockControl:
                runner.StopRun();
                break;

            case RunStepType.Interrupt:
                await PlayInterrupt(token);
                break;
        }
    }

    /// <summary>等玩家按住 W 走夠距離或時間。沒在走的時候顯示提示。</summary>
    private async UniTask WaitForWalk(RunSequenceStep step, CancellationToken token)
    {
        bool useDistance = step.walkDistance > 0f;
        bool useTime = step.walkSeconds > 0f;

        if (!useDistance && !useTime)
        {
            Debug.LogWarning("[BlackSheepRunSequence] Walk 步驟的距離與時間都是 0，已略過。");
            return;
        }

        runner.ResetWalkCounters();

        while (true)
        {
            bool distanceDone = useDistance && runner.ForwardDistance >= step.walkDistance;
            bool timeDone = useTime && runner.WalkedSeconds >= step.walkSeconds;
            if (distanceDone || timeDone) break;

            if (walkHintRoot != null)
                walkHintRoot.SetActive(!runner.IsWalkingForward);

            await UniTask.Yield(token);
        }

        if (walkHintRoot != null) walkHintRoot.SetActive(false);
    }

    private async UniTask ShowFloatingText(string text, float holdSeconds, CancellationToken token)
    {
        if (string.IsNullOrEmpty(text)) return;

        floatingTextRoot.SetActive(true);
        await UniTask.Yield(token); // 等剛啟用的 Typewriter 完成 OnEnable

        var done = new UniTaskCompletionSource();
        UnityAction onShowed = () => done.TrySetResult();

        typewriter.onTextShowed.AddListener(onShowed);
        typewriter.ShowText(text);
        await done.Task.AttachExternalCancellation(token);
        typewriter.onTextShowed.RemoveListener(onShowed);

        await UniTask.Delay(TimeSpan.FromSeconds(holdSeconds), cancellationToken: token);
        floatingTextRoot.SetActive(false);
    }

    private async UniTask PlayInterrupt(CancellationToken token)
    {
        runner.StopRun();
        if (interruptImage == null) return;

        interruptImage.gameObject.SetActive(true);

        for (int i = 0; i < flashCount; i++)
        {
            interruptImage.color = Color.white;
            await UniTask.Delay(TimeSpan.FromSeconds(flashInterval), cancellationToken: token);
            interruptImage.color = Color.black;
            await UniTask.Delay(TimeSpan.FromSeconds(flashInterval), cancellationToken: token);
        }

        interruptImage.color = Color.black;
        await UniTask.Delay(TimeSpan.FromSeconds(blackHoldSeconds), cancellationToken: token);
    }
}