using Gamemanager;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI
{
    public class BagMenu : BasePanel
    {
        [Header("通用操作按鈕")]
        [SerializeField] private Button useButton;
        [SerializeField] private Button closeButton;
    
        [Header("物品分類標籤頁")]
        [SerializeField] private Button allItemTabButton; // 新增：全部物品按鈕
        [SerializeField] private Button equipmentTabButton; 
        [SerializeField] private Button consumableTabButton; 
        [SerializeField] private Button materialTabButton;
        [SerializeField] private Button keyItemTabButton;

        [Header("商店資訊")] // 你的命名是 StoreItem，但這裡是 Bag，建議檢討變數命名一致性
        [SerializeField] GameObject prefabSlotStoreItem;
        [SerializeField] GameObject scrollViewContentStoreItemListGrid;
        
        [Header("顯示模式切換")]
        [Tooltip("打勾：滑鼠懸浮時顯示跟隨格子的浮動資訊框。取消打勾：使用右側固定面板。")]
        public bool useFloatingTooltip = true;

        [Header("當前選中物品資訊 (固定面板)")]
        [SerializeField] private GameObject fixedInfoPanel; // 建議把固定面板包成一個父節點方便開關
        [SerializeField] private Image selectedItemIcon;
        [SerializeField] private TMP_Text selectedItemName;
        [SerializeField] private TMP_Text selectedItemDescription;

        [Header("當前選中物品資訊 (浮動面板 Tooltip)")]
        [SerializeField] private GameObject floatingTooltipPanel;
        [SerializeField] private RectTransform floatingTooltipRect; // 用於計算座標
        [SerializeField] private Image tooltipItemIcon;
        [SerializeField] private TMP_Text tooltipItemName;
        [SerializeField] private TMP_Text tooltipItemDescription;
        [Tooltip("滑鼠游標與面板的預設距離 (X往右，Y往下通常設負值)")]
        [SerializeField] private Vector2 floatingtooltipOffset = new Vector2(15f, -15f);

        // ★ 新增：用來控制是否要在 Update 中持續更新座標的開關
        [SerializeField] private bool _isTooltipActive = false;
        protected override void Awake()
        {
            base.Awake();
            
            //GameManager.Instance.UIManager.ClosePanel(UIType.GameHUD);
           
            InitializeCommonButtons();
            InitializeCategoryButtons();
            
            // 訂閱點擊與懸浮事件
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnInventoryItemClickedEvent, OnInventoryItemClickedEvent);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnInventoryItemHoveredEvent, OnInventoryItemHoveredEvent);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            //GameManager.Instance.UIManager.OpenPanel<GameHUD>(UIType.GameHUD);
            GameManager.Instance.MainGameEvent.Unsubscribe<InventoryItemClickedEvent>(OnInventoryItemClickedEvent);
            GameManager.Instance.MainGameEvent.Unsubscribe<InventoryItemHoveredEvent>(OnInventoryItemHoveredEvent);
        }
        
        protected override void Start()
        {
            base.Start();

            InventoryManager.InventoryPanelController.SetBagInfo(uiPanel, scrollViewContentStoreItemListGrid, prefabSlotStoreItem);
            
            GameManager.Instance.MainGameEvent.Send(new CursorToggledEvent() { ShowCursor = true });
            //InventoryManager.InventoryPanelController.RefreshPlayerBagItem(ItemControllerType.All);
            // 預設開啟時顯示全部
            GameManager.Instance.MainGameEvent.Send(new PlayerBagRefreshedEvent() { ItemControllerType = ItemControllerType.All });
            if (floatingTooltipPanel != null) floatingTooltipPanel.SetActive(false);
        }

        // ★ 新增：每幀更新浮動面板位置，達成跟隨滑鼠的效果
        private void Update()
        {
            if (_isTooltipActive && useFloatingTooltip && floatingTooltipPanel.activeSelf)
            {
                UpdateTooltipPosition();
            }
        }
        
        private void OnInventoryItemClickedEvent(InventoryItemClickedEvent cmd)
        {
            UpdateItemInfoPanel(cmd.StoredInventoryItemRuntimeData);
        }
        // 處理懸浮事件
        private void OnInventoryItemHoveredEvent(InventoryItemHoveredEvent cmd)
        {
            if (useFloatingTooltip)
            {
                // ★ 模式 A：浮動模式 (Tooltip)
                fixedInfoPanel.SetActive(false); // 確保固定面板關閉

                if (cmd.StoredInventoryItemRuntimeData != null && cmd.HoveredSlotRect != null)
                {
                    UpdateFloatingTooltip(cmd.StoredInventoryItemRuntimeData);
                }
                else
                {
                    floatingTooltipPanel.SetActive(false); // 滑鼠移出，隱藏浮動框
                }
            }
            else
            {
                // ★ 模式 B：傳統固定面板模式
                floatingTooltipPanel.SetActive(false); // 確保浮動框關閉
                fixedInfoPanel.SetActive(true);

                if (cmd.StoredInventoryItemRuntimeData != null)
                {
                    UpdateItemInfoPanel(cmd.StoredInventoryItemRuntimeData);
                }
                else
                {
                    UpdateItemInfoPanel(InventoryManager.Instance.curClickInventoryItemRuntimeData);
                }
            }
        }

        // 浮動面板專屬更新邏輯
        private void UpdateFloatingTooltip(InventoryItemRuntimeData data)
        {
            if (data == null || data.BaseTemplete == null) return;

            floatingTooltipPanel.SetActive(true);
            tooltipItemIcon.LoadSpriteAsync(data.BaseTemplete.ItemIconPath);
            tooltipItemName.text = data.BaseTemplete.Name;
            tooltipItemDescription.text = data.BaseTemplete.ItemDescription;

            // 啟動跟隨邏輯，並立刻更新一次位置防閃爍
            _isTooltipActive = true;
            UpdateTooltipPosition();
        }
        
        // ★ 核心物理邏輯：跟隨滑鼠與螢幕邊界運算
        private void UpdateTooltipPosition()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 finalPos = mousePos + floatingtooltipOffset;

            // 取得面板的實際寬高 (考量畫布縮放)
            float tooltipWidth = floatingTooltipRect.rect.width * floatingTooltipRect.lossyScale.x;
            float tooltipHeight = floatingTooltipRect.rect.height * floatingTooltipRect.lossyScale.y;

            // --- 螢幕邊界防呆計算 (假設 Pivot 在左上角 X:0, Y:1) ---
            
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

            // 極端情況防護 (超出頂部或左側)
            if (finalPos.y > Screen.height) finalPos.y = Screen.height;
            if (finalPos.x < 0) finalPos.x = 0;

            floatingTooltipRect.position = finalPos;
        }
        
        // 將原本的 UpdateClickItemInfo 改名為 UpdateItemInfoPanel，使其適用於所有資訊更新場景
        private void UpdateItemInfoPanel(InventoryItemRuntimeData data)
        {
            // 修正：必須同時檢查 data 是否為 null，以及其 BaseTemplete 是否有效 (過濾未點擊時的 Unity 序列化空殼)
            if (data == null || data.BaseTemplete == null)
            {
                selectedItemIcon.gameObject.SetActive(false);
                selectedItemName.text = string.Empty;
                selectedItemDescription.text = string.Empty;
                return;
            }

            selectedItemIcon.gameObject.SetActive(true);
            selectedItemIcon.LoadSpriteAsync(data.BaseTemplete.ItemIconPath);
            selectedItemName.text = data.BaseTemplete.Name;
            selectedItemDescription.text = data.BaseTemplete.ItemDescription;
        }
        
        // void UpdateClickItemInfo(InventoryItemRuntimeData inventoryItemRuntimeData)
        // {
        //     if (inventoryItemRuntimeData == null) return;
        //
        //     selectedItemIcon.sprite = inventoryItemRuntimeData.BaseTemplete.ItemIconPath;
        //     selectedItemName.text = inventoryItemRuntimeData.BaseTemplete.Name;
        //     selectedItemDescription.text = inventoryItemRuntimeData.BaseTemplete.ItemDescription;
        // }
        
        void InitializeCommonButtons()
        {
            useButton.onClick.AddListener(OnUseButtonClicked);
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        void InitializeCategoryButtons()
        {
            // 必須在 Inspector 中將按鈕拖曳綁定
            if (allItemTabButton != null) allItemTabButton.onClick.AddListener(OnAllItemTabButtonClicked);
            if (equipmentTabButton != null) equipmentTabButton.onClick.AddListener(OnEquipmentTabButtonClicked);
            if (consumableTabButton != null) consumableTabButton.onClick.AddListener(OnConsumableTabButtonClicked);
            if (materialTabButton != null) materialTabButton.onClick.AddListener(OnMaterialTabButtonClicked);
            if (keyItemTabButton != null) keyItemTabButton.onClick.AddListener(OnKeyItemTabButtonClicked);
        }

        void OnUseButtonClicked()
        {
            if (InventoryManager.Instance.curClickInventoryItemRuntimeData != null)
            {
                // TODO: 執行物品使用邏輯
            }
        }

        void OnCloseButtonClicked()
        {
            RequestClose();
        }

        // --- 分類按鈕事件發送 ---

        void OnAllItemTabButtonClicked()
        {
            // 你必須去定義 ItemControllerType.All
            GameManager.Instance.MainGameEvent.Send(new PlayerBagRefreshedEvent() { ItemControllerType = ItemControllerType.All });  
        }

        void OnEquipmentTabButtonClicked()
        {
            GameManager.Instance.MainGameEvent.Send(new PlayerBagRefreshedEvent() { ItemControllerType = ItemControllerType.Equipment });  
        }

        void OnConsumableTabButtonClicked()
        {
            GameManager.Instance.MainGameEvent.Send(new PlayerBagRefreshedEvent() { ItemControllerType = ItemControllerType.Consumable });  
        }
        
        void OnMaterialTabButtonClicked()
        {
            GameManager.Instance.MainGameEvent.Send(new PlayerBagRefreshedEvent() { ItemControllerType = ItemControllerType.Material });  
        }

        void OnKeyItemTabButtonClicked()
        {
            GameManager.Instance.MainGameEvent.Send(new PlayerBagRefreshedEvent() { ItemControllerType = ItemControllerType.KeyItem });  
        }
    }
}