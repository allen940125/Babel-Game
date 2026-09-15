using Gamemanager;
using TMPro;
using UnityEngine;
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
        
        [Header("當前選中物品資訊")]
        [SerializeField] private Image selectedItemIcon;
        [SerializeField] private TMP_Text selectedItemName;
        [SerializeField] private TMP_Text selectedItemDescription;

        protected override void Awake()
        {
            base.Awake();
            
            GameManager.Instance.UIManager.ClosePanel(UIType.GameHUD);
           
            InitializeCommonButtons();
            InitializeCategoryButtons();
            
            // 訂閱點擊與懸浮事件
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnInventoryItemClickedEvent, OnInventoryItemClickedEvent);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnInventoryItemHoveredEvent, OnInventoryItemHoveredEvent);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            GameManager.Instance.UIManager.OpenPanel<GameHUD>(UIType.GameHUD);
            GameManager.Instance.MainGameEvent.Unsubscribe<InventoryItemClickedEvent>();
            GameManager.Instance.MainGameEvent.Unsubscribe<InventoryItemHoveredEvent>();
        }
        
        protected override void Start()
        {
            base.Start();

            // 將 UI 參考交給 Controller 處理
            InventoryManager.InventoryPanelController.SetBagInfo(uiPanel, scrollViewContentStoreItemListGrid, prefabSlotStoreItem);
            
            GameManager.Instance.MainGameEvent.Send(new CursorToggledEvent() { ShowCursor = true });
            
            // 預設開啟時顯示全部
            GameManager.Instance.MainGameEvent.Send(new PlayerBagRefreshedEvent() { ItemControllerType = ItemControllerType.All });
        }

        private void OnInventoryItemClickedEvent(InventoryItemClickedEvent cmd)
        {
            UpdateItemInfoPanel(cmd.StoredInventoryItemRuntimeData);
        }
        // 處理懸浮事件
        private void OnInventoryItemHoveredEvent(InventoryItemHoveredEvent cmd)
        {
            if (cmd.StoredInventoryItemRuntimeData != null)
            {
                // 狀態 A：滑鼠移入格子，顯示該懸浮物品的資訊
                UpdateItemInfoPanel(cmd.StoredInventoryItemRuntimeData);
            }
            else
            {
                // 狀態 B：滑鼠移出格子，恢復顯示「當前已在 InventoryManager 中確認選中」的物品
                // 若目前沒有任何點擊選中的物品，則傳入 null 以清空面板
                UpdateItemInfoPanel(InventoryManager.Instance.curClickInventoryItemRuntimeData);
            }
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
            selectedItemIcon.sprite = data.BaseTemplete.ItemIconPath;
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