using Gamemanager;
using TMPro; // 引入文字
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI
{
    // ★ 記得補上 IPointerClickHandler 介面，否則 OnPointerClick 不會觸發
    public class ItemAnchor : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("專屬綁定設定")]
        [Tooltip("這個坑位專門用來顯示哪個 ID 的物品？")]
        public int targetItemID; 

        [Header("UI 元件綁定")]
        [SerializeField] private GameObject contentRoot; 
        [SerializeField] private Image iconImage;        
        [SerializeField] private TMP_Text quantityText;  // ★ 新增：數量文字

        private InventoryItemRuntimeData _storedData;
        private GameObject _dragGhost;
        private RectTransform _ghostRect;

        public void BindData(InventoryItemRuntimeData data)
        {
            _storedData = data;

            // 沒資料，或是資料數量為 0，就關閉此坑位
            if (_storedData == null || _storedData.BaseTemplete == null || _storedData.quantity <= 0)
            {
                contentRoot.SetActive(false); 
                return;
            }

            contentRoot.SetActive(true); 
            iconImage.LoadSpriteAsync(_storedData.BaseTemplete.ItemIconPath); 
            
            // ★ 新增：更新數量顯示 (如果數量是 1，看你要不要隱藏文字)
            if (quantityText != null)
            {
                quantityText.text = _storedData.quantity > 1 ? _storedData.quantity.ToString() : "";
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_storedData == null) return;
            GameManager.Instance.MainGameEvent.Send(new InventoryItemClickedEvent() { StoredInventoryItemRuntimeData = _storedData });
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_storedData == null) return;
            GameManager.Instance.MainGameEvent.Send(new InventoryItemHoveredEvent() 
            { 
                StoredInventoryItemRuntimeData = _storedData,
                HoveredSlotRect = this.GetComponent<RectTransform>()
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

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_storedData == null || _storedData.quantity <= 0 || iconImage.sprite == null) return;

            _dragGhost = new GameObject("DragGhost");
            Canvas topmostCanvas = GetComponentInParent<Canvas>().rootCanvas;
            _dragGhost.transform.SetParent(topmostCanvas.transform, false);

            Image ghostImage = _dragGhost.AddComponent<Image>();
            ghostImage.sprite = this.iconImage.sprite; 
            ghostImage.raycastTarget = false; 

            _ghostRect = _dragGhost.GetComponent<RectTransform>();
            _ghostRect.sizeDelta = GetComponent<RectTransform>().sizeDelta;
            
            OnPointerExit(eventData); 
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragGhost != null) _ghostRect.position = Mouse.current.position.ReadValue();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dragGhost != null) Destroy(_dragGhost);
            if (_storedData == null) return;

            if (!EventSystem.current.IsPointerOverGameObject())
            {
                ExecuteDropToWorld();
            }
        }

        private void ExecuteDropToWorld()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            float distanceToZZero = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, distanceToZZero));
            worldPosition.z = 0f;

            GameManager.Instance.MainGameEvent.Send(new ItemDroppedInWorldEvent()
            {
                ItemData = _storedData,
                DropPosition = worldPosition
            });
        }

        public void OnClick()
        {
            
        }
    }
}