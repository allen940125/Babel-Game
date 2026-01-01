using Gamemanager;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class DraggableObject : MonoBehaviour
{
    [Header("基礎設定")]
    public bool isDraggable = true;
    public float dragThreshold = 0.1f; // 判定閾值：移動超過 0.1 單位就不算點擊

    protected bool _isDragging = false;
    protected Vector3 _offset;
    protected Camera _mainCamera;
    protected Collider2D _myCollider;
    
    // 新增：記錄開始拖曳時的位置
    protected Vector3 _startDragPosition;

    protected virtual void Awake()
    {
        if (Camera.main != null) _mainCamera = Camera.main;
        else _mainCamera = Object.FindFirstObjectByType<Camera>();
        
        _myCollider = GetComponent<Collider2D>();
        
        // 註冊事件
        if(GameManager.Instance != null && GameManager.Instance.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterVulnerablePhaseEvent, OnLock);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterIdlePhaseEvent, UnLock);
        }
    }
    
    protected virtual void OnDisable()
    {
        if(GameManager.Instance != null && GameManager.Instance.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterVulnerablePhaseEvent>(OnLock);
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterIdlePhaseEvent>(UnLock);
        }
    }

    protected virtual void Update()
    {
        if (!isDraggable) return;
        if (Mouse.current == null) return;

        // A. 按下
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = GetMouseWorldPos();
            if (_myCollider.OverlapPoint(mousePos))
            {
                OnDragStart(mousePos);
            }
        }

        // B. 放開
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (_isDragging)
            {
                OnDragEnd(); // 這裡現在包含了判斷邏輯
            }
        }

        // C. 拖曳中
        if (_isDragging)
        {
            OnDragging();
        }
    }

    // --- 事件處理 ---

    public void OnLock(BossEnterVulnerablePhaseEvent cmd)
    {
        isDraggable = false;
    }

    public void UnLock(BossEnterIdlePhaseEvent cmd)
    {
        // ★修正 Bug：解除鎖定時應該要是 true
        isDraggable = true; 
    }
    
    // --- 虛擬方法 ---

    protected virtual void OnDragStart(Vector2 mousePos)
    {
        _isDragging = true;
        _offset = transform.position - (Vector3)mousePos;
        
        // ★記錄起始位置
        _startDragPosition = transform.position;
    }

    protected virtual void OnDragEnd()
    {
        _isDragging = false;

        // ★核心邏輯：計算移動距離
        float distance = Vector3.Distance(transform.position, _startDragPosition);

        if (distance < dragThreshold)
        {
            // 移動很小 -> 視為「點擊確認」
            OnClicked();
        }
        else
        {
            // 移動很大 -> 視為「單純調整位置」
            OnRepositioned();
        }
    }

    protected virtual void OnDragging()
    {
        Vector3 targetPos = GetMouseWorldPos() + (Vector2)_offset;
        transform.position = new Vector3(targetPos.x, targetPos.y, 0);
    }

    // --- 新增這兩個讓子類別去覆寫 ---
    
    // 當玩家只是「點一下」沒移動時觸發
    protected virtual void OnClicked() { } 

    // 當玩家「拖曳移動後放開」時觸發
    protected virtual void OnRepositioned() { }

    protected Vector2 GetMouseWorldPos()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        return _mainCamera.ScreenToWorldPoint(screenPos);
    }
}