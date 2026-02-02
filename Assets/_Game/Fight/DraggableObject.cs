using Gamemanager;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class DraggableObject : MonoBehaviour
{
    [Header("基礎設定")]
    public bool isDraggable = true;
    public float dragThreshold = 0.1f; 

    protected bool _isDragging = false;
    protected Vector3 _offset;
    protected Camera _mainCamera;
    protected Collider2D _myCollider;
    
    // --- 新增：發光腳本參照 ---
    protected SimpleSpriteGlow _glowEffect;

    protected Vector3 _startDragPosition;

    protected string _originalTag; 
    protected virtual void Awake()
    {
        _originalTag = gameObject.tag; // 記住我是 PlayerButton
        
        if (Camera.main != null) _mainCamera = Camera.main;
        else _mainCamera = Object.FindFirstObjectByType<Camera>();
        
        _myCollider = GetComponent<Collider2D>();
        
        // --- 新增：自動抓取發光腳本 ---
        _glowEffect = GetComponent<SimpleSpriteGlow>();
        
        // 註冊事件
        if(GameManager.Instance != null && GameManager.Instance.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterVulnerablePhaseEvent, OnLock);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterIdlePhaseEvent, UnLock);
        }
    }

    // 建議在 Start 初始化一次狀態，確保遊戲開始時發光狀態正確
    protected virtual void Start()
    {
        UpdateGlowState();
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
                OnDragEnd(); 
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
        gameObject.tag = _originalTag;
        UpdateGlowState(); // 更新發光
    }

    public void UnLock(BossEnterIdlePhaseEvent cmd)
    {
        isDraggable = true; 
        gameObject.tag = "Untagged";
        UpdateGlowState(); // 更新發光
    }
    
    // --- 新增：統一控制發光的方法 ---
    protected void UpdateGlowState()
    {
        if (_glowEffect != null)
        {
            // 如果可以拖曳(isDraggable = true) -> 開啟發光 (isGlowing = true)
            // 如果鎖定中(isDraggable = false) -> 關閉發光 (isGlowing = false)
            _glowEffect.isGlowing = isDraggable;
        }
    }

    // --- 虛擬方法 ---

    protected virtual void OnDragStart(Vector2 mousePos)
    {
        _isDragging = true;
        _offset = transform.position - (Vector3)mousePos;
        _startDragPosition = transform.position;
        
        // (選用) 拖曳時如果想讓它變更亮，可以在這裡改 _glowEffect 的參數
        // 例如：_glowEffect.scaleMultiplier = 1.3f;
        gameObject.tag = "Untagged";
    }

    protected virtual void OnDragEnd()
    {
        _isDragging = false;
        
        // --- ★ 關鍵 2：拖曳結束，把 Tag 改回來 ---
        gameObject.tag = _originalTag; 

        // --- ★ 關鍵 3：主動偵測腳底下有沒有機關 ---
        CheckDropCollision();

        float distance = Vector3.Distance(transform.position, _startDragPosition);

        if (distance < dragThreshold)
        {
            OnClicked();
        }
        else
        {
            OnRepositioned();
        }
    }

    // --- ★ 新增：放開時的手動偵測邏輯 ---
    protected void CheckDropCollision()
    {
        // 在物體中心點做一個小範圍的物理偵測
        // OverlapPointAll 會回傳所有重疊的 Collider
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);

        foreach (var hit in hits)
        {
            // 忽略自己
            if (hit == _myCollider) continue;

            // 試著抓取對方身上的 BossSpecialMechanism
            // (包含 BossCleanerMechanism 或 BossCelesteMechanism)
            BossSpecialMechanism mechanism = hit.GetComponent<BossSpecialMechanism>();
            
            // 如果沒抓到，有可能是撞到子物件，往父物件找找看
            if (mechanism == null)
                mechanism = hit.GetComponentInParent<BossSpecialMechanism>();

            // 如果找到了機關
            if (mechanism != null)
            {
                Debug.Log($"放開時偵測到機關：{mechanism.name}");
                // 手動呼叫機關的觸發方法，把自己傳進去驗證 Tag
                mechanism.ManualTrigger(this.gameObject);
            }
        }
    }
    
    protected virtual void OnDragging()
    {
        Vector3 targetPos = GetMouseWorldPos() + (Vector2)_offset;
        transform.position = new Vector3(targetPos.x, targetPos.y, 0);
    }
    
    protected virtual void OnClicked() { } 
    protected virtual void OnRepositioned() { }

    protected Vector2 GetMouseWorldPos()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        return _mainCamera.ScreenToWorldPoint(screenPos);
    }
}