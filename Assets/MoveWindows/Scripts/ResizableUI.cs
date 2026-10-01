using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ResizableUI : MonoBehaviour
{
    public static ResizableUI Instance { get; private set; }
    
    [SerializeField]
    private EventSystem _eventSystem;
    [SerializeField] private GraphicRaycaster _curChooseGraphicRaycaster;
    public GraphicRaycaster curChoose_graphicRaycaster
    {
        get => _curChooseGraphicRaycaster;
        private set
        {
            _curChooseGraphicRaycaster = value;

            if (value != null)
            {
                Canvas canvas = value.GetComponent<Canvas>();

                Debug.Log(
                    $"🎯 curChoose_graphicRaycaster = {value.gameObject.name}\n" +
                    $"   Canvas = {(canvas != null ? canvas.name : "NULL")}\n" +
                    $"   RenderMode = {(canvas != null ? canvas.renderMode.ToString() : "NULL")}"
                );
            }
            else
            {
                Debug.Log("🎯 curChoose_graphicRaycaster = NULL");
            }
        }
    }
    private List<ResizableUIObject> _resizableObjects { get; set; } = new List<ResizableUIObject>();
    private ResizableUIObject _selectedResizableUIObject = null;

    private void Awake()
    {
        Instance = this;

        _eventSystem = EventSystem.current;
        if (_eventSystem == null)
            _eventSystem = FindObjectOfType<EventSystem>();

        if (_eventSystem == null)
            Debug.LogError("❌ EventSystem not found");
    }
    
    private void Update()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (_resizableObjects.Count <= 0)
            return;

        if (EventSystem.current == null)
            return;

        PointerEventData data = new PointerEventData(EventSystem.current);
        data.position = Mouse.current.position.ReadValue();

        List<RaycastResult> results = new List<RaycastResult>();

        EventSystem.current.RaycastAll(data, results);

        if (results.Count > 0)
        {
            // 找出第一個真正的 GraphicRaycaster
            _curChooseGraphicRaycaster = null;

            foreach (RaycastResult result in results)
            {
                GraphicRaycaster raycaster =
                    result.module as GraphicRaycaster;

                if (raycaster != null)
                {
                    _curChooseGraphicRaycaster = raycaster;
                    break;
                }
            }

            if (_curChooseGraphicRaycaster != null)
            {
                Canvas canvas =
                    _curChooseGraphicRaycaster.GetComponent<Canvas>();

                Debug.Log(
                    $"🎯 curChoose_graphicRaycaster：" +
                    $"{_curChooseGraphicRaycaster.gameObject.name} | " +
                    $"Canvas：{(canvas != null ? canvas.name : "NULL")} | " +
                    $"RenderMode：{(canvas != null ? canvas.renderMode.ToString() : "NULL")}"
                );
            }

            for (int x = 0; x < results.Count; x++)
            {
                Debug.Log(
                    $"[ResizableUI] Raycast {x}: " +
                    $"{results[x].gameObject.name} | " +
                    $"Module: {results[x].module.GetType().Name}"
                );

                ResizableUIObject resizableUIObject = results[x].gameObject.GetComponentInParent<ResizableUIObject>();

                if (resizableUIObject != null)
                {
                    if (resizableUIObject != _selectedResizableUIObject)
                    {
                        ChangeSelectedObject(resizableUIObject);
                    }

                    break;
                }
            }
        }
    }

    private void ChangeSelectedObject(ResizableUIObject selectedResizableUIObject)
    {
        for (int x = 0; x < _resizableObjects.Count; x++)
        {
            _resizableObjects[x].Select(_resizableObjects[x] == selectedResizableUIObject);
        }
        _selectedResizableUIObject = selectedResizableUIObject;
    }

    public void AddObject(ResizableUIObject resizableUIObject)
    {
        if (!_resizableObjects.Contains(resizableUIObject))
        {
            _resizableObjects.Add(resizableUIObject);
        }
    }
    
   [ContextMenu("🔍 一鍵完整檢查 Resizable UI")]
    public void CheckEnvironment()
    {
        Debug.Log("========== Resizable UI 完整檢查開始 ==========");

        bool allPassed = true;

        // =========================================================
        // 1. EventSystem
        // =========================================================

        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
        {
            eventSystem = FindObjectOfType<EventSystem>();
        }

        if (eventSystem == null)
        {
            Debug.LogError("❌ EventSystem 不存在！");
            allPassed = false;
        }
        else
        {
            Debug.Log($"✅ EventSystem：{eventSystem.gameObject.name}");

            var inputModule =
                eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            if (inputModule == null)
            {
                Debug.LogError(
                    "❌ EventSystem 沒有 InputSystemUIInputModule！"
                );
                allPassed = false;
            }
            else if (!inputModule.enabled)
            {
                Debug.LogError(
                    "❌ InputSystemUIInputModule 存在，但沒有啟用！"
                );
                allPassed = false;
            }
            else
            {
                Debug.Log("✅ InputSystemUIInputModule 正常");
            }
        }


        // =========================================================
        // 2. 搜尋所有 Canvas
        // =========================================================

        Canvas[] canvases = FindObjectsOfType<Canvas>(true);

        Debug.Log($"📋 場景 Canvas 數量：{canvases.Length}");

        if (canvases.Length == 0)
        {
            Debug.LogError("❌ 場景完全沒有 Canvas！");
            allPassed = false;
        }


        // =========================================================
        // 3. 搜尋所有 GraphicRaycaster
        // =========================================================

        GraphicRaycaster[] raycasters =
            FindObjectsOfType<GraphicRaycaster>(true);

        Debug.Log($"📋 場景 GraphicRaycaster 數量：{raycasters.Length}");

        if (raycasters.Length == 0)
        {
            Debug.LogError("❌ 場景完全沒有 GraphicRaycaster！");
            allPassed = false;
        }

        foreach (GraphicRaycaster raycaster in raycasters)
        {
            Canvas canvas = raycaster.GetComponent<Canvas>();

            Debug.Log(
                $"🔎 Raycaster：{raycaster.gameObject.name} | " +
                $"Canvas：{(canvas != null ? canvas.name : "NULL")} | " +
                $"Enabled：{raycaster.enabled}"
            );

            if (!raycaster.enabled)
            {
                Debug.LogError(
                    $"❌ GraphicRaycaster {raycaster.gameObject.name} 沒有啟用！"
                );

                allPassed = false;
            }
        }


        // =========================================================
        // 4. 檢查所有 ResizableUIObject
        // =========================================================

        ResizableUIObject[] objects =
            FindObjectsOfType<ResizableUIObject>(true);

        Debug.Log($"📋 ResizableUIObject 數量：{objects.Length}");

        if (objects.Length == 0)
        {
            Debug.LogError("❌ 場景中沒有 ResizableUIObject！");
            allPassed = false;
        }

        foreach (ResizableUIObject obj in objects)
        {
            Debug.Log($"🔎 ResizableUIObject：{obj.gameObject.name}");

            obj.CheckSetup();
        }


        // =========================================================
        // 5. 檢查每個 ResizableUIObject 的拖曳角
        // =========================================================

        foreach (ResizableUIObject obj in objects)
        {
            ResizableUIDraggable[] draggables =
                obj.GetComponentsInChildren<ResizableUIDraggable>(true);

            Debug.Log(
                $"🔎 {obj.gameObject.name} 擁有 {draggables.Length} 個 Resize Handle"
            );

            foreach (ResizableUIDraggable draggable in draggables)
            {
                Graphic graphic =
                    draggable.GetComponent<Graphic>();

                if (graphic == null)
                {
                    Debug.LogError(
                        $"❌ {draggable.gameObject.name} 沒有 Image / Graphic！"
                    );

                    allPassed = false;
                    continue;
                }

                if (!graphic.raycastTarget)
                {
                    Debug.LogError(
                        $"❌ {draggable.gameObject.name} Raycast Target 沒開！"
                    );

                    allPassed = false;
                }
                else
                {
                    Debug.Log(
                        $"✅ Resize Handle：{draggable.gameObject.name} Raycast 正常"
                    );
                }
            }
        }


        // =========================================================
        // 6. 檢查 ResizableUI 自己目前抓到哪個 Raycaster
        // =========================================================

        if (curChoose_graphicRaycaster == null)
        {
            Debug.LogError(
                "❌ ResizableUI 目前沒有 GraphicRaycaster！"
            );
        
            allPassed = false;
        }
        else
        {
            Canvas canvas =
                curChoose_graphicRaycaster.GetComponent<Canvas>();
        
            Debug.Log(
                $"🎯 ResizableUI 目前使用的 Raycaster：" +
                $"{curChoose_graphicRaycaster.gameObject.name}"
            );
        
            if (canvas != null)
            {
                Debug.Log(
                    $"🎯 所屬 Canvas：{canvas.name}"
                );
        
                Debug.Log(
                    $"🎯 Canvas Render Mode：{canvas.renderMode}"
                );
            }
        }


        // =========================================================
        // 7. 最終結果
        // =========================================================

        if (allPassed)
        {
            Debug.Log(
                "========== ✅ 基本環境檢查全部通過 =========="
            );
        }
        else
        {
            Debug.Log(
                "========== ❌ 發現問題，請看上面的紅色 Log =========="
            );
        }
    }
}
