using Game.UI;
using UnityEngine;

/// <summary>
/// 對話安全網：偵測「對話視窗被別的東西關掉了，但對話系統還以為在播放」的狀況，並自動收尾。
///
/// 為什麼需要：UIManager 按 ESC 時會直接關閉最上層的 Panel（也就是對話視窗），
/// 但 Dialoguecontroller 不知道，DialogueIsPlaying 會一直是 true，
/// 結果玩家永遠被鎖住、也無法再開新的對話（整個遊戲卡住）。
///
/// 用法：把這支腳本放進專案就好，不用掛任何東西。
/// 遊戲開始時會自動建立一個常駐物件（切換場景也不會消失）。
/// 不需要修改同事的 Dialoguecontroller / UIManager。
/// </summary>
public class DialogueSafetyNet : MonoBehaviour
{
    private bool seenOpen;
    private int missingFrames;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindFirstObjectByType<DialogueSafetyNet>() != null) return;

        var go = new GameObject("DialogueSafetyNet");
        DontDestroyOnLoad(go);
        go.AddComponent<DialogueSafetyNet>();
    }

    private void Update()
    {
        var dc = Dialoguecontroller.Instance;
        if (dc == null || !dc.DialogueIsPlaying)
        {
            seenOpen = false;
            missingFrames = 0;
            return;
        }

        var gm = GameManager.Instance;
        var ui = gm != null ? gm.UIManager : null;
        if (ui == null) return;

        bool open = ui.IsPanelOpen(UIType.DialogueWindowNew) || ui.IsPanelOpen(UIType.NarrationWindow);

        if (open)
        {
            // 視窗是非同步開啟的，必須先看過它開著，之後才能判斷「被關掉了」
            seenOpen = true;
            missingFrames = 0;
            return;
        }

        if (!seenOpen) return;

        // 連續幾幀都不見才處理，避免視窗切換瞬間誤判
        if (++missingFrames >= 3)
        {
            Debug.LogWarning("[DialogueSafetyNet] 對話視窗被外部關閉（例如按 ESC），已自動結束對話並解除鎖定。");
            seenOpen = false;
            missingFrames = 0;
            dc.EndDialogue();
        }
    }
}
