using UnityEngine;

[RequireComponent(typeof(EntityCore))]
public class StaminaVisualFeedback : MonoBehaviour, IEntityRuntimeDependent
{
    [SerializeField] private SpriteRenderer _sr;
    [SerializeField] private Color _lowStaminaColor = Color.red;
    [SerializeField] private Color _normalColor = Color.white;
    
    private EntityRuntime _entityData;
    private StaminaTrait _staminaTrait;

    private void Awake()
    {
        if (_sr == null) _sr = GetComponentInChildren<SpriteRenderer>();
    }

    // ==========================================
    // ★ 介面實作：這是唯一的資料與綁定入口，完全取代 Start
    // ==========================================
    public void OnRuntimeDataChanged(EntityRuntime newData)
    {
        // 1. 防禦性解綁：如果原本有舊特徵，先解除訂閱防止記憶體洩漏
        if (_staminaTrait != null) 
        {
            _staminaTrait.OnStaminaRatioChanged -= UpdateStaminaColor;
        }
    
        // 2. 接收新大腦
        _entityData = newData;

        // 3. 索取新大腦的特徵並重新訂閱
        if (_entityData != null && _entityData.TryGetTrait(out _staminaTrait))
        {
            _staminaTrait.OnStaminaRatioChanged += UpdateStaminaColor;
            UpdateStaminaColor(_staminaTrait.StaminaRatio); // 瞬間刷新視覺防斷層
        }
    }
    
    private void OnDestroy()
    {
        // 實體銷毀時的最終記憶體釋放
        if (_staminaTrait != null)
        {
            _staminaTrait.OnStaminaRatioChanged -= UpdateStaminaColor;
        }
    }

    private void UpdateStaminaColor(float ratio)
    {
        // 若實體正在無敵/受擊閃爍狀態，放棄修改顏色，交由 Damage 邏輯主導
        if (_entityData != null && _entityData.HasState(EntityStateFlags.Invincible)) return;

        _sr.color = Color.Lerp(_lowStaminaColor, _normalColor, ratio);
    }
}