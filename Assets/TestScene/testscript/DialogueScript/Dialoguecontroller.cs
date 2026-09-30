using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Datamanager;
using Game.UI;
using Ink.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class Dialoguecontroller : Singleton<Dialoguecontroller>
{
    public string CurrentNpcId { get; private set; }
    public Story CurrentStory { get; private set; }
    public bool DialogueIsPlaying { get; private set; }
    public event System.Action OnDialogueEnded;
    private UIType currentUIType;

    public async UniTask StartDialogue(int inkId, UIType uiType)
    {
        if (DialogueIsPlaying) return;

        await UniTask.WaitUntil(() => GameContainer.Get<DataManager>().Database != null);

        var entry = GameContainer.Get<DataManager>().GetDataByID<DialogueDataBaseTemplete>(inkId);
        if (entry == null)
        {
            Debug.LogError($"DialogueDataBase 裡找不到 id: {inkId}");
            return;
        }

        var handle = Addressables.LoadAssetAsync<TextAsset>(entry.PrefabPath);
        TextAsset inkJson = await handle.Task;
        if (inkJson == null)
        {
            Debug.LogError($"找不到 Address 為 {entry.PrefabPath} 的 Ink 檔案");
            return;
        }

        CurrentNpcId = inkId.ToString();
        CurrentStory = new Story(inkJson.text);
        Addressables.Release(handle);

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
    }
}