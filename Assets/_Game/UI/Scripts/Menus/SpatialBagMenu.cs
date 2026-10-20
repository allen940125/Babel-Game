using System.Collections.Generic;
using Gamemanager;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI
{
    public class SpatialBagMenu : BasePanel 
    {
        [Header("通用操作按鈕")]
        [SerializeField] private Button closeButton;
        
        [Header("背包坑位設定")]
        [Tooltip("請將 Hierarchy 裡面排好的 ItemAnchor 全部拖入此陣列")]
        [SerializeField] private ItemAnchor[] allAnchors;

        [Header("顯示模式切換")]
        [Tooltip("打勾：滑鼠懸浮時顯示跟隨格子的浮動資訊框。取消打勾：使用右側固定面板。")]
        public bool useFloatingTooltip = true;

        [Header("當前選中物品資訊 (浮動面板 Tooltip)")]
        [SerializeField] private GameObject floatingTooltipPanel;
        [SerializeField] private RectTransform floatingTooltipRect;
        [SerializeField] private Image tooltipItemIcon;
        [SerializeField] private TMP_Text tooltipItemName;
        [SerializeField] private TMP_Text tooltipItemDescription;
        [Tooltip("滑鼠游標與面板的預設距離 (X往右，Y往下通常設負值)")]
        [SerializeField] private Vector2 floatingtooltipOffset = new Vector2(15f, -15f);

        // 用來控制是否要在 Update 中持續更新座標的開關
        private bool _isTooltipActive = false;
        
        protected override void Awake()
        {
            base.Awake();
            
            InitializeCommonButtons();
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnPlayerBagRefreshedEvent, OnBagRefreshed);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnInventoryItemHoveredEvent, OnInventoryItemHoveredEvent);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameManager.Instance.MainGameEvent.Unsubscribe<PlayerBagRefreshedEvent>(OnBagRefreshed);
            GameManager.Instance.MainGameEvent.Unsubscribe<InventoryItemHoveredEvent>(OnInventoryItemHoveredEvent);
        }

        protected override void Start()
        {
            base.Start();
            GameManager.Instance.MainGameEvent.Send(new PlayerBagRefreshedEvent() { ItemControllerType = ItemControllerType.All });
        }

        private void Update()
        {
            // 每幀更新浮動面板位置
            if (_isTooltipActive && useFloatingTooltip && floatingTooltipPanel != null && floatingTooltipPanel.activeSelf)
            {
                UpdateTooltipPosition();
            }
        }
        
        private void OnBagRefreshed(PlayerBagRefreshedEvent cmd)
        {
            // 1. 透過 Manager 取得攤平並過濾好的資料
            List<InventoryItemRuntimeData> currentItems = InventoryManager.Instance.GetPlayerItems(cmd.ItemControllerType);

            // 2. 執行 ID 配對
            foreach (var anchor in allAnchors)
            {
                var matchData = currentItems.Find(item => item.itemId == anchor.targetItemID);
                anchor.BindData(matchData);
            }
        }
        
        private void OnInventoryItemHoveredEvent(InventoryItemHoveredEvent cmd)
        {
            if (useFloatingTooltip)
            {
                // 浮動模式：隱藏你的固定面板 (請替換為 SpatialBagMenu 實際的固定面板變數)
                // if (fixedInfoPanel != null) fixedInfoPanel.SetActive(false);

                if (cmd.StoredInventoryItemRuntimeData != null && cmd.HoveredSlotRect != null)
                {
                    UpdateFloatingTooltip(cmd.StoredInventoryItemRuntimeData);
                }
                else
                {
                    if (floatingTooltipPanel != null) floatingTooltipPanel.SetActive(false);
                    _isTooltipActive = false;
                }
            }
            else
            {
                // 固定面板模式
                if (floatingTooltipPanel != null) floatingTooltipPanel.SetActive(false);
                _isTooltipActive = false;
                
                // 顯示你的固定面板並更新資料
                // if (fixedInfoPanel != null) fixedInfoPanel.SetActive(true);
                // UpdateItemInfoPanel(cmd.StoredInventoryItemRuntimeData ?? InventoryManager.Instance.curClickInventoryItemRuntimeData);
            }
        }

        private void UpdateFloatingTooltip(InventoryItemRuntimeData data)
        {
            if (data == null || data.BaseTemplete == null || floatingTooltipPanel == null) return;

            floatingTooltipPanel.SetActive(true);
            tooltipItemIcon.LoadSpriteAsync(data.BaseTemplete.ItemIconPath);
            tooltipItemName.text = data.BaseTemplete.Name;
            tooltipItemDescription.text = data.BaseTemplete.ItemDescription;

            _isTooltipActive = true;
            UpdateTooltipPosition(); // 立刻更新一次位置防閃爍
        }

        private void UpdateTooltipPosition()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 finalPos = mousePos + floatingtooltipOffset;

            float tooltipWidth = floatingTooltipRect.rect.width * floatingTooltipRect.lossyScale.x;
            float tooltipHeight = floatingTooltipRect.rect.height * floatingTooltipRect.lossyScale.y;

            // 碰到底部：把面板往上推到游標上方
            if (finalPos.y - tooltipHeight < 0)
            {
                finalPos.y = mousePos.y - floatingtooltipOffset.y + tooltipHeight;
            }

            // 碰到右側：把面板往左推到游標左側
            if (finalPos.x + tooltipWidth > Screen.width)
            {
                finalPos.x = mousePos.x - floatingtooltipOffset.x - tooltipWidth;
            }

            // 極端情況防護
            if (finalPos.y > Screen.height) finalPos.y = Screen.height;
            if (finalPos.x < 0) finalPos.x = 0;

            floatingTooltipRect.position = finalPos;
        }
        
        void InitializeCommonButtons()
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        void OnCloseButtonClicked()
        {
            RequestClose();
        }
    }
}