using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionController3D : MonoBehaviour
{
    [Header("基礎設定")]
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private float rotationSpeed = 0.5f;

    [Header("視覺化偵錯工具 (Gizmos)")]
    [SerializeField] private bool showDebugRay = true;
    [SerializeField] private bool showDragPlane = true;
    [SerializeField] private float planeVisualSize = 5f;

    // 即時狀態監控 (在 Inspector 中設定為唯讀觀察)
    [Header("即時狀態監控 (唯讀)")]
    [SerializeField] private string currentTargetName = "None";
    [SerializeField] private Vector3 lastHitPoint;
    [SerializeField] private bool isDraggingMonitor;

    private Camera _mainCam;
    private IDragHandler3D _currentDraggable;
    private IRotateHandler3D _currentRotatable;
    private IPointerClickHandler _currentClickable;
    
    private Plane _dragPlane;
    private bool _isDragging = false;
    private bool _isRotating = false;
    private Vector2 _mouseDownPos;

    // 儲存最後一次的射線與命中狀態供 Gizmos 繪製
    private Ray _debugRay;
    private bool _debugHitSuccess;

    private void Awake()
    {
        _mainCam = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        // 隨時更新偵錯射線
        _debugRay = _mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
        _debugHitSuccess = Physics.Raycast(_debugRay, out RaycastHit hit, 100f, interactableLayer);
        
        if (_debugHitSuccess)
        {
            lastHitPoint = hit.point;
            currentTargetName = hit.collider.gameObject.name;
        }
        else if (!_isDragging)
        {
            currentTargetName = "None";
        }

        isDraggingMonitor = _isDragging;

        HandleLeftClickAndDrag();
        HandleRightClickRotate();
    }

    private void HandleLeftClickAndDrag()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            _mouseDownPos = Mouse.current.position.ReadValue();
            if (_debugHitSuccess)
            {
                // 重新發射射線取得 Collider
                Physics.Raycast(_debugRay, out RaycastHit hit, 100f, interactableLayer);
                _currentDraggable = hit.collider.GetComponent<IDragHandler3D>();
                _currentClickable = hit.collider.GetComponent<IPointerClickHandler>();

                if (_currentDraggable != null)
                {
                    _isDragging = true;
                    // 以攝影機正前方為法線建立平面，避免視角垂直問題
                    _dragPlane = new Plane(-_mainCam.transform.forward, hit.point);
                    _currentDraggable.OnDragStart(hit.point);
                }
                else
                {
                    Debug.LogWarning($"[偵錯] 射線擊中 {hit.collider.name}，但該物件缺少 IDragHandler3D 組件！");
                }
            }
            else
            {
                Debug.Log($"[偵錯] 左鍵點擊落空。請檢查：1. LayerMask ({interactableLayer.value}) 是否正確 2. 物件是否有 3D Collider。");
            }
        }

        if (_isDragging && Mouse.current.leftButton.isPressed)
        {
            if (_dragPlane.Raycast(_debugRay, out float enter))
            {
                Vector3 hitPoint = _debugRay.GetPoint(enter);
                lastHitPoint = hitPoint;
                _currentDraggable.OnDrag(hitPoint);
            }
            else
            {
                Debug.LogError("[偵錯] 射線與拖曳平面無法相交！請檢查攝影機視角與平面法線。");
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (_isDragging && _currentDraggable != null)
            {
                _currentDraggable.OnDragEnd();
                if (Vector2.Distance(_mouseDownPos, Mouse.current.position.ReadValue()) < 5f)
                {
                    _currentClickable?.OnClick();
                }
                _isDragging = false;
                _currentDraggable = null;
                _currentClickable = null;
            }
        }
    }

    private void HandleRightClickRotate()
    {
        // 在 HandleRightClickRotate() 中修改：
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (_debugHitSuccess)
            {
                Physics.Raycast(_debugRay, out RaycastHit hit, 100f, interactableLayer);
                _currentRotatable = hit.collider.GetComponent<IRotateHandler3D>();
                if (_currentRotatable != null)
                {
                    _isRotating = true;
                    // 直接透過介面呼叫，絕對不使用 as 進行實體轉型！
                    _currentRotatable.OnRotateStart(hit.point);
                }
            }
        }

        if (_isRotating && Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            // 傳入當前的攝影機射線與螢幕位移，供組件內部選擇具體的旋轉數學演算法
            _currentRotatable.OnRotate(_debugRay, mouseDelta);
        }

        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            _isRotating = false;
            _currentRotatable = null;
        }
    }

    // --- Scene View 視覺化繪製 ---
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // 1. 繪製滑鼠射線
        if (showDebugRay)
        {
            Gizmos.color = _debugHitSuccess ? Color.green : Color.red;
            Gizmos.DrawRay(_debugRay.origin, _debugRay.direction * 100f);

            if (_debugHitSuccess)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(lastHitPoint, 0.2f);
            }
        }

        // 2. 繪製 3D 拖曳投影平面
        if (showDragPlane && _isDragging)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            // 利用矩陣將 Gizmo 旋轉並平移到投影面的位置與方向
            Gizmos.matrix = Matrix4x4.TRS(lastHitPoint, Quaternion.LookRotation(_dragPlane.normal), Vector3.one);
            Gizmos.DrawCube(Vector3.zero, new Vector3(planeVisualSize, planeVisualSize, 0.01f));
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(planeVisualSize, planeVisualSize, 0.01f));
            // 畫出平面法線
            Gizmos.DrawRay(Vector3.zero, Vector3.forward * 2f);
        }
    }
}


// --- 3D 點擊行為合約 ---
public interface IPointerClickHandler
{
    void OnClick();
}

// --- 3D 拖曳行為合約 ---
public interface IDragHandler3D
{
    void OnDragStart(Vector3 hitPoint);
    void OnDrag(Vector3 targetWorldPosition);
    void OnDragEnd();
}

// --- 3D 旋轉行為合約 ---
public interface IRotateHandler3D
{
    void OnRotateStart(Vector3 hitPoint);
    void OnRotate(Ray cameraRay, Vector2 screenDelta);
}