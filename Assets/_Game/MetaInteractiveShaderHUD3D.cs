using Gamemanager;
using UnityEngine;
using System.Collections;

public class MetaInteractiveShaderHUD3D : MonoBehaviour
{
    public enum DisplayMode { Health, Stamina, Timer } 
    public enum BindingMode { LocalParent, GlobalPlayer, GlobalBoss, ManualInject }
    public enum VisualMode { Scale2D, Shader3D }

    [Header("核心綁定設定")]
    [SerializeField] private BindingMode bindingMode = BindingMode.LocalParent;
    [SerializeField] private DisplayMode displayMode = DisplayMode.Health;
    [SerializeField] private VisualMode visualMode = VisualMode.Scale2D;
    [SerializeField] private float smoothSpeed = 10f;

    [Header("2D 渲染設定")]
    [SerializeField] private Transform _fillPivot; 
    [SerializeField] private SpriteRenderer _fillSprite;
    [SerializeField] private Color fullColor = Color.green;
    [SerializeField] private Color emptyColor = Color.red;

    [Header("3D Shader 渲染設定")]
    [SerializeField] private Renderer targetRenderer;
    
    [Header("顯示設定")]
    [Tooltip("勾選後，數值會被翻轉 (1 - Ratio)。適用於：倒數計時器想要顯示成『逐漸充能』的進度條。")]
    [SerializeField] private bool invertFill = false;
    
    private Material _runtimeMaterial;
    private int _fillAmountPropertyID;
    private Vector3 _initialScale; 

    private EntityRuntime _target;
    private float _targetRatio = 1.0f;
    private float _currentRatio = 1.0f;
    
    private StaminaTrait _cachedStaminaTrait;
    private TimerTrait _cachedTimerTrait; 

    private void Awake()
    {
        if (_fillPivot != null) _initialScale = _fillPivot.localScale;

        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            _runtimeMaterial = targetRenderer.material; 
            _fillAmountPropertyID = Shader.PropertyToID("_FillAmount");
        }
    }

    private void Start()
    {
        if (bindingMode == BindingMode.LocalParent)
        {
            var core = GetComponentInParent<EntityCore>();
            if (core != null) 
                TryBindValidatedTarget(core.RuntimeData);
        }
        else if (bindingMode == BindingMode.GlobalPlayer)
        {
            var playerRuntime = GameManager.Instance.MainGameMediator.CurrentPlayerRuntime;
            if (playerRuntime != null) 
                TryBindValidatedTarget(playerRuntime);
        }
        else if (bindingMode == BindingMode.GlobalBoss)
        {
            var existingBoss = GameManager.Instance.MainGameMediator.CurrentBossRuntime;
            if (existingBoss != null)
            {
                TryBindValidatedTarget(existingBoss);
            }

            // 訂閱註冊廣播
            GameManager.Instance.MainGameMediator.OnBossRegisteredEvent += OnBossRegistered;
        }
    }

    // ★ 接收到廣播時，不要馬上綁定，交給驗證器！
    private void OnBossRegistered(EntityRuntime newBoss)
    {
        TryBindValidatedTarget(newBoss);
    }

    // ==========================================
    // ★ 核心防護網：驗證型綁定
    // ==========================================
    private void TryBindValidatedTarget(EntityRuntime candidate)
    {
        if (candidate == null) return;
        
        // 如果這個腳本是啟用的，開啟協程等待資料填滿
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(WaitUntilReadyAndBind(candidate));
        }
    }

    private IEnumerator WaitUntilReadyAndBind(EntityRuntime candidate)
    {
        // 死死盯著這個候選人，直到它具備我們需要的資料
        while (!IsTargetReady(candidate))
        {
            yield return null;
        }

        // 資料終於寫入了，執行正式綁定！
        if (_target != candidate)
        {
            Debug.Log($"<color=lime>[HUD 綁定] 目標資料已初始化完畢，正式綁定 {displayMode}！</color>");
            BindTarget(candidate);
        }
    }

    private bool IsTargetReady(EntityRuntime candidate)
    {
        switch (displayMode)
        {
            case DisplayMode.Health:
                // ★ 關鍵：必須等到 HealthComponent 把最大血量寫入後，才算是一隻活著的 Boss
                return candidate.MaxHealth > 0; 
            case DisplayMode.Stamina:
                return candidate.TryGetTrait(out StaminaTrait _);
            case DisplayMode.Timer:
                return candidate.TryGetTrait(out TimerTrait _);
        }
        return false;
    }

    private void OnDisable() => UnbindTarget();

    private void OnDestroy()
    {
        if (_runtimeMaterial != null) Destroy(_runtimeMaterial);

        if (GameManager.Instance != null && GameManager.Instance.MainGameMediator != null)
        {
            GameManager.Instance.MainGameMediator.OnBossRegisteredEvent -= OnBossRegistered;
        }
    }

    private void Update()
    {
        _currentRatio = Mathf.Lerp(_currentRatio, _targetRatio, Time.deltaTime * smoothSpeed);

        if (visualMode == VisualMode.Scale2D && _fillPivot != null)
        {
            Vector3 currentScale = _initialScale;
            currentScale.x = _initialScale.x * _currentRatio;
            _fillPivot.localScale = currentScale;
            
            if (_fillSprite != null) 
                _fillSprite.color = Color.Lerp(emptyColor, fullColor, _currentRatio);
        }
        else if (visualMode == VisualMode.Shader3D && _runtimeMaterial != null)
        {
            _runtimeMaterial.SetFloat(_fillAmountPropertyID, _currentRatio);
        }
    }

    // ==========================================
    // 核心綁定邏輯 (維持不變)
    // ==========================================
    // ★ 核心綁定邏輯 (修改這裡)
    public void BindTarget(EntityRuntime newTarget)
    {
        UnbindTarget();
        _target = newTarget;
        if (_target == null) return;

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
        else if (displayMode == DisplayMode.Timer)
        {
            if (_target.TryGetTrait(out _cachedTimerTrait))
            {
                _cachedTimerTrait.OnTimerRatioChanged += UpdateTargetRatio;
                UpdateTargetRatio(_cachedTimerTrait.TimerRatio);
            }
        }
        
        // 瞬間填滿，不跑動畫
        _currentRatio = _targetRatio;
        
        if (visualMode == VisualMode.Scale2D && _fillPivot != null)
        {
            Vector3 currentScale = _initialScale;
            currentScale.x = _initialScale.x * _currentRatio;
            _fillPivot.localScale = currentScale;
            if (_fillSprite != null) _fillSprite.color = Color.Lerp(emptyColor, fullColor, _currentRatio);
        }
        else if (visualMode == VisualMode.Shader3D && _runtimeMaterial != null)
        {
            _runtimeMaterial.SetFloat(_fillAmountPropertyID, _currentRatio);
        }
    }

    private void UnbindTarget()
    {
        if (_target == null) return;

        if (displayMode == DisplayMode.Health)
            _target.OnHealthRatioChanged -= UpdateTargetRatio;
        else if (displayMode == DisplayMode.Stamina && _cachedStaminaTrait != null)
        {
            _cachedStaminaTrait.OnStaminaRatioChanged -= UpdateTargetRatio;
            _cachedStaminaTrait = null;
        }
        else if (displayMode == DisplayMode.Timer && _cachedTimerTrait != null)
        {
            _cachedTimerTrait.OnTimerRatioChanged -= UpdateTargetRatio;
            _cachedTimerTrait = null;
        }
        _target = null;
    }

    // ★ 在這裡攔截並翻轉數值！
    private void UpdateTargetRatio(float newRatio) 
    {
        // 如果開啟翻轉，就把 0 變 1，把 0.8 變 0.2
        _targetRatio = invertFill ? (1f - newRatio) : newRatio;
    }

    [ContextMenu("Calculate Bounds")]
    public void SetupBounds()
    {
        // 邊界計算邏輯...
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        if (targetRenderer == null) return;
        Material mat = Application.isPlaying ? targetRenderer.material : targetRenderer.sharedMaterial;
        if (mat == null) return;
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        Vector3 fillDir = mat.GetVector("_FillDirection").normalized;
        if (fillDir == Vector3.zero) fillDir = Vector3.up; 
        Bounds bounds = mf.sharedMesh.bounds;
        Vector3[] corners = new Vector3[8];
        Vector3 min = bounds.min; Vector3 max = bounds.max;
        corners[0] = new Vector3(min.x, min.y, min.z); corners[1] = new Vector3(min.x, min.y, max.z);
        corners[2] = new Vector3(min.x, max.y, min.z); corners[3] = new Vector3(min.x, max.y, max.z);
        corners[4] = new Vector3(max.x, min.y, min.z); corners[5] = new Vector3(max.x, min.y, max.z);
        corners[6] = new Vector3(max.x, max.y, min.z); corners[7] = new Vector3(max.x, max.y, max.z);

        float minBound = float.MaxValue, maxBound = float.MinValue;
        foreach (Vector3 corner in corners)
        {
            float projection = Vector3.Dot(corner, fillDir);
            if (projection < minBound) minBound = projection;
            if (projection > maxBound) maxBound = projection;
        }
        mat.SetFloat("_MinBound", minBound); mat.SetFloat("_MaxBound", maxBound);
    }
}