using Gamemanager;
using UnityEngine;

// ★ 所有繼承此底層的 Meta 物件，都會自動具備第四面牆的物理與互動能力！
[RequireComponent(typeof(BoxCollider))]
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

        // ★ 父類別自動訂閱戰鬥階段事件！
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterAttackingPhaseEvent, OnBossAttacking);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterIdlePhaseEvent, OnBossIdle);
        }
    }

    protected virtual void OnDestroy()
    {
        // ★ 嚴格記憶體釋放合約
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterAttackingPhaseEvent>(OnBossAttacking);
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterIdlePhaseEvent>(OnBossIdle);
        }
    }

    /// <summary>
    /// 自動建構物理規範與厚度防護
    /// </summary>
    private void SetupComponentsAutomagically()
    {
        _boxCollider = GetComponent<BoxCollider>();
        _draggable = GetComponent<DraggableBehavior3D>();
        _rotatable = GetComponent<RotatableBehavior3D>();
        _glow = GetComponent<GlowBehavior3D>();

        // 強制把 BoxCollider 的 Z 軸拉厚到 5.0f
        if (_boxCollider != null && _boxCollider.size.z < 1.0f)
        {
            Vector3 newSize = _boxCollider.size;
            newSize.z = 5.0f;
            _boxCollider.size = newSize;
            _boxCollider.isTrigger = false;
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

        if (canBlockBullets)
        {
            try { gameObject.tag = "Wall"; } 
            catch { Debug.LogWarning($"找不到 'Wall' Tag，無法將 {gameObject.name} 設為護盾！"); }
        }
        else
        {
            if (gameObject.CompareTag("Wall")) gameObject.tag = "Untagged";
        }
    }

    // 提供通用介面供外部或子類別動態修改狀態
    public void SetInteractable(bool draggable, bool rotatable)
    {
        canBeDragged = draggable;
        canBeRotated = rotatable;
        ApplyToggles();
    }

    // ==========================================
    // ★ 核心落實：階段切換自動仲裁
    // ==========================================
    private void OnBossAttacking(BossEnterAttackingPhaseEvent evt)
    {
        if (!ignorePhaseLock)
        {
            Debug.Log($"<color=orange>[掩體鎖定] {gameObject.name} 進入防禦態，鎖死拖曳旋轉！</color>");
            SetInteractable(false, false);
        }
    }

    private void OnBossIdle(BossEnterIdlePhaseEvent evt)
    {
        if (!ignorePhaseLock)
        {
            Debug.Log($"<color=cyan>[武器解鎖] {gameObject.name} 進入休眠態，開啟拖曳與攻擊權限！</color>");
            SetInteractable(true, true);
        }
    }
}