using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroNarrationTrigger : MonoBehaviour
{
    [SerializeField] private int InkId;
    [SerializeField] private string explorationSceneName; // Scene名字

    private void Start()
    {
        TriggerIntro().Forget();
    }

    private async UniTaskVoid TriggerIntro()
    {
        await UniTask.WaitUntil(() => Dialoguecontroller.Instance != null);
        await UniTask.WaitUntil(() => GameManager.Instance != null && GameManager.Instance.IsInitialized);


        Dialoguecontroller.Instance.OnDialogueEnded += OnIntroFinished;
        Dialoguecontroller.Instance.StartDialogue(InkId, UIType.NarrationWindow).Forget();
    }

    private void OnIntroFinished()
    {
        Dialoguecontroller.Instance.OnDialogueEnded -= OnIntroFinished;
        SceneManager.LoadScene(explorationSceneName);
    }
}