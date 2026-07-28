using UnityEngine;
using Gamemanager;

// ★ 同樣直接繼承共同底層 InteractiveMetaEntity3D！
public class MetaInteractiveHUD3D : InteractiveMetaEntity3D
{
    public enum HUDType { PlayerHealth, PlayerStamina, BossHealth }

    [Header("儀表板屬性")]
    [SerializeField] private HUDType hudType;
    [SerializeField] private SpriteRenderer fillSpriteRenderer;
    [SerializeField] private bool autoHideOutsideCombat = false;

    // ★ 徹底刪除 [SerializeField] private float maxScaleX！
    [Header("視覺變形參數")]
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private Color fullColor = Color.green;
    [SerializeField] private Color emptyColor = Color.red;

    private float _targetRatio = 1.0f;
    private float _currentRatio = 1.0f;
    
    // ★ 新增內部快取變數，自動記憶這張圖片「滿血時的初始 X 軸長度」
    private float _cachedInitialScaleX;

    protected override void Awake()
    {
        base.Awake(); // ★ 呼叫父類別的 Awake，自動處理一切物理設定！

        // ★ 核心自動化：在遊戲啟動的第一個瞬間，直接將 Prefab 當時的 X 軸長度記錄為 100% 滿值的真實來源！
        if (fillSpriteRenderer != null)
        {
            _cachedInitialScaleX = fillSpriteRenderer.transform.localScale.x;
        }
        else
        {
            Debug.LogError($"[錯誤] {gameObject.name} 的 FillSpriteRenderer 未指派！");
        }

        if (autoHideOutsideCombat && hudType == HUDType.BossHealth)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (GameManager.Instance?.MainGameEvent == null) return;

        switch (hudType)
        {
            case HUDType.PlayerHealth:
                GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnPlayerHealthChangedEvent, OnPlayerHealth);
                break;
            case HUDType.PlayerStamina:
                GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnPlayerStaminaChangedEvent, OnPlayerStamina);
                break;
            case HUDType.BossHealth:
                GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossHealthChangedEvent, OnBossHealth);
                GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterSpecialPhaseEvent, OnBossCombatStart);
                GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterIdlePhaseEvent, OnBossCombatEnd);
                break;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance?.MainGameEvent == null) return;

        switch (hudType)
        {
            case HUDType.PlayerHealth:
                GameManager.Instance.MainGameEvent.Unsubscribe<PlayerHealthChangedEvent>(OnPlayerHealth);
                break;
            case HUDType.PlayerStamina:
                GameManager.Instance.MainGameEvent.Unsubscribe<PlayerStaminaChangedEvent>(OnPlayerStamina);
                break;
            case HUDType.BossHealth:
                GameManager.Instance.MainGameEvent.Unsubscribe<BossHealthChangedEvent>(OnBossHealth);
                GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterSpecialPhaseEvent>(OnBossCombatStart);
                GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterIdlePhaseEvent>(OnBossCombatEnd);
                break;
        }
    }

    private void Update()
    {
        if (fillSpriteRenderer == null) return;

        _currentRatio = Mathf.Lerp(_currentRatio, _targetRatio, Time.deltaTime * smoothSpeed);

        // ★ 直接使用自動記錄的 _cachedInitialScaleX 進行比例乘法
        Vector3 scale = fillSpriteRenderer.transform.localScale;
        scale.x = _currentRatio * _cachedInitialScaleX;
        fillSpriteRenderer.transform.localScale = scale;

        fillSpriteRenderer.color = Color.Lerp(emptyColor, fullColor, _currentRatio);
    }

    private void OnPlayerHealth(PlayerHealthChangedEvent evt) => _targetRatio = Mathf.Clamp01((float)evt.CurrentHealth / evt.MaxHealth);
    private void OnPlayerStamina(PlayerStaminaChangedEvent evt) => _targetRatio = Mathf.Clamp01(evt.CurrentStamina / evt.MaxStamina);
    private void OnBossHealth(BossHealthChangedEvent evt) => _targetRatio = Mathf.Clamp01((float)evt.CurrentHealth / evt.MaxHealth);

    private void OnBossCombatStart(BossEnterSpecialPhaseEvent evt) { if (autoHideOutsideCombat) gameObject.SetActive(true); }
    private void OnBossCombatEnd(BossEnterIdlePhaseEvent evt) { if (autoHideOutsideCombat) gameObject.SetActive(false); }
}