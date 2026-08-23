using System;
using UnityEngine;
using UnityEngine.Serialization;

public class MetaInteractiveHUD3D : InteractiveMetaEntity3D
{
    // ★ 將 HUDType 降級為只區分「要看哪種數值」，不再區分「是誰的數值」
    public enum DisplayMode { Health, Stamina }

    [FormerlySerializedAs("targetSO")]
    [Header("資料來源 (SSOT)")]
    [Tooltip("把你想監看的對象 SO 拖進來，不管是玩家還是 Boss 都可以！")]
    [SerializeField] private EntityRuntime target;
    [SerializeField] private DisplayMode displayMode = DisplayMode.Health;

    [Header("儀表板屬性")]
    [SerializeField] private SpriteRenderer fillSpriteRenderer;
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private Color fullColor = Color.green;
    [SerializeField] private Color emptyColor = Color.red;

    private float _targetRatio = 1.0f;
    private float _currentRatio = 1.0f;
    private float _cachedInitialScaleX;
    
    // 用來暫存抓到的特徵，方便解除訂閱
    private StaminaTrait _cachedStaminaTrait;

    protected override void Awake()
    {
        base.Awake(); 

        if (fillSpriteRenderer != null) _cachedInitialScaleX = fillSpriteRenderer.transform.localScale.x;
        if (target == null) Debug.LogError($"[錯誤] {gameObject.name} 未綁定目標 SO！");
    }

    private void OnEnable()
    {
        if (target == null) return;

        // ★ 核心：直接訂閱特定 SO 的資料變化事件！不再透過 GameManager！
        if (displayMode == DisplayMode.Health)
        {
            target.OnHealthRatioChanged += UpdateTargetRatio;
            // 啟用時強制同步一次當前畫面
            UpdateTargetRatio(target.MaxHealth > 0 ? (float)target.CurrentHealth / target.MaxHealth : 0f);
        }
        else if (displayMode == DisplayMode.Stamina)
        {
            if (target.TryGetTrait(out _cachedStaminaTrait))
            {
                _cachedStaminaTrait.OnStaminaRatioChanged += UpdateTargetRatio;
                UpdateTargetRatio(_cachedStaminaTrait.StaminaRatio);
            }
        }
    }

    private void OnDisable()
    {
        if (target == null) return;

        // ★ 解除訂閱，防止記憶體洩漏
        if (displayMode == DisplayMode.Health)
        {
            target.OnHealthRatioChanged -= UpdateTargetRatio;
        }
        else if (displayMode == DisplayMode.Stamina && _cachedStaminaTrait != null)
        {
            _cachedStaminaTrait.OnStaminaRatioChanged -= UpdateTargetRatio;
        }
    }

    // ★ 接收到廣播時，只負責更新目標比例
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