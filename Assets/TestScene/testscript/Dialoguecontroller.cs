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

    public async UniTask StartDialogue(string npcId, TextAsset inkJson)
    {
        if (DialogueIsPlaying) return;//防連點

        await UniTask.WaitUntil(() => GameContainer.Get<DataManager>().Database != null);

        CurrentNpcId = npcId;
        CurrentStory = new Story(inkJson.text);
        CurrentStory.ChoosePathString(npcId); // 紀錄現在對話NPC

        DialogueIsPlaying = true;

        var dialogueWindow = await GameManager.Instance.UIManager.OpenPanel<Dialogueshow>(UIType.DialogueWindowNew);
        dialogueWindow.Init();
    }

    public void EndDialogue()
    {
        DialogueIsPlaying = false;
        CurrentStory = null;
        CurrentNpcId = null;

        GameManager.Instance.UIManager.ClosePanel(UIType.DialogueWindowNew);
    }


}

