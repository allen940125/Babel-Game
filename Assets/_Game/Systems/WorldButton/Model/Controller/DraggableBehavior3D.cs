using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DraggableBehavior3D : MonoBehaviour, IDragHandler3D
{
    [Header("狀態設定")]
    [SerializeField] private bool isDraggable = true;
    [SerializeField] private float dropDetectRadius = 0.5f;

    [Header("★ 物理拖曳設定 (Physics Drag)")]
    [SerializeField] private bool usePhysicsCollision = true; // 開關：是否啟用物理防穿透
    [SerializeField] private float physicsDragSpeed = 20f;    // 拖曳跟隨的靈敏度
    [SerializeField] private float maxVelocity = 50f;         // 最大速度限制 (防止滑鼠甩太快導致穿透)

    [Tooltip("開啟時，只要沒有在拖曳，物件就會鎖死在原地，不會被其他物件撞飛。")]
    [SerializeField] private bool lockPhysicsWhenIdle = true; 
    
    // 供外部腳本呼叫的屬性，賦值時自動觸發物理更新
    public bool LockPhysicsWhenIdle
    {
        get => lockPhysicsWhenIdle;
        set
        {
            lockPhysicsWhenIdle = value;
            UpdatePhysicsLockState();
        }
    }

    [Tooltip("開啟時，當腳本被關閉或 isDraggable = false 時鎖死物理。")]
    [SerializeField] private bool lockPhysicsWhenDisabled = true;
    
    // 供外部腳本呼叫的屬性，賦值時自動觸發物理更新
    public bool LockPhysicsWhenDisabled
    {
        get => lockPhysicsWhenDisabled;
        set
        {
            lockPhysicsWhenDisabled = value;
            UpdatePhysicsLockState();
        }
    }
    
    [Header("偵錯專區 (Inspector 操作)")]
    [SerializeField] private bool showDetectionSphere = true;
    [SerializeField] private Color debugSphereColor = new Color(1f, 0.5f, 0f, 0.4f);
    
    private Vector3 _offset;
    
    // --- 物理拖曳需要的內部變數 ---
    private Rigidbody _rb;
    private bool _isDragging = false;
    private Vector3 _targetDragPosition;
    private bool _wasGravityOn;
    private bool _wasKinematic;
    private bool _defaultKinematic; // 記憶物件最原始的物理狀態
    
    // 新增一個變數來記憶原本的約束狀態
    private RigidbodyConstraints _originalConstraints;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            _defaultKinematic = _rb.isKinematic; // ★ 記住它在 Inspector 最初的狀態
        }
    }

    private void OnEnable()
    {
        isDraggable = true;
        UpdatePhysicsLockState(); // ★ 狀態更新
    }

    private void OnDisable()
    {
        isDraggable = false;
        UpdatePhysicsLockState(); // ★ 腳本被關閉時，立刻鎖死
    }
    
#if UNITY_EDITOR
    // 當你在 Unity Inspector 中修改任何數值時，會自動觸發此方法
    private void OnValidate()
    {
        // 確保遊戲在執行中，且剛體已獲取，則立刻更新物理狀態
        if (Application.isPlaying && _rb != null)
        {
            UpdatePhysicsLockState();
        }
    }
#endif

    public void OnDragStart(Vector3 hitPoint)
    {
        if (!this.enabled || !isDraggable)
        {
            Debug.LogWarning($"[權限攔截] {gameObject.name} 拒絕拖曳！");
            return;
        }
        
        _offset = transform.position - hitPoint;
        _isDragging = true;

        if (usePhysicsCollision && _rb != null)
        {
            _wasGravityOn = _rb.useGravity;
            _wasKinematic = _rb.isKinematic;
            _originalConstraints = _rb.constraints; // 記錄物件原本的旋轉/移動限制
            
            _rb.useGravity = false;
            _rb.isKinematic = false;
            
            // ★ 核心修復 1：拖曳期間強制「鎖死所有旋轉」，徹底杜絕撞牆時產生的物理力矩 (旋轉)
            //_rb.constraints = _originalConstraints | RigidbodyConstraints.FreezeRotation; 
            
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero; 
        }
    }

    public void OnDrag(Vector3 targetWorldPosition)
    {
        if (!this.enabled || !isDraggable) return;

        if (usePhysicsCollision && _rb != null)
        {
            // ★ 物理模式：絕對不能改 transform.position，而是更新目標座標給 FixedUpdate 處理
            _targetDragPosition = targetWorldPosition + _offset;
        }
        else
        {
            // 傳統模式：瞬間移動
            transform.position = targetWorldPosition + _offset;
        }
    }

    private void FixedUpdate()
    {
        if (_isDragging && usePhysicsCollision && _rb != null)
        {
            // 1. 計算目標方向與距離
            Vector3 direction = _targetDragPosition - _rb.position;
            float distance = direction.magnitude;

            // 2. 死區放大到 0.05f (過濾掉人手顫抖與浮點數誤差)
            if (distance > 0.05f) 
            {
                Vector3 targetVelocity = direction * physicsDragSpeed;
                targetVelocity = Vector3.ClampMagnitude(targetVelocity, maxVelocity);

                // ★ 核心減速控制：不要直接賦值，用 Lerp 讓當前速度「平滑過渡」到目標速度
                // 後面的 Time.fixedDeltaTime * 15f 決定了煞車的靈敏度 (數值越大越黏手，越小越滑)
                _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 15f);
            }
            else
            {
                // 3. 進入死區，絕對煞車
                _rb.linearVelocity = Vector3.zero;
                
                // (可選) 強制精準貼齊目標點，徹底消滅微動
                _rb.MovePosition(_targetDragPosition); 
            }
        }
    }

    public void OnDragEnd()
    {
        if (!this.enabled || !isDraggable) return;

        _isDragging = false;

        if (usePhysicsCollision && _rb != null)
        {
            _rb.useGravity = _wasGravityOn;
            // _rb.isKinematic = _wasKinematic; // ★ 這行可以刪除，交給下面的 UpdatePhysicsLockState 統一管理
            _rb.linearVelocity = Vector3.zero; 
            _rb.constraints = _originalConstraints; 
        }

        CheckDropCollision3D();
        
        UpdatePhysicsLockState(); // ★ 放開滑鼠時，重新校準一次物理鎖定狀態
    }
    
    private void CheckDropCollision3D()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, dropDetectRadius);
        Debug.Log($"[物理偵測] {gameObject.name} 放開，偵測範圍內共有 {hits.Length} 個 Collider。");

        foreach (var hit in hits)
        {
            if (hit.gameObject == this.gameObject) continue;

            BossSpecialMechanism mechanism = hit.GetComponentInParent<BossSpecialMechanism>();
            if (mechanism != null)
            {
                Debug.Log($"<color=green>[成功觸發]</color> 找到機關：{mechanism.name}，發送手動觸發訊號！");
                mechanism.ManualTrigger(this.gameObject);
                return;
            }
            else
            {
                Debug.Log($"[忽略目標] 抓到 {hit.name}，但其父子層級中沒有 BossSpecialMechanism 組件。");
            }
        }
        Debug.LogWarning($"[觸發落空] {gameObject.name} 範圍內沒有任何有效的 BossSpecialMechanism。");
    }

    private void SetDraggable(bool state)
    {
        isDraggable = state;
        UpdatePhysicsLockState(); // ★ 狀態更新
        Debug.Log($"[狀態變更] {gameObject.name} 的 isDraggable 被切換為: {state}");
    }
    
    // ==========================================
    // ★ 核心狀態同步控制器 (管理所有 Kinematic 狀態)
    // ==========================================
    private void UpdatePhysicsLockState()
    {
        if (_rb == null) return; 

        // 狀態 1：正在被玩家抓著拖曳中
        if (_isDragging)
        {
            // 必須是非 Kinematic，物理引擎才會幫忙算撞牆防穿透
            _rb.isKinematic = false;
            return; 
        }

        // --- 以下是沒有被抓著 (放開/閒置/禁用) 的判定 ---
        
        bool isDisabled = !this.enabled || !isDraggable;
        bool shouldLock = false;

        // 狀態 2：系統禁用鎖定
        if (isDisabled && lockPhysicsWhenDisabled)
        {
            shouldLock = true;
        }
        // 狀態 3：閒置狀態鎖定
        else if (!isDisabled && lockPhysicsWhenIdle)
        {
            shouldLock = true;
        }

        // 執行最終物理狀態變更
        _rb.isKinematic = shouldLock ? true : _defaultKinematic;
        
        // 只要被鎖死，立刻抹除所有殘留動能，達成完美靜止
        if (shouldLock)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }
    
    // --- 右鍵選單強制執行工具 ---
    [ContextMenu("偵錯：強制執行放下偵測 (Manual Check)")]
    public void DebugManualDropCheck()
    {
        Debug.Log("=== 執行 Inspector 強制放下偵測 ===");
        CheckDropCollision3D();
    }

    [ContextMenu("偵錯：切換拖曳權限 (Toggle Draggable)")]
    public void DebugToggleDraggable()
    {
        SetDraggable(!isDraggable);
    }

    // --- Scene View 視覺化 ---
    private void OnDrawGizmosSelected()
    {
        if (!showDetectionSphere) return;

        Gizmos.color = debugSphereColor;
        Gizmos.DrawSphere(transform.position, dropDetectRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, dropDetectRadius);
    }
}