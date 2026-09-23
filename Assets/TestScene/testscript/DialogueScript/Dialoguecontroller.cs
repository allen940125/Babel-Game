using Cysharp.Threading.Tasks;
using Datamanager;
using Game.UI;
using Ink.Runtime;
using System.Collections.Generic;
using UnityEngine;

public class Dialoguecontroller : Singleton<Dialoguecontroller>
{
    public string CurrentNpcId { get; private set; }
    public Story CurrentStory { get; private set; }
    public bool DialogueIsPlaying { get; private set; }

    public event System.Action OnDialogueEnded;

    private UIType currentUIType;

    public async UniTask StartDialogue(string npcId, TextAsset inkJson, UIType uiType)
    {
        if (DialogueIsPlaying) return;//防連點

        await UniTask.WaitUntil(() => GameContainer.Get<DataManager>().Database != null);

        CurrentNpcId = npcId;
        CurrentStory = new Story(inkJson.text);
        CurrentStory.ChoosePathString(npcId); // 紀錄現在對話NPC

        DialogueIsPlaying = true;

        currentUIType = uiType;

        if (uiType == UIType.DialogueWindowNew)
        {
            var dialogueWindow = await GameManager.Instance.UIManager.OpenPanel<Dialogueshow>(uiType);
            dialogueWindow.Init();
        }
        else if (uiType == UIType.NarrationWindow)
        {
            var narrationWindow = await GameManager.Instance.UIManager.OpenPanel<NarrationWindow>(uiType);
            narrationWindow.Init();
        }

    }

    public void EndDialogue()
    {
        DialogueIsPlaying = false;
        CurrentStory = null;
        CurrentNpcId = null;

        GameManager.Instance.UIManager.ClosePanel(currentUIType);

        OnDialogueEnded?.Invoke();

        Debug.Log("EndDialohue被呼叫，currentUIType=" + currentUIType);
    }


}

