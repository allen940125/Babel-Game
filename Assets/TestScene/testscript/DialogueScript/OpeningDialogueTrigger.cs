using Cysharp.Threading.Tasks;
using UnityEngine;

public class OpeningDialogueTrigger : MonoBehaviour
{
    [SerializeField] private int InkId = 1002;
    [SerializeField] private GameObject blackBG;              // 拖上你的黑底 BG 物件
    [SerializeField] private string nextSceneName;             // 先留空也沒關係

    [SerializeField] private OpeningIntroAudio introAudio;

    private void Start()
    {
        TriggerOpening().Forget();
    }

    private async UniTaskVoid TriggerOpening()
    {
        await UniTask.WaitUntil(() => Dialoguecontroller.Instance != null);
        await UniTask.WaitUntil(() => GameManager.Instance != null && GameManager.Instance.IsInitialized);

        await introAudio.PlayIntroSequenceAsync();   // 等音效序列播完

        Dialoguecontroller.Instance.OnDialogueEnded += OnDialogueFinished;
        Dialoguecontroller.Instance.StartDialogue(InkId, UIType.DialogueWindowNew).Forget();

        // 1. 等對話框「真的開啟」（DialogueIsPlaying 在面板載入完成前就可能是 true，不能只看它）
        var ui = GameManager.Instance.UIManager;
        float waited = 0f;
        while (waited < 10f && !(ui != null && ui.IsPanelOpen(UIType.DialogueWindowNew)))
        {
            waited += Time.unscaledDeltaTime;
            await UniTask.Yield();
        }

        // 2. 等對話框自己的黑底完全蓋上。這時 DialogueStageEffects 會放開 ScreenBlackout，
        //    所以「ScreenBlackout 不再 Hold」就是黑底已就緒的訊號（最多等 5 秒，避免卡住）。
        waited = 0f;
        while (ScreenBlackout.IsHolding && waited < 5f)
        {
            waited += Time.unscaledDeltaTime;
            await UniTask.Yield();
        }

        // 3. 再多等兩幀，確保畫面已經穩定
        await UniTask.Yield();
        await UniTask.Yield();

        if (blackBG != null)
            blackBG.SetActive(false);
    }

    private void OnDialogueFinished()
    {
        Dialoguecontroller.Instance.OnDialogueEnded -= OnDialogueFinished;

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneTransition.Load(nextSceneName).Forget();
    }
}