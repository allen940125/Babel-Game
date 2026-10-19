using Cysharp.Threading.Tasks;
using UnityEngine;

public class TransitionCutsceneTrigger : MonoBehaviour
{
    [Header("要在劇情期間停用的功能")]
    [SerializeField] private MonoBehaviour[] scriptsToDisableDuringCutscene;

    [Header("流程設定")]
    [Tooltip("進場景後先用全螢幕圖片讓玩家看。留空 = 不顯示圖片，改成等 Scene View Delay 秒。")]
    [SerializeField] private Sprite introImage;
    [Tooltip("圖片填滿方式：Cover = 填滿並裁邊；Fit = 完整顯示（有黑邊）；Stretch = 拉伸。")]
    [SerializeField] private FullscreenImage.FitMode introImageFit = FullscreenImage.FitMode.Cover;
    [Tooltip("圖片停留秒數；0 = 只能玩家點擊（F / 左鍵 / 空白鍵）才繼續。點擊在前 0.5 秒會被忽略。")]
    [SerializeField] private float introImageSeconds = 5f;
    [SerializeField] private float introImageFadeIn = 0.8f;
    [Tooltip("沒有設定圖片時，沿用舊的行為：停這麼多秒讓玩家看場景。")]
    [SerializeField] private float sceneViewDelay = 4f;
    [SerializeField] private GameObject blackBG;

    [Header("對話內容")]
    [SerializeField] private int narrationInkId;
    [SerializeField] private int guideDialogueInkId;

    private void Start()
    {
        RunCutscene().Forget();
    }

    private async UniTaskVoid RunCutscene()
    {
        // 一進場景就先鎖住移動/撞NPC切場景，避免玩家在演出期間亂跑
        SetGameplayEnabled(false);

        // 1. 圖片要「第一時間」顯示：放在所有等待之前（Start 在第一個畫面渲染前執行），
        //    這樣玩家一進場景看到的就是這張圖，不會先看到後面的 3D 場景。
        //    想要「直接出現、沒有淡入」就把 Intro Image Fade In 設成 0。
        if (introImage != null)
            await FullscreenImage.Show(introImage, introImageFit, introImageFadeIn);

        await UniTask.WaitUntil(() => Dialoguecontroller.Instance != null);
        await UniTask.WaitUntil(() => GameManager.Instance != null && GameManager.Instance.IsInitialized);

        if (introImage != null)
        {
            await FullscreenImage.WaitForDismiss(introImageSeconds);

            // 先蓋上黑底，再讓圖片消失（立刻消失），避免圖片消失時露出後面的場景
            if (blackBG != null) blackBG.SetActive(true);
            await FullscreenImage.Hide(0f);
        }
        else
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(sceneViewDelay));
            if (blackBG != null) blackBG.SetActive(true);
        }

        // 2. 跑旁白（黑底已經開著）
        await PlayDialogueAndWait(narrationInkId, UIType.NarrationWindow);

        // 3. 關掉黑底，看到玩家跟引路人
        if (blackBG != null) blackBG.SetActive(false);

        // 4. 跑玩家與引路人的對話
        await PlayDialogueAndWait(guideDialogueInkId, UIType.DialogueWindowNew);

        // 劇情全部跑完，恢復玩家操作
        SetGameplayEnabled(true);
    }

    private async UniTask PlayDialogueAndWait(int inkId, UIType uiType)
    {
        Dialoguecontroller.Instance.StartDialogue(inkId, uiType).Forget();

        // 先等對話真的開始，避免還沒切換狀態就被誤判成「已結束」
        await UniTask.WaitUntil(() => Dialoguecontroller.Instance.DialogueIsPlaying);
        // 等對話真的結束
        await UniTask.WaitUntil(() => !Dialoguecontroller.Instance.DialogueIsPlaying);
    }

    private void SetGameplayEnabled(bool enabled)
    {
        foreach (var script in scriptsToDisableDuringCutscene)
        {
            if (script != null)
                script.enabled = enabled;
        }
    }
}