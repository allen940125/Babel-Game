using Gamemanager;
using UnityEngine;

// ★ 所有繼承此底層的 Meta 物件，都會自動具備第四面牆的物理與互動能力！
[RequireComponent(typeof(DraggableBehavior3D))]
[RequireComponent(typeof(RotatableBehavior3D))]
public abstract class InteractiveMetaEntity3D : MonoBehaviour
{
    [Header("★ 第四面牆通用功能開關")]
    [SerializeField] protected bool canBeDragged = true;
    [SerializeField] protected bool canBeRotated = true;
    [SerializeField] protected bool canBlockBullets = true;
    
    [Header("★ 戰鬥階段鎖定設定")]
    [Tooltip("如果為 true，Boss 攻擊時此物件不會被鎖死 (例如：暫停按鈕、設定視窗)")]
    [SerializeField] protected bool ignorePhaseLock = false;
    
    protected BoxCollider _boxCollider;
    protected DraggableBehavior3D _draggable;
    protected RotatableBehavior3D _rotatable;
    protected GlowBehavior3D _glow;

    // --- 編輯器生命週期 ---
    protected virtual void Reset() => SetupComponentsAutomagically();
    protected virtual void OnValidate() => ApplyToggles();

    protected virtual void Awake()
    {
        SetupComponentsAutomagically();
        ApplyToggles();

        if (GameManager.Instance?.MainGameEvent != null)
        {
            // ★ 新增：訂閱轉場事件
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterTransitionPhaseEvent, OnBossTransition);
            
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterAttackingPhaseEvent, OnBossAttacking);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterIdlePhaseEvent, OnBossIdle);
        }
    }

    protected virtual void OnDestroy()
    {
        if (GameManager.Instance?.MainGameEvent != null)
        {
            // ★ 新增：解除訂閱轉場事件
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterTransitionPhaseEvent>(OnBossTransition);
            
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterAttackingPhaseEvent>(OnBossAttacking);
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterIdlePhaseEvent>(OnBossIdle);
        }
    }

    /// <summary>
    /// 自動建構物理規範與厚度防護
    /// </summary>
    private void SetupComponentsAutomagically()
    {
        // ★ 核心修復：改用 GetComponentInChildren，它會先找自己，沒有的話會自動往下層子物件找
        _boxCollider = GetComponentInChildren<BoxCollider>();
        
        _draggable = GetComponent<DraggableBehavior3D>();
        _rotatable = GetComponent<RotatableBehavior3D>();
        _glow = GetComponent<GlowBehavior3D>();

        if (_boxCollider != null && _boxCollider.size.z < 1.0f)
        {
            Vector3 newSize = _boxCollider.size;
            newSize.z = 5.0f;
            _boxCollider.size = newSize;
            _boxCollider.isTrigger = false;
        }
        else if (_boxCollider == null)
        {
            // 防呆警告：萬一你子物件也忘記掛 Collider，要在這裡報錯
            Debug.LogError($"[架構錯誤] {gameObject.name} 及其子物件中都找不到 BoxCollider！");
        }
    }

    /// <summary>
    /// 即時套用功能開關
    /// </summary>
    protected virtual void ApplyToggles()
    {
        if (_draggable == null) _draggable = GetComponent<DraggableBehavior3D>();
        if (_rotatable == null) _rotatable = GetComponent<RotatableBehavior3D>();
        if (_glow == null) _glow = GetComponent<GlowBehavior3D>();

        if (_draggable != null) _draggable.enabled = canBeDragged;
        if (_rotatable != null) _rotatable.enabled = canBeRotated;
        if (_glow != null) _glow.enabled = (canBeDragged || canBeRotated);

        // if (canBlockBullets)
        // {
        //     try { gameObject.tag = "Wall"; } 
        //     catch { Debug.LogWarning($"找不到 'Wall' Tag，無法將 {gameObject.name} 設為護盾！"); }
        // }
        // else
        // {
        //     if (gameObject.CompareTag("Wall")) gameObject.tag = "Untagged";
        // }
    }

    // 提供通用介面供外部或子類別動態修改狀態
    public void SetInteractable(bool draggable, bool rotatable)
    {
        canBeDragged = draggable;
        canBeRotated = rotatable;
        ApplyToggles();
    }

    protected virtual void OnBossTransition(BossEnterTransitionPhaseEvent evt)
    {
        if (!ignorePhaseLock)
        {
            Debug.Log($"<color=orange>[權限沒收] {gameObject.name} 進入轉場，提前鎖死玩家操作，但允許被擊飛！</color>");
            
            // ★ 1. 關閉「禁用時鎖死物理」的保護機制
            if (_draggable != null)
            {
                _draggable.LockPhysicsWhenDisabled = false; 
            }
            
            // ★ 2. 沒收玩家滑鼠權限 (因為上面關了保護，所以剛體不會變成 Kinematic，依然可以被炸飛)
            SetInteractable(false, false);
        }
    }

    protected virtual void OnBossAttacking(BossEnterAttackingPhaseEvent evt)
    {
        if (!ignorePhaseLock)
        {
            Debug.Log($"<color=orange>[掩體鎖定] {gameObject.name} 進入防禦態，徹底鎖死！</color>");
            
            // ★ 1. 開啟保護機制
            if (_draggable != null)
            {
                _draggable.LockPhysicsWhenDisabled = true; 
            }
            
            // ★ 2. 雙重確認沒收權限，此時剛體會立刻變成 isKinematic = true
            SetInteractable(false, false);
        }
    }

    protected virtual void OnBossIdle(BossEnterIdlePhaseEvent evt)
    {
        if (!ignorePhaseLock)
        {
            Debug.Log($"<color=cyan>[武器解鎖] {gameObject.name} 進入休眠態，開啟權限！</color>");
            
            // 恢復玩家權限，拖曳腳本會自動接管物理狀態
            SetInteractable(true, true);
        }
    }
}