using Game.Input;
using Game.Audio;
using UnityEngine;

namespace Game.UI
{
    public class BasePanel : MonoBehaviour
    {
        protected GameObject uiPanel;
        protected bool isRemove = false;
        protected UIType panelType; // 用來儲存傳入的 UIType
        
        public UIType CurrentUIType => panelType;
        
        public UIGroup Group { get; set; } // 每個 UI Panel 有一個分組

        [Header("當前 UI 是否需要設置成進入 UI 狀態（會調整 InputManagers 的按鍵輸入）")]
        [SerializeField] InputType isSetInputActive;

        [Header("按鈕的基礎音效")]
        [SerializeField] protected AudioData audio_NormalBtn;

        protected virtual void Awake()
        {
            uiPanel = gameObject;
        }

        protected virtual void Start()
        {
            // 設定輸入狀態，可根據需要在面板開啟時調整
            GameManager.Instance.InputManagers.SetInputActive(isSetInputActive);
        }

        protected virtual void OnDestroy()
        {
            
        }

        /// <summary>
        /// 設定這個 Panel 是否顯示
        /// </summary>
        public virtual void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }

        /// <summary>
        /// 開啟 Panel 並儲存傳入的 UIType
        /// </summary>
        public virtual void OpenPanel(UIType type)
        {
            panelType = type;
            SetActive(true);
        }

        /// <summary>
        /// 【防呆設計 - 子類別使用】
        /// 向 UIManager 發出「關閉請求」。
        /// UIManager 會檢查堆疊並決定是否關閉。
        /// 子類別（如 SettingsWindow）的關閉按鈕應該呼叫這個。
        /// </summary>
        protected void RequestClose()
        {
            // 如果 GameManager 或 UIManager 已經被銷毀，
            // 至少要能把自己銷毀，避免殘留
            if (GameManager.Instance != null && GameManager.Instance.UIManager != null)
            {
                // 透過 UIManager 安全地關閉
                GameManager.Instance.UIManager.ClosePanel(panelType);
            }
            else
            {
                Debug.LogWarning($"[BasePanel] UIManager 不存在，強行銷毀 {panelType}");
                ExecuteClose(); // 緊急備案
            }
        }

        /// <summary>
        /// 【防呆設計 - UIManager 專用】
        /// 執行真正的關閉和銷毀邏輯。
        /// 標記為 'internal' 使其只在你的遊戲邏輯內部可見，
        /// 降低了從外部被誤用的風險。
        /// </summary>
        internal virtual void ExecuteClose()
        {
            isRemove = true;
            SetActive(false);
            Destroy(gameObject); // 若未來有面板回收系統，可替換成回收邏輯
        }
    }
}
