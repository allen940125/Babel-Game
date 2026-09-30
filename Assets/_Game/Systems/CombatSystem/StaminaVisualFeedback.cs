using UnityEngine;

[RequireComponent(typeof(EntityCore))]
public class StaminaVisualFeedback : MonoBehaviour
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

    private void Start()
    {
        _entityData = GetComponent<EntityCore>().RuntimeData;
        if (_entityData != null && _entityData.TryGetTrait(out _staminaTrait))
        {
            // 透過事件驅動，取代 Update 中每幀計算
            _staminaTrait.OnStaminaRatioChanged += UpdateStaminaColor;
            UpdateStaminaColor(_staminaTrait.StaminaRatio);
        }
        else
        {
            Debug.LogError($"[邏輯錯誤] {gameObject.name} 掛載了 StaminaVisualFeedback，但沒有 StaminaTrait！");
            enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (_staminaTrait != null)
        {
            _staminaTrait.OnStaminaRatioChanged -= UpdateStaminaColor;
        }
    }

    private void UpdateStaminaColor(float ratio)
    {
        // 若實體正在無敵/受擊閃爍狀態，放棄修改顏色，交由 Damage 邏輯主導
        if (_entityData.HasState(EntityStateFlags.Invincible)) return;

        _sr.color = Color.Lerp(_lowStaminaColor, _normalColor, ratio);
    }
}