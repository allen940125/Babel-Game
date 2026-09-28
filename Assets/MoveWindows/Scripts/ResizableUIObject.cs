using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEditor;

public enum ResizableUIDraggableIds
{
    CornerTopLeft,
    CornerTopRight,
    CornerBottomLeft,
    CornerBottomRight
}

public enum DraggableDirections
{
    Both,
    Vertical,
    Horizontal,
    None
}

public enum ElementType
{
    Text,
    TextMeshProGUI,
    Image
}

public class ResizableUIObject : MonoBehaviour
{
#pragma warning disable 0649
    [Header("Resiza Settings")]
    [SerializeField] private DraggableDirections _draggableDirection;

    [SerializeField] private bool _minSize;
    [SerializeField] private float _minSizeX;
    [SerializeField] private float _minSizeY;

    [SerializeField] private bool _lockCorners;
    [SerializeField] private bool _lockCornerTopLeft;
    [SerializeField] private bool _lockCornerTopRight;
    [SerializeField] private bool _lockCornerBottomLeft;
    [SerializeField] private bool _lockCornerBottomRight;
#pragma warning disable 0649

    private RectTransform _rectTransform;
    public bool IsSelected { get; private set; }
    public DraggableDirections DraggableDirection { get { return _draggableDirection; } }

    private void Awake()
    {
        _rectTransform = transform.parent.GetComponent<RectTransform>();
        if (_rectTransform == null)
        {
            Debug.Log(string.Format("ResizableUIObject {0} unable to locate parent RectTransform", gameObject.name));
        }
    }

    private void Start()
    {
        if (ResizableUI.Instance == null)
        {
            Debug.Log("Add the ResizableUI GameObject to the Scene");
        }
        else
        {
            ResizableUI.Instance.AddObject(this);
        }
    }

    public void Select(bool select)
    {
        if (IsSelected != select)
        {
            IsSelected = select;

            for (int x = 0; x < transform.childCount; x++)
            {
                transform.GetChild(x).gameObject.SetActive(IsSelected);
            }
        }
    }

    public void UpdateSize(ResizableUIDraggable draggable, Vector2 delta)
    {
        if (delta == Vector2.zero) 
        {
            Debug.LogWarning("[ResizableUIObject] 收到 Delta 為 0 的無效更新。");
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
        delta /= scaleFactor;

        switch (DraggableDirection)
        {
            case DraggableDirections.Horizontal: delta.y = 0f; break;
            case DraggableDirections.Vertical: delta.x = 0f; break;
            case DraggableDirections.None: return;
        }

        Vector2 offsetMin = _rectTransform.offsetMin;
        Vector2 offsetMax = _rectTransform.offsetMax;
        float newX, newY;

        switch (draggable.DraggableID)
        {
            case ResizableUIDraggableIds.CornerTopLeft:
                if (!_lockCornerTopLeft)
                {
                    newX = Mathf.Min(offsetMin.x + delta.x, offsetMax.x - _minSizeX);
                    newY = Mathf.Max(offsetMax.y + delta.y, offsetMin.y + _minSizeY);
                    _rectTransform.offsetMin = new Vector2(newX, offsetMin.y);
                    _rectTransform.offsetMax = new Vector2(offsetMax.x, newY);
                }
                break;
            case ResizableUIDraggableIds.CornerTopRight:
                if (!_lockCornerTopRight)
                {
                    newX = Mathf.Max(offsetMax.x + delta.x, offsetMin.x + _minSizeX);
                    newY = Mathf.Max(offsetMax.y + delta.y, offsetMin.y + _minSizeY);
                    _rectTransform.offsetMax = new Vector2(newX, newY);
                }
                break;
            case ResizableUIDraggableIds.CornerBottomLeft:
                if (!_lockCornerBottomLeft)
                {
                    newX = Mathf.Min(offsetMin.x + delta.x, offsetMax.x - _minSizeX);
                    newY = Mathf.Min(offsetMin.y + delta.y, offsetMax.y - _minSizeY);
                    _rectTransform.offsetMin = new Vector2(newX, newY);
                }
                break;
            case ResizableUIDraggableIds.CornerBottomRight:
                if (!_lockCornerBottomRight)
                {
                    newX = Mathf.Max(offsetMax.x + delta.x, offsetMin.x + _minSizeX);
                    newY = Mathf.Min(offsetMin.y + delta.y, offsetMax.y - _minSizeY);
                    _rectTransform.offsetMax = new Vector2(newX, offsetMax.y);
                    _rectTransform.offsetMin = new Vector2(offsetMin.x, newY);
                }
                break;
        }

        Debug.Log($"[ResizableUIObject] 📐 縮放執行完畢 | 節點: {draggable.DraggableID} | 修改前 Min/Max: {offsetMin}/{offsetMax} | 修改後 Min/Max: {_rectTransform.offsetMin}/{_rectTransform.offsetMax}");
    }
    
    [ContextMenu("Debug: 嚴格檢查節點與排版結構")]
    public void CheckSetup()
    {
        Debug.Log($"--- 開始嚴格檢查 {gameObject.name} 節點結構 ---");

        // 1. 父節點與排版引擎衝突檢查
        if (transform.parent == null)
        {
            Debug.LogError("致命錯誤：沒有父節點。");
            return;
        }

        RectTransform parentRect = transform.parent.GetComponent<RectTransform>();
        if (parentRect == null)
        {
            Debug.LogError("致命錯誤：父節點缺少 RectTransform。");
        }
        else
        {
            // 檢查是否被 Layout Group 綁架 (這是無法縮放的最常見原因)
            if (transform.parent.GetComponent<LayoutGroup>() != null)
                Debug.LogError("致命錯誤：父節點掛載了 LayoutGroup。Unity 排版引擎會強制覆寫你的縮放數值，必須移除。");
            
            if (transform.parent.GetComponent<ContentSizeFitter>() != null)
                Debug.LogError("致命錯誤：父節點掛載了 ContentSizeFitter。這會導致腳本修改的尺寸立刻被重置，必須移除。");
            
            Debug.Log("父節點佈局依賴檢查完成。");
        }

        // 2. 自身點擊攔截檢查
        Graphic graphic = GetComponent<Graphic>();
        if (graphic == null || !graphic.raycastTarget)
        {
            Debug.LogError("致命錯誤：此物件缺少 Graphic 元件或未勾選 Raycast Target，將無法被點擊選取。");
        }

        // 3. 拖曳器實體與事件接收能力檢查
        ResizableUIDraggable[] draggables = GetComponentsInChildren<ResizableUIDraggable>(true);
        if (draggables.Length < 4)
        {
            Debug.LogError($"致命錯誤：拖曳節點不足 4 個 (目前 {draggables.Length} 個)。");
        }
        else
        {
            foreach (var drag in draggables)
            {
                Graphic dragGraphic = drag.GetComponent<Graphic>();
                if (dragGraphic == null)
                {
                    Debug.LogError($"致命錯誤：拖曳節點 {drag.gameObject.name} 缺少 Graphic 元件(如 Image)。EventSystem 無法對空物件派發 OnDrag 事件。");
                }
                else if (!dragGraphic.raycastTarget)
                {
                    Debug.LogError($"致命錯誤：拖曳節點 {drag.gameObject.name} 的 Raycast Target 未勾選。游標拖曳將直接穿透，不會觸發縮放。");
                }
            }
            Debug.Log("拖曳器事件接收實體檢查完成。");
        }
        
        foreach (var drag in draggables)
        {
            if (drag == null)
                continue;

            Graphic dragGraphic = drag.GetComponent<Graphic>();

            if (dragGraphic == null)
            {
                Debug.LogError(
                    $"❌ {drag.gameObject.name} 沒有 Graphic"
                );
            }
            else if (!dragGraphic.raycastTarget)
            {
                Debug.LogError(
                    $"❌ {drag.gameObject.name} Raycast Target 沒開"
                );
            }

            // 檢查 ResizableUIObject 綁定
            var serializedObject = new UnityEditor.SerializedObject(drag);
            var elementProperty =
                serializedObject.FindProperty("_resizableElement");

            if (elementProperty == null ||
                elementProperty.objectReferenceValue == null)
            {
                Debug.LogError(
                    $"❌ {drag.gameObject.name} 沒有指定 ResizableUIObject！"
                );
            }
            else
            {
                Debug.Log(
                    $"✅ {drag.gameObject.name} → " +
                    $"{elementProperty.objectReferenceValue.name}"
                );
            }
        }
        
        Debug.Log("--- 檢查結束 ---");
    }
}
