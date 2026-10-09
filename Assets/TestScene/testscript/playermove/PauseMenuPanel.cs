using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ESC 選單面板（目前只有兩個沒有功能的按鈕：離開遊戲、讀檔）。
/// 掛在選單面板的最外層物件上。由 PlayerExplorationController 在按 ESC 時呼叫 Open / Close。
///
/// 開啟時會把遊戲時間暫停（Time.timeScale = 0），關閉或物件被關掉時自動還原。
/// 按鈕的 OnClick 先接 OnQuitButton / OnLoadButton，之後做好功能時，
/// 只要在下面兩個 UnityEvent 接上實際行為即可，不用改這支腳本。
/// </summary>
public class PauseMenuPanel : MonoBehaviour
{
    [Tooltip("打開選單時是否暫停遊戲時間（敵人、物理、計時都會停）")]
    [SerializeField] private bool pauseTimeWhileOpen = true;

    [Tooltip("按下「離開遊戲」時觸發（功能之後再接）")]
    [SerializeField] private UnityEvent onQuitRequested;
    [Tooltip("按下「讀檔」時觸發（功能之後再接）")]
    [SerializeField] private UnityEvent onLoadRequested;

    private float previousTimeScale = 1f;
    private bool timePaused;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        // 場景開始時就算忘了先關掉，也會自動收起來
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        RestoreTime(); // 不管是 Close、換場景還是物件被銷毀，都不能讓遊戲停在暫停狀態
    }

    public void Open()
    {
        if (IsOpen) return;
        gameObject.SetActive(true);

        if (pauseTimeWhileOpen && !timePaused)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            timePaused = true;
        }
    }

    public void Close()
    {
        if (!IsOpen) return;
        gameObject.SetActive(false); // OnDisable 會自動還原時間
    }

    /// <summary>「離開遊戲」按鈕的 OnClick 接這個。</summary>
    public void OnQuitButton()
    {
        Debug.Log("[PauseMenu] 離開遊戲：功能尚未實作");
        onQuitRequested?.Invoke();
    }

    /// <summary>「讀檔」按鈕的 OnClick 接這個。</summary>
    public void OnLoadButton()
    {
        Debug.Log("[PauseMenu] 讀檔：功能尚未實作");
        onLoadRequested?.Invoke();
    }

    private void RestoreTime()
    {
        if (!timePaused) return;
        Time.timeScale = previousTimeScale;
        timePaused = false;
    }
}
