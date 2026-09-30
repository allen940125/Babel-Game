using Cysharp.Threading.Tasks;
using UnityEngine;

public class TransitionCutsceneTrigger : MonoBehaviour
{
    [Header("要在劇情期間停用的功能")]
    [SerializeField] private MonoBehaviour[] scriptsToDisableDuringCutscene;

    [Header("流程設定")]
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
        await UniTask.WaitUntil(() => Dialoguecontroller.Instance != null);
        await UniTask.WaitUntil(() => GameManager.Instance != null && GameManager.Instance.IsInitialized);

        // 一進場景就先鎖住移動/撞NPC切場景，避免玩家在演出期間亂跑
        SetGameplayEnabled(false);

        // 1. 先讓玩家看一下場景
        await UniTask.Delay(System.TimeSpan.FromSeconds(sceneViewDelay));

        // 2. 切黑底，跑旁白
        if (blackBG != null) blackBG.SetActive(true);
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