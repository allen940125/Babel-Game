using UnityEngine;
using UnityEngine.EventSystems;

public class HoverScaleVisualizer3D : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("縮放設定")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1.1f);
    
    private Vector3 _originalScale;
    private bool _isInitialized = false;

    private void Awake()
    {
        if (targetTransform == null) targetTransform = transform;
        _originalScale = targetTransform.localScale;
        _isInitialized = true;
    }

    private void OnDisable()
    {
        // 防呆：物件被隱藏時強制恢復原大小，避免下次出現時維持放大的錯誤狀態
        if (_isInitialized && targetTransform != null)
        {
            targetTransform.localScale = _originalScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetTransform != null)
        {
            targetTransform.localScale = hoverScale;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetTransform != null)
        {
            targetTransform.localScale = _originalScale;
        }
    }
}