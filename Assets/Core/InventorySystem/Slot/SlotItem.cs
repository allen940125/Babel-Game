using Gamemanager;
using UnityEngine.EventSystems; // 必須引入此命名空間

public class SlotItem : Slot, IPointerEnterHandler, IPointerExitHandler
{
    public InventoryItemRuntimeData storedInventoryItemRuntimeData;
    
    public void Initialize(InventoryItemRuntimeData inventoryItemRuntimeData)
    {
        storedInventoryItemRuntimeData = inventoryItemRuntimeData;
        base.Initialize(inventoryItemRuntimeData.BaseTemplete);
        textItemQuantity.text = inventoryItemRuntimeData.quantity.ToString();
    }

    protected override void ItemOnClicked()
    {
        base.ItemOnClicked();
        // 點擊事件：負責實質的「選中」，供使用按鈕操作
        GameManager.Instance.MainGameEvent.Send(new InventoryItemClickedEvent() { StoredInventoryItemRuntimeData = storedInventoryItemRuntimeData });
    }

    // 實作滑鼠進入事件 (僅觸發一次)
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (storedInventoryItemRuntimeData == null) return;
        
        // 發送懸浮事件：通知 UI 更新顯示資訊
        // 需自行定義 InventoryItemHoveredEvent
        GameManager.Instance.MainGameEvent.Send(new InventoryItemHoveredEvent() { StoredInventoryItemRuntimeData = storedInventoryItemRuntimeData });
    }

    // 實作滑鼠離開事件 (僅觸發一次)
    public void OnPointerExit(PointerEventData eventData)
    {
        // 傳送 null 代表滑鼠移出，UI 可決定是否要恢復顯示「已選中」的物品資訊
        GameManager.Instance.MainGameEvent.Send(new InventoryItemHoveredEvent() { StoredInventoryItemRuntimeData = null });
    }
}