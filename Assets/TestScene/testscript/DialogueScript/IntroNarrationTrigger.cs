using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroNarrationTrigger : MonoBehaviour
{
    [SerializeField] private string InkId;
    [SerializeField] private TextAsset inkJson;
    [SerializeField] private string explorationSceneName; // Scene名字

    private void Start()
    {
        TriggerIntro().Forget();
    }

    private async UniTaskVoid TriggerIntro()
    {
        await UniTask.WaitUntil(() => Dialoguecontroller.Instance != null);

        Dialoguecontroller.Instance.OnDialogueEnded += OnIntroFinished;
        Dialoguecontroller.Instance.StartDialogue(InkId, inkJson, UIType.NarrationWindow).Forget();
    }

    private void OnIntroFinished()
    {
        Dialoguecontroller.Instance.OnDialogueEnded -= OnIntroFinished;
        SceneManager.LoadScene(explorationSceneName);
    }
}