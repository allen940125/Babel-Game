using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.UI;
using Ink.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : Singleton<DialogueManager>
{
    public string CurrentNpcId;
    public Story CurrentStory { get; private set; }
    public bool DialogueIsPlaying;

    [SerializeField] private TextAsset inkTestAsset0001;
    [SerializeField] private TextAsset inkTestAsset0002;
    private Dictionary<string, string> storyStates = new();
    
    private void Update()
    {
        #region 測試用

        if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame && !DialogueManager.Instance.DialogueIsPlaying)
        {
            Debug.Log("鍵盤上的 J 鍵被按下了 (Was Pressed This Frame)！");
            DialogueManager.Instance.StartDialogue("Test1002", inkTestAsset0002).Forget();
        }
        if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame && !DialogueManager.Instance.DialogueIsPlaying)
        {
            Debug.Log("鍵盤上的 G 鍵被按下了 (Was Pressed This Frame)！");
            DialogueManager.Instance.StartDialogue("Test1001", inkTestAsset0001).Forget();
        }

        #endregion
    }
    
    public async UniTask StartDialogue(string npcId, TextAsset inkJson)
    {
        Debug.Log("Starting Dialogue" + npcId);
        CurrentNpcId = npcId;
        CurrentStory = new Story(inkJson.text);

        if (storyStates.TryGetValue(npcId, out var savedJson))
        {
            CurrentStory.state.LoadJson(savedJson);
        }


        DialogueIsPlaying = true;
        var dialogueWindow = await GameManager.Instance.UIManager.OpenPanel<DialogueWindow>(UIType.DialogueWindow);
        dialogueWindow.Init();
    }
    
    public void EndDialogue()
    {
        if (!string.IsNullOrEmpty(CurrentNpcId) && CurrentStory != null)
        {
            // 儲存 Ink 對話的變數狀態
            storyStates[CurrentNpcId] = CurrentStory.state.ToJson();
        }

        DialogueIsPlaying = false;
        CurrentStory = null;
        GameManager.Instance.UIManager.ClosePanel(UIType.DialogueWindow);
    }
}