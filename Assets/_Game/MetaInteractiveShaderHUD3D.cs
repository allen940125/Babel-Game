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

    [Header("2D 渲染設定 (Scale2D 模式)")]
    [SerializeField] private Transform _fillPivot; 
    [SerializeField] private SpriteRenderer _fillSprite;
    [SerializeField] private Color fullColor = Color.green;
    [SerializeField] private Color emptyColor = Color.red;

    [Header("3D Shader 渲染設定 (Shader3D 模式)")]
    [SerializeField] private Renderer targetRenderer;
    
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
        // ★ 所有模式都改用協程去追蹤，徹底消滅生命週期先後順序造成的 Bug
        if (bindingMode == BindingMode.LocalParent)
        {
            var core = GetComponentInParent<EntityCore>();
            if (core != null) 
                StartCoroutine(WaitAndBindTargetRoutine(core.RuntimeData));
            else 
                Debug.LogError($"[錯誤] {gameObject.name} 找不到父物件的 EntityCore！");
        }
        else if (bindingMode == BindingMode.GlobalPlayer)
        {
            StartCoroutine(WaitAndBindGlobalRoutine(isBoss: false));
        }
        else if (bindingMode == BindingMode.GlobalBoss)
        {
            StartCoroutine(WaitAndBindGlobalRoutine(isBoss: true));
        }
    }

    // ==========================================
    // ★ 萬用獵犬協程 (解決所有生成時間差與數值空窗期)
    // ==========================================
    private IEnumerator WaitAndBindGlobalRoutine(bool isBoss)
    {
        EntityRuntime validTarget = null;

        while (validTarget == null)
        {
            if (GameManager.Instance != null && GameManager.Instance.MainGameMediator != null)
            {
                var candidate = isBoss 
                    ? GameManager.Instance.MainGameMediator.CurrentBossRuntime 
                    : GameManager.Instance.MainGameMediator.CurrentPlayerRuntime;

                if (candidate != null && _target != candidate)
                {
                    if (IsTargetReady(candidate))
                    {
                        validTarget = candidate;
                    }
                }
            }
            if (validTarget == null) yield return null; 
        }

        Debug.Log($"<color=lime>[HUD 綁定] 全域目標尋找結束！成功捕捉到 {displayMode} 資料。</color>");
        BindTarget(validTarget);
    }

    private IEnumerator WaitAndBindTargetRoutine(EntityRuntime candidate)
    {
        // 針對 LocalParent 模式，確保父物件把資料塞滿了才綁定
        while (!IsTargetReady(candidate))
        {
            yield return null;
        }
        Debug.Log($"<color=lime>[HUD 綁定] 父物件資料初始化完成，綁定 {displayMode}！</color>");
        BindTarget(candidate);
    }

    // ★ 核心防呆：檢查目標的「特徵」是否真正初始化完畢
    private bool IsTargetReady(EntityRuntime candidate)
    {
        if (candidate == null) return false;

        switch (displayMode)
        {
            case DisplayMode.Health:
                // ★ 雙重防護：不僅要設定上限，當前血量也必須大於 0，防止抓到殘血空殼
                return candidate.MaxHealth > 0 && candidate.CurrentHealth > 0; 
                
            case DisplayMode.Stamina:
                return candidate.TryGetTrait(out StaminaTrait _);
                
            case DisplayMode.Timer:
                return candidate.TryGetTrait(out TimerTrait _);
        }
        return false;
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

    private void OnDisable() => UnbindTarget();
    private void OnDestroy() { if (_runtimeMaterial != null) Destroy(_runtimeMaterial); }

    // ==========================================
    // 核心綁定邏輯 (維持不變)
    // ==========================================
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

    private void UpdateTargetRatio(float newRatio) => _targetRatio = newRatio;

    // ==========================================
    // 編輯器自動計算功能 (維持不變)
    // ==========================================
    [ContextMenu("Calculate Bounds")]
    public void SetupBounds()
    {
        // 你的邊界計算邏輯保持原樣...
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