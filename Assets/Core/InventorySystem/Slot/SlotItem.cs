using Gamemanager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI; // 必須引入此命名空間

public class SlotItem : Slot, IPointerEnterHandler, IPointerExitHandler,IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public InventoryItemRuntimeData storedInventoryItemRuntimeData;
    
    // 用於顯示拖曳殘影的臨時物件
    private GameObject _dragGhost;
    private RectTransform _ghostRect;
    
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (storedInventoryItemRuntimeData == null) return;
        
        GameManager.Instance.MainGameEvent.Send(new InventoryItemHoveredEvent() 
        { 
            StoredInventoryItemRuntimeData = storedInventoryItemRuntimeData,
            HoveredSlotRect = this.GetComponent<RectTransform>() // ★ 把自己的變換元件傳出去
        });
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GameManager.Instance.MainGameEvent.Send(new InventoryItemHoveredEvent() 
        { 
            StoredInventoryItemRuntimeData = null,
            HoveredSlotRect = null
        });
    }
    
    // ==========================================
    // ★ 拖曳系統實作
    // ==========================================
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 嚴謹校驗：資料必須存在、數量必須 > 0、且必須要有真實圖片才能拖曳
        if (storedInventoryItemRuntimeData == null || 
            storedInventoryItemRuntimeData.quantity <= 0 ||
            imageItemIcon.sprite == null) 
        {
            return;
        }

        // 1. 生成一個臨時的 2D 殘影
        _dragGhost = new GameObject("DragGhost");
        // 必須放在最上層 Canvas 下，避免被其他 UI 遮擋
        Canvas topmostCanvas = GetComponentInParent<Canvas>().rootCanvas;
        _dragGhost.transform.SetParent(topmostCanvas.transform, false);

        // 2. 複製當前 Slot 的圖示給殘影
        Image ghostImage = _dragGhost.AddComponent<Image>();
        ghostImage.sprite = this.imageItemIcon.sprite; // 假設你有這個參照
        ghostImage.raycastTarget = false; // 絕對不能擋住射線

        _ghostRect = _dragGhost.GetComponent<RectTransform>();
        _ghostRect.sizeDelta = GetComponent<RectTransform>().sizeDelta;
        
        // 拖曳開始時，視為滑鼠移出，關閉浮動資訊
        OnPointerExit(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragGhost != null)
        {
            // 殘影無條件跟隨滑鼠
            _ghostRect.position = Mouse.current.position.ReadValue();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragGhost != null)
        {
            Destroy(_dragGhost); // 銷毀殘影
        }

        if (storedInventoryItemRuntimeData == null) return;

        // ★ 核心判定：滑鼠放開時，是否懸浮在任何 UI 物件上？
        // 如果為 true，代表丟在其他介面上 (例如想整理背包)，不處理丟棄。
        // 如果為 false，代表丟在透明背景 / 3D 遊戲畫面上，執行實體化。
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            ExecuteDropToWorld();
        }
    }

    private void ExecuteDropToWorld()
    {
        // 1. 取得滑鼠的螢幕 2D 座標
        Vector2 mousePos = Mouse.current.position.ReadValue();

        // 2. 座標轉換的數學關鍵：ScreenToWorldPoint 的 Z 值代表「目標平面距離攝影機有多遠」
        // 假設你的攝影機在 Z = -10，而你要把物品丟在 Z = 0 的平面，這個距離就是 10。
        float distanceToZZero = Mathf.Abs(Camera.main.transform.position.z);
        
        // 3. 轉換為世界座標
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, distanceToZZero));
        
        // 4. 強制將 Z 軸歸零，確保完全貼齊你的 2.5D 基準面
        worldPosition.z = 0f;

        // 5. 發送廣播，不再依賴 hit.point
        GameManager.Instance.MainGameEvent.Send(new ItemDroppedInWorldEvent()
        {
            ItemData = storedInventoryItemRuntimeData,
            DropPosition = worldPosition
        });
    }
}