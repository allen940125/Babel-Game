using Febucci.Examples;
using UnityEngine;
using Yarn;
using UnityEngine.InputSystem;

public class TextTrigger : MonoBehaviour
{
    [SerializeField] private TypewriterWithGlitch glitchTypewriter;

    [System.Serializable]
    public struct DialogueLine
    {
        [TextArea(1, 3)] public string text;
        [Tooltip("要發生動態亂碼的起始字元 Index（忽略 Tag 與空格，從 0 開始算）。無亂碼填 -1")]
        public int glitchStartIndex;
        [Tooltip("亂碼影響的字數長度")]
        public int glitchLength;
    }

    [SerializeField]
    private DialogueLine[] lines = new DialogueLine[]
    {
        new DialogueLine { text = "I'm <shake>Scared</shake>", glitchStartIndex = -1, glitchLength = 0 },
        new DialogueLine { text = "Do you know WHY<rot>?</rot>", glitchStartIndex = -1, glitchLength = 0 },
        new DialogueLine { text = "Because......", glitchStartIndex = -1, glitchLength = 0 },
        new DialogueLine { text = "I REALLY <color=#FF0000>LOVE</color> YOU", glitchStartIndex = -1, glitchLength = 0 },
        new DialogueLine { text = "DO <speed=03.5>YOU<speed> <color=#000000>LOVE</color> ME?", glitchStartIndex = 2, glitchLength = 3 }
    };

    private int currentIndex = 0;

    void Start()
    {
        if (glitchTypewriter != null)
        {
            glitchTypewriter.onTextShowed.AddListener(OnLineFinished);
            ShowCurrentLine();
        }
    }

    void Update()
    {
        // 2. 改用 New Input System 語法：偵測空白鍵或滑鼠左鍵點擊
        bool isSpacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool isMousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if (isSpacePressed || isMousePressed)
        {
            OnPlayerInput();
        }
    }

    void ShowCurrentLine()
    {
        var line = lines[currentIndex];
        glitchTypewriter.PlayTypewriterWithDynamicGlitch(line.text, line.glitchStartIndex, line.glitchLength);
    }

    void OnPlayerInput()
    {
        if (glitchTypewriter.IsTyping)
        {
            // 打字中按下按鍵的處理（視你的需求）
        }
        else
        {
            currentIndex++;
            if (currentIndex < lines.Length)
            {
                ShowCurrentLine();
            }
            else
            {
                OnDialogueEnd();
            }
        }
    }

    void OnLineFinished()
    {
        // 單句播放完畢
    }

    void OnDialogueEnd()
    {
        Debug.Log("對話完全結束！");
        glitchTypewriter.onTextShowed.RemoveListener(OnLineFinished);
    }
}
