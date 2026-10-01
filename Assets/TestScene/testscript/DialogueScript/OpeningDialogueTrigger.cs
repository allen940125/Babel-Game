using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OpeningDialogueTrigger : MonoBehaviour
{
    [SerializeField] private int InkId = 1002;
    [SerializeField] private GameObject blackBG;              // 拖上你的黑底 BG 物件
    [SerializeField] private float waitBeforeDialogue = 2f;    // 音效還沒做好前，先用固定秒數頂著轉場
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

        await introAudio.PlayIntroSequenceAsync();   // 這裡改成等音效序列播完

        if (blackBG != null)
            blackBG.SetActive(false);

        Dialoguecontroller.Instance.OnDialogueEnded += OnDialogueFinished;
        Dialoguecontroller.Instance.StartDialogue(InkId, UIType.DialogueWindowNew).Forget();
    }

    private void OnDialogueFinished()
    {
        Dialoguecontroller.Instance.OnDialogueEnded -= OnDialogueFinished;

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }
}