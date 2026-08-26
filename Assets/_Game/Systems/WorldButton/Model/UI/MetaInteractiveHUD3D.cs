using UnityEngine;
// 移除 UnityEngine.Serialization，因為不需要 FormerlySerializedAs 了

public class MetaInteractiveHUD3D : InteractiveMetaEntity3D
{
    public enum DisplayMode { Health, Stamina }
    
    // ★ 新增：定義這個 UI 該去哪裡找資料？
    public enum BindingMode 
    { 
        LocalParent,    // 找父物件 (適用於：怪物頭頂血條)
        GlobalPlayer,   // 找中繼站的玩家 (適用於：場景上的補血站面板、固定 HUD)
        GlobalBoss,
        ManualInject    // 手動注入 (適用於：Boss 血條，由 Boss 狀態機手動塞進來)
    }

    [Header("資料綁定設定")]
    [SerializeField] private BindingMode bindingMode = BindingMode.LocalParent;
    [SerializeField] private DisplayMode displayMode = DisplayMode.Health;

    // ★ 拔除 SerializeField！改為私有變數，完全透過程式邏輯動態綁定
    private EntityRuntime _target;

    [Header("儀表板屬性")]
    [SerializeField] private SpriteRenderer fillSpriteRenderer;
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private Color fullColor = Color.green;
    [SerializeField] private Color emptyColor = Color.red;

    private float _targetRatio = 1.0f;
    private float _currentRatio = 1.0f;
    private float _cachedInitialScaleX;
    
    private StaminaTrait _cachedStaminaTrait;

    protected override void Awake()
    {
        base.Awake(); 
        if (fillSpriteRenderer != null) _cachedInitialScaleX = fillSpriteRenderer.transform.localScale.x;
    }

    // ★ 把原本的 OnEnable 拔掉，因為 OnEnable 執行時可能資料還沒準備好。
    // 改在 Start 階段執行自動尋找邏輯
    private void Start()
    {
        if (bindingMode == BindingMode.LocalParent)
        {
            // 往上找大腦
            var core = GetComponentInParent<EntityCore>();
            if (core != null) BindTarget(core.RuntimeData);
            else Debug.LogError($"[錯誤] {gameObject.name} 找不到父物件的 EntityCore！");
        }
        else if (bindingMode == BindingMode.GlobalPlayer)
        {
            // 透過 Mediator 找玩家 (請確保你的 Mediator 有實作這個屬性)
            var playerRuntime = GameManager.Instance.MainGameMediator.CurrentPlayerRuntime;
            if (playerRuntime != null) BindTarget(playerRuntime);
            else Debug.LogError($"[錯誤] {gameObject.name} 找不到全域玩家資料！");
        }
        else if (bindingMode == BindingMode.GlobalBoss)
        {
            var bossRuntime = GameManager.Instance.MainGameMediator.CurrentBossRuntime;
            if (bossRuntime != null) BindTarget(bossRuntime);
            else Debug.LogError($"[錯誤] {gameObject.name} 找不到全域Boss資料！");
            // 如果 Start 時 Boss 還沒開戰 (bossRuntime 是 null)，
            // 你可以讓這個 UI 訂閱剛才提到的 ShowBossUIEvent，等事件發生時再執行 BindTarget。
        }
        // 如果是 ManualInject，就什麼都不做，等別人來呼叫 BindTarget
    }

    private void OnDisable()
    {
        // 確保關閉時解除訂閱
        UnbindTarget();
    }

    // ==========================================
    // ★ 核心方法：開放外部注入與事件綁定
    // ==========================================
    public void BindTarget(EntityRuntime newTarget)
    {
        // 1. 先解綁舊的，避免重複訂閱或記憶體洩漏
        UnbindTarget();

        _target = newTarget;
        if (_target == null) return;

        // 2. 依照顯示模式訂閱對應事件
        if (displayMode == DisplayMode.Health)
        {
            _target.OnHealthRatioChanged += UpdateTargetRatio;
            UpdateTargetRatio(_target.MaxHealth > 0 ? (float)_target.CurrentHealth / _target.MaxHealth : 0f);
        }
        else if (displayMode == DisplayMode.Stamina)
        {
            if (_target.TryGetTrait(out _cachedStaminaTrait))
            {
                _cachedStaminaTrait.OnStaminaRatioChanged += UpdateTargetRatio;
                UpdateTargetRatio(_cachedStaminaTrait.StaminaRatio);
            }
        }
        
        // 瞬間把 UI 填滿到正確位置，不跑動畫
        _currentRatio = _targetRatio;
    }

    private void UnbindTarget()
    {
        if (_target == null) return;

        if (displayMode == DisplayMode.Health)
        {
            _target.OnHealthRatioChanged -= UpdateTargetRatio;
        }
        else if (displayMode == DisplayMode.Stamina && _cachedStaminaTrait != null)
        {
            _cachedStaminaTrait.OnStaminaRatioChanged -= UpdateTargetRatio;
            _cachedStaminaTrait = null;
        }
        _target = null;
    }

    private void UpdateTargetRatio(float newRatio)
    {
        _targetRatio = newRatio;
    }

    private void Update()
    {
        if (fillSpriteRenderer == null) return;

        _currentRatio = Mathf.Lerp(_currentRatio, _targetRatio, Time.deltaTime * smoothSpeed);

        Vector3 scale = fillSpriteRenderer.transform.localScale;
        scale.x = _currentRatio * _cachedInitialScaleX;
        fillSpriteRenderer.transform.localScale = scale;

        fillSpriteRenderer.color = Color.Lerp(emptyColor, fullColor, _currentRatio);
    }
}