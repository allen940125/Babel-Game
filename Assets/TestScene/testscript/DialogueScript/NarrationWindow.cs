using Cysharp.Threading.Tasks;
using Febucci.TextAnimatorForUnity;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class NarrationWindow : BasePanel
{
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TypewriterComponent typewriter;

    public void Init()
    {
        typewriter.onTextShowed.AddListener(OnTypewriterFinished);
        ContinueStory();
    }

    private void Update()
    {
        if (!Dialoguecontroller.Instance.DialogueIsPlaying) return;
        if (Keyboard.current == null && Mouse.current == null) return;

        bool advance = (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                    || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        if (!advance) return;

        if (typewriter.IsShowingText)
            typewriter.SkipTypewriter();
        else
            ContinueStory();
    }

    private void ContinueStory()
    {
        var story = Dialoguecontroller.Instance.CurrentStory;
        if (story == null) return;

        if (story.canContinue)
        {
            string line = "";
            while (story.canContinue)
            {
                line = story.Continue();
                if (!string.IsNullOrWhiteSpace(line)) break;
            }
            typewriter.ShowText(line);
        }
        else
        {
            Dialoguecontroller.Instance.EndDialogue();
        }
    }

    private void OnTypewriterFinished()
    {
        var story = Dialoguecontroller.Instance.CurrentStory;
        if (story != null && !story.canContinue)
            Dialoguecontroller.Instance.EndDialogue();
    }
}