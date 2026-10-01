using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WindowDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("拖曳目標 (必須是視窗的根節點 basepanel)")]
    [SerializeField] private RectTransform _targetRectTransform;
    private Canvas _parentCanvas;

    private void Awake()
    {
        if (_targetRectTransform == null) _targetRectTransform = GetComponent<RectTransform>();
        _parentCanvas = GetComponentInParent<Canvas>();

        if (_targetRectTransform != null)
        {
            if (_targetRectTransform.GetComponent<LayoutGroup>() != null)
                Debug.LogError($"[WindowDraggable] 嚴重警告：目標 {_targetRectTransform.name} 掛載了 LayoutGroup，這將導致座標被強制覆寫！");
            
            Debug.Log($"[WindowDraggable] 初始設定完畢。目標: {_targetRectTransform.name}, 初始座標: {_targetRectTransform.anchoredPosition}, 錨點 Min: {_targetRectTransform.anchorMin}, Max: {_targetRectTransform.anchorMax}");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_targetRectTransform != null)
        {
            _targetRectTransform.SetAsLastSibling();
            Debug.Log($"[WindowDraggable] 🟢 開始拖曳視窗。當前座標: {_targetRectTransform.anchoredPosition}");
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_targetRectTransform == null || _parentCanvas == null) return;

        Vector2 beforePos = _targetRectTransform.anchoredPosition;
        Vector2 delta = eventData.delta / _parentCanvas.scaleFactor;
        _targetRectTransform.anchoredPosition += delta;

        Debug.Log($"[WindowDraggable] 🔵 拖曳中 | 滑鼠 Delta: {eventData.delta} | 校正後 Delta: {delta} | 座標變化: {beforePos} -> {_targetRectTransform.anchoredPosition}");
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"[WindowDraggable] 🔴 結束拖曳視窗。最終座標: {_targetRectTransform.anchoredPosition}");
    }
}