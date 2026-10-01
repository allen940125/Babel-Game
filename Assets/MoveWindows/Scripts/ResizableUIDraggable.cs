using UnityEngine;
using UnityEngine.EventSystems;

public class ResizableUIDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private ResizableUIObject _resizableElement;
    [SerializeField] private ResizableUIDraggableIds _draggableId;

    public ResizableUIDraggableIds DraggableID => _draggableId;

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[ResizeNode] 🟢 開始拖曳縮放角：{gameObject.name} ({_draggableId})");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_resizableElement == null)
        {
            Debug.LogError($"[ResizeNode] ❌ {_draggableId} 沒有指定 ResizableUIObject！");
            return;
        }

        Debug.Log($"[ResizeNode] 🔵 傳遞 Delta: {eventData.delta} 給 ResizableUIObject");
        _resizableElement.UpdateSize(this, eventData.delta);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"[ResizeNode] 🔴 結束拖曳縮放角：{gameObject.name}");
    }
}