using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Audio;
using Gamemanager;

namespace Game.UI
{
    public class ConfirmPanel : BasePanel
    {
        [Header("★ 介面元件綁定 (請在 Inspector 拉好，嚴禁 Runtime 尋找)")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        // 用來儲存外部傳進來的指令委派
        private Action _onConfirmAction;

        protected override void Awake()
        {
            base.Awake();
            InitializeButtons();
        }

        private void InitializeButtons()
        {
            yesButton.onClick.AddListener(OnYesButtonClicked);
            noButton.onClick.AddListener(OnNoButtonClicked);
        }

        /// <summary>
        /// 由 UIManager 或 Wrapper 呼叫，注入字串與委派。
        /// 呼叫此方法前，系統應該已經透過你框架的 OpenPanel 邏輯將此介面實例化或啟動。
        /// </summary>
        public void ShowConfirm(string title, string message, Action onConfirm)
        {
            if (titleText != null) titleText.text = title;
            if (messageText != null) messageText.text = message;
            
            _onConfirmAction = onConfirm;
        }

        private void OnYesButtonClicked()
        {
            // 如果你的 BasePanel 有定義按鈕音效變數，請自行替換 audio_NormalBtn
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayUISound(audio_NormalBtn); 

            // 1. 引爆引信：執行 Wrapper 傳進來的真正指令
            _onConfirmAction?.Invoke();

            // 2. 記憶體清理
            _onConfirmAction = null;

            // 3. 呼叫 BasePanel 的關閉機制
            RequestClose();
        }

        private void OnNoButtonClicked()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayUISound(audio_NormalBtn);

            // 1. 玩家拒絕，直接清理記憶體
            _onConfirmAction = null;

            // 2. 呼叫 BasePanel 的關閉機制
            RequestClose();
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            // 防呆：如果面板被強制銷毀，確保釋放參考
            _onConfirmAction = null;
        }
    }
}