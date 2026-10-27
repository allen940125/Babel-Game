using UnityEngine;

[RequireComponent(typeof(BaseButtonController3D))]
[RequireComponent(typeof(RammingWeaponBehavior3D))]
public class SyringeMechanicAdapter : MonoBehaviour
{
    [Header("抽血針筒設定")]
    [Tooltip("每個階段最多可以吸血/補血的總循環次數")]
    [SerializeField] private int maxCyclesPerPhase = 2;
    
    [Header("★ 視覺控制 (Shader Fill)")]
    [Tooltip("拖入掛有 ShaderFillVisualizer 的模型物件")]
    [SerializeField] private ShaderFillVisualizer fillVisualizer;
    
    private BaseButtonController3D _button;
    private RammingWeaponBehavior3D _weapon;
    
    private int _currentHealCount = 0;
    private bool _hasBlood = true; // 預設一開始裡面是有血(綠色)的

    private void Awake()
    {
        _button = GetComponent<BaseButtonController3D>();
        _weapon = GetComponent<RammingWeaponBehavior3D>();

        // 訂閱事件
        _button.OnCommandExecuted.AddListener(HandleHealUsed);
        _weapon.OnRammingSuccess.AddListener(HandleEnemyStabbed);
        _button.OnReset.AddListener(ResetSyringeForNewPhase);
    }

    private void Start()
    {
        // 遊戲一開始，強制重置狀態與視覺
        UpdateSyringeState();
    }

    // ==========================================
    // 1. 打針 (消耗血)
    // ==========================================
    private void HandleHealUsed()
    {
        if (_hasBlood)
        {
            _hasBlood = false;
            _currentHealCount++;
            
            Debug.Log($"<color=cyan>[針筒] 已注射！目前使用次數: {_currentHealCount}/{maxCyclesPerPhase}</color>");
            UpdateSyringeState();
        }
    }

    // ==========================================
    // 2. 刺敵人 (抽血)
    // ==========================================
    private void HandleEnemyStabbed(GameObject target)
    {
        if (!_hasBlood && _currentHealCount < maxCyclesPerPhase)
        {
            _hasBlood = true;
            Debug.Log($"<color=green>[針筒] 成功從 {target.name} 抽出鮮血！</color>");
            UpdateSyringeState();
        }
    }

    // ==========================================
    // 3. Boss 切換階段 (重置)
    // ==========================================
    private void ResetSyringeForNewPhase()
    {
        _currentHealCount = 0;
        _hasBlood = true;
        UpdateSyringeState();
    }

    // ==========================================
    // ★ 核心狀態與 Shader 視覺更新
    // ==========================================
    private void UpdateSyringeState()
    {
        // 1. 邏輯鎖定：沒血了，或是次數用光了，就鎖死按鈕
        bool shouldLock = !_hasBlood || (_currentHealCount >= maxCyclesPerPhase);
        _button.IsExternallyLocked = shouldLock;

        // 2. 視覺更新：呼叫 ShaderFillVisualizer
        if (fillVisualizer != null)
        {
            // 如果有血，FillAmount 設為 1.0 (滿的)
            // 如果沒血，FillAmount 設為 0.0 (空的)
            float targetFill = _hasBlood ? 1.0f : 0.0f;
            fillVisualizer.SetFillAmount(targetFill);
        }
    }
}