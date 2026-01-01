using System.Collections.Generic;
using System.Linq;
using Game.UI;
using Ink.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueWindow : BasePanel
{
    [Header("Dialogue UI")]
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI dialogueBodyText;
    
    [Header("Choices UI")]
    [SerializeField] private GameObject[] choices;
    [SerializeField] private TextMeshProUGUI[] choicesText;
    
    [Header("Character UI")]
    [SerializeField] private Image leftCharacterImage;
    [SerializeField] private Image rightCharacterImage;
    
    [SerializeField] private List<CharacterInfo> characterInfoList;
    private Dictionary<string, Sprite> characterPortraitMap;
    private string currentSpeaker = "";

    
    private void Update()
    {
        if (DialogueManager.Instance.DialogueIsPlaying && Input.GetKeyDown(KeyCode.F))
        {
            ContinueStory();
        }
    }

    public void Init()
    {
        characterPortraitMap = new Dictionary<string, Sprite>();
        foreach (var info in characterInfoList)
        {
            characterPortraitMap[info.characterName] = info.portrait;
        }
        
        // ⭕ 先初始化 Choices Text
        choicesText = new TextMeshProUGUI[choices.Length];
        int index = 0;
        
        foreach (GameObject choice in choices)
        {
            var tmp = choice.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp == null)
            {
                Debug.LogError($"找不到 TMP 組件在 {choice.name}");
            }
            choicesText[index] = tmp;
            index++;
        }


        // ⭕ 再繼續劇情
        ContinueStory();
    }


    public void ContinueStory()
    {
        var story = DialogueManager.Instance.CurrentStory;

        if (story == null)
        {
            DialogueManager.Instance.EndDialogue();
            return;
        }
        
        HideChoices(); // ⬅️ 每次繼續故事前先隱藏舊選項
        
        if (story.canContinue)
        {
            string nextLine = story.Continue();
            dialogueBodyText.text = nextLine;
            DisplayChoices(); // 有可能還會產生新的選項
        }
        else if (story.currentChoices.Count > 0)
        {
            DisplayChoices();
        }
        else
        {
            DialogueManager.Instance.EndDialogue();
        }
        
        HandleTags(story.currentTags);
    }
    
    private void DisplayChoices()
    {
        var story = DialogueManager.Instance.CurrentStory;
        
        List<Choice> currnetChoices = story.currentChoices;

        if (currnetChoices.Count > choices.Length)
        {
            
        }
        
        int index = 0;

        foreach (Choice choice in currnetChoices)
        {
            choices[index].gameObject.SetActive(true);
            choicesText[index].text = choice.text;
            index++;
        }

        for (int i = 0; i < currnetChoices.Count; i++)
        {
            int choiceIndex = i; // 防止閉包錯誤
            choices[i].SetActive(true);
            choicesText[i].text = currnetChoices[i].text;

            Button btn = choices[i].GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                OnChoiceSelected(choiceIndex);
            });
        }
    }

    private void OnChoiceSelected(int choiceIndex)
    {
        DialogueManager.Instance.CurrentStory.ChooseChoiceIndex(choiceIndex);
        HideChoices(); // ⬅️ 加這行
        ContinueStory();
    }

    private void HideChoices()
    {
        foreach (GameObject choice in choices)
        {
            choice.SetActive(false);
        }
    }
    
    private void HandleTags(List<string> tags)
    {
        foreach (var tag in tags)
        {
            Debug.Log(tag);
            if (tag.StartsWith("scene: "))
            {
                // 处理场景标签
            }
            else if (tag.StartsWith("character: "))
            {
                ParseCharacterDefinition(tag);
            }
            else if (tag.StartsWith("enter:"))
            {
                string content = tag.Substring("enter:".Length).Trim();
                string[] parts = content.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    string characterName = parts[0];
                    string side = parts[1];
                    SetCharacterImage(side, characterName);
                }
            }
            else if (tag.StartsWith("exit:"))
            {
                string side = tag.Substring("exit:".Length).Trim();
                ClearCharacterImage(side);
            }
            else if (tag.StartsWith("speaker: "))
            {
                currentSpeaker = tag.Substring("speaker:".Length).Trim();
                characterNameText.text = currentSpeaker;
            }
            else if (tag.StartsWith("additem: "))
            {
                string content = tag.Substring("additem:".Length).Trim();
                string[] parts = content.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    int itemID = int.Parse(parts[0]);
                    int itemQuantity = int.Parse(parts[1]);
                    InventoryManager.Instance.AddItem(itemID, itemQuantity);
                }
            }
        }
    }

    // 解析角色定義（動態添加到 portraitMap）
    private void ParseCharacterDefinition(string tag)
    {
        string paramsStr = tag.Substring("character: ".Length);
        Dictionary<string, string> parameters = new Dictionary<string, string>();
    
        foreach (string param in paramsStr.Split(','))
        {
            string trimmedParam = param.Trim();
            string[] pair = trimmedParam.Split('=');
            if (pair.Length == 2)
            {
                string key = pair[0].Trim();
                string value = pair[1].Trim();
                parameters[key] = value;
            }
        }

        if (parameters.TryGetValue("name", out string charName) &&
            parameters.TryGetValue("sprite", out string spriteName))
        {
            Sprite sprite = characterInfoList.FirstOrDefault(c => c.characterName == spriteName)?.portrait;
            if (sprite != null)
            {
                characterPortraitMap[charName] = sprite;
            }
            else
            {
                Debug.LogWarning($"角色 '{charName}' 的 Sprite '{spriteName}' 不存在于 characterInfoList 中！");
            }
        }
    }
    
    private void SetCharacterImage(string side, string characterName)
    {
        // 先清除同側現有角色
        ClearCharacterImage(side);

        if (!characterPortraitMap.TryGetValue(characterName, out var sprite))
        {
            Debug.LogWarning($"找不到角色圖片：{characterName}");
            return;
        }

        if (side == "left")
        {
            leftCharacterImage.sprite = sprite;
            leftCharacterImage.gameObject.SetActive(true);
        }
        else if (side == "right")
        {
            rightCharacterImage.sprite = sprite;
            rightCharacterImage.gameObject.SetActive(true);
        }
    }

    private void ClearCharacterImage(string side)
    {
        if (side == "left")
        {
            leftCharacterImage.sprite = null;
            leftCharacterImage.gameObject.SetActive(false);
        }
        else if (side == "right")
        {
            rightCharacterImage.sprite = null;
            rightCharacterImage.gameObject.SetActive(false);
        }
    }
}   

[System.Serializable]
public class CharacterInfo
{
    public string characterName;
    public Sprite portrait;
}
