using System;
using UnityEngine;

// 特徵的共同合約
public interface IEntityTrait 
{
    void Initialize();
}

// ==========================================
// 特徵 A：體力與移動模組 (取代原本的 Player 專屬變數)
// ==========================================
[Serializable]
public class StaminaTrait : IEntityTrait
{
    public float maxStamina = 100f;
    [NonSerialized] public float currentStamina; // 不存硬碟
    
    public float moveSpeed = 5f;
    public float dashSpeed = 20f;
    public float dashCost = 30f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.5f;

    public float StaminaRatio => maxStamina > 0 ? Mathf.Clamp01(currentStamina / maxStamina) : 0f;

    public event Action<float> OnStaminaRatioChanged;
    
    public void Initialize()
    {
        currentStamina = maxStamina;
    }

    public bool ConsumeStamina(float amount)
    {
        if (currentStamina < amount) return false;
        currentStamina -= amount;
        OnStaminaRatioChanged?.Invoke(StaminaRatio); // ★ 廣播
        return true;
    }

    public void RegenStamina(float amountPerSec, float deltaTime)
    {
        currentStamina = Mathf.Min(maxStamina, currentStamina + (amountPerSec * deltaTime));
        OnStaminaRatioChanged?.Invoke(StaminaRatio); // ★ 廣播
    }
}

// ★ 大世界專屬的移動特徵 (掌管跳躍、攀爬等非戰鬥移動)
[Serializable]
public class ExplorationTrait : IEntityTrait
{
    [Header("大世界移動參數")]
    public float adventureWalkSpeed = 6f; 
    public float adventureSprintSpeed = 12f;
    
    [Header("跳躍與物理參數")]
    public float jumpForce = 8f;
    public float gravityMultiplier = 2f; // 讓大世界跳躍手感更好的重力加成
    public float fallDamageThreshold = 10f; 

    public void Initialize()
    {
        // 這裡通常不需要初始化邏輯
    }
}

// ==========================================
// 特徵 B：計時器模組 (取代原本的 Boss 專屬變數)
// ==========================================
[Serializable]
public class TimerTrait : IEntityTrait
{
    [NonSerialized] public float currentTimer;
    [NonSerialized] public float maxTimer;
    [NonSerialized] private bool _isTimerFinished = false;

    public float TimerRatio => maxTimer > 0 ? Mathf.Clamp01(currentTimer / maxTimer) : 0f;

    public void Initialize()
    {
        currentTimer = 0;
        maxTimer = 0;
        _isTimerFinished = false;
    }

    public void StartTimer(float duration)
    {
        maxTimer = duration;
        currentTimer = duration;
        _isTimerFinished = false;
    }

    public void ReduceTimer(float seconds)
    {
        if (_isTimerFinished) return;
        currentTimer = Mathf.Max(0f, currentTimer - seconds);
        if (currentTimer <= 0f) _isTimerFinished = true;
    }

    public bool TickTimer(float deltaTime)
    {
        if (_isTimerFinished)
        {
            _isTimerFinished = false; 
            return true; 
        }
        if (currentTimer <= 0f) return false;

        currentTimer -= deltaTime;
        if (currentTimer <= 0f)
        {
            currentTimer = 0f;
            _isTimerFinished = true;
            return true;
        }
        return false;
    }
}

// ==========================================
// 特徵 C：執行期實體錨點 (解決外部指令尋找目標的問題)
// ==========================================
[Serializable]
public class RuntimeAnchorTrait : IEntityTrait
{
    [NonSerialized] private IHealable activeHealable;
    [NonSerialized] private IDamageable activeDamageable;

    public void Initialize()
    {
        activeHealable = null;
        activeDamageable = null;
    }

    public void RegisterEntity(GameObject entityObj)
    {
        activeHealable = entityObj.GetComponent<IHealable>();
        activeDamageable = entityObj.GetComponent<IDamageable>();
    }

    public void TryHeal(HealPayload payload)
    {
        if (activeHealable != null) activeHealable.ReceiveHeal(payload);
        else Debug.LogWarning("嘗試治療，但實體錨點為空！");
    }

    public void TryDamage(DamagePayload payload)
    {
        if (activeDamageable != null) activeDamageable.TakeDamage(payload);
        else Debug.LogWarning("嘗試傷害，但實體錨點為空！");
    }
}