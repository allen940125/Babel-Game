using UnityEngine;

[CreateAssetMenu(fileName = "SO_PlayerRuntimeData", menuName = "Game/Runtime Data/Player Entity Data")]
public class PlayerRuntimeSO : BaseEntityRuntimeSO
{
    [Header("★ Player 專屬：體力與移動能力數值 (集中管理、方便 Buff/Debuff)")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float currentStamina;
    
    [Tooltip("基礎移動速度 (陷阱或道具可直接修改此數值達到加速/緩速)")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashCost = 30f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.5f;
    
    // ★ 執行期錨點 (供 Command 或 UI 呼叫，不存硬碟)
    [System.NonSerialized] private IHealable activePlayerHealable;
    [System.NonSerialized] private IDamageable activePlayerDamageable;

    // 唯讀封裝：供 PlayerController3D 在 Update/FixedUpdate 直接讀取！
    public float MaxStamina => maxStamina;
    public float CurrentStamina => currentStamina;
    public float StaminaRatio => maxStamina > 0 ? Mathf.Clamp01(currentStamina / maxStamina) : 0f;
    public float MoveSpeed => moveSpeed;
    public float DashSpeed => dashSpeed;
    public float DashCost => dashCost;
    public float DashDuration => dashDuration;
    public float DashCooldown => dashCooldown;

    public override void Initialize(int maxHP, int atk, int def)
    {
        base.Initialize(maxHP, atk, def);
        currentStamina = maxStamina;
    }

    public bool ConsumeStamina(float amount)
    {
        if (currentStamina < amount) return false;
        currentStamina -= amount;
        return true;
    }

    public void RegenStamina(float amountPerSec, float deltaTime)
    {
        currentStamina = Mathf.Min(maxStamina, currentStamina + (amountPerSec * deltaTime));
    }
    
    public void RegisterPlayer(GameObject playerObj)
    {
        activePlayerHealable = playerObj.GetComponent<IHealable>();
        activePlayerDamageable = playerObj.GetComponent<IDamageable>();
    }

    public void TryHealPlayer(HealPayload payload)
    {
        if (activePlayerHealable != null) activePlayerHealable.ReceiveHeal(payload);
        else Debug.LogWarning("嘗試治療玩家，但場景中無玩家實體！");
    }

    public void TryDamagePlayer(DamagePayload payload)
    {
        if (activePlayerDamageable != null) activePlayerDamageable.TakeDamage(payload);
        else Debug.LogWarning("嘗試傷害玩家，但場景中無玩家實體！");
    }
}