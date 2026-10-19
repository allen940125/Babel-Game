using UnityEngine;
using UnityEngine.Events;

public class DamageToInteractionAdapter : MonoBehaviour, IEntityRuntimeDependent
{
    private InteractiveMetaEntity3D _metaEntity;
    private BaseButtonController3D _button;
    
    [Header("損壞時的數值設定")]
    [SerializeField] private bool lockClickWhenBroken = false;
    [SerializeField] private float brokenEfficiency = 0.5f;

    [Header("★ 供外部使用的生命比例 (0.0 ~ 1.0)")]
    public UnityEvent<float> OnHealthPercentChanged; 

    // 儲存目前正在監聽的大腦資料，為了能在腳本銷毀時正確取消訂閱
    private EntityRuntime _currentData; 

    private void Awake()
    {
        _button = GetComponent<BaseButtonController3D>();
        _metaEntity = _button != null ? _button : GetComponent<InteractiveMetaEntity3D>();
    }

    // ★ 1. 遊戲開始時，大腦送資料過來
    public void OnRuntimeDataChanged(EntityRuntime newData)
    {
        // 如果之前有訂閱舊的，先退訂 (防呆)
        if (_currentData != null)
        {
            _currentData.OnHealthRatioChanged -= HandleHealthChanged;
        }

        _currentData = newData;

        if (_currentData != null)
        {
            // ★ 2. 訂閱：以後只要血量一變，大腦就會自動呼叫 HandleHealthChanged
            _currentData.OnHealthRatioChanged += HandleHealthChanged;
            
            // 初始化時先手動檢查一次當前狀態
            float initialRatio = _currentData.MaxHealth > 0 ? (float)_currentData.CurrentHealth / _currentData.MaxHealth : 0f;
            HandleHealthChanged(initialRatio);
        }
    }

    // ★ 3. 每次扣血/補血時，這裡會被即時觸發
    private void HandleHealthChanged(float healthRatio)
    {
        // 把百分比廣播出去給 UI 或特效用
        OnHealthPercentChanged?.Invoke(healthRatio);

        // 如果比例 <= 0，代表壞了
        bool isBroken = healthRatio <= 0f;

        // 鎖死/解鎖物理拖曳
        if (_metaEntity != null)
        {
            _metaEntity.SetInteractable(!isBroken, !isBroken);
        }

        // 修改按鈕效能與開關
        if (_button != null)
        {
            _button.IsExternallyLocked = isBroken && lockClickWhenBroken;
            _button.EfficiencyMultiplier = isBroken ? brokenEfficiency : 1.0f;
        }
    }

    // ★ 4. 記得在銷毀時取消訂閱，避免記憶體洩漏 (Memory Leak)
    private void OnDestroy()
    {
        if (_currentData != null)
        {
            _currentData.OnHealthRatioChanged -= HandleHealthChanged;
        }
    }
}