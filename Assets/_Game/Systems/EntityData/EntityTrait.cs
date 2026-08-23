using System;
using UnityEngine;

// ★ 將 interface 改為 abstract class
[Serializable]
public abstract class EntityTrait 
{
    // 基底預設提供淺拷貝能力
    public virtual EntityTrait Clone()
    {
        // 呼叫底層的淺拷貝
        return (EntityTrait)this.MemberwiseClone();
    }

    // 將 Initialize 變成虛擬方法，讓子類別決定要不要覆寫
    public virtual void Initialize(EntityRuntime owner) { }
}

// ==========================================
// 特徵 A：體力與移動模組
// ==========================================
[Serializable]
public class StaminaTrait : EntityTrait
{
    public float maxStamina = 100f;
    [NonSerialized] public float currentStamina; 
    
    public float moveSpeed = 5f;
    public float dashSpeed = 20f;
    public float dashCost = 30f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.5f;

    public float StaminaRatio => maxStamina > 0 ? Mathf.Clamp01(currentStamina / maxStamina) : 0f;
    public event Action<float> OnStaminaRatioChanged;
    
    // ★ 必須加上 override
    public override EntityTrait Clone()
    {
        var copy = (StaminaTrait)base.Clone();
        copy.currentStamina = 0; 
        copy.OnStaminaRatioChanged = null; 
        return copy;
    }
    
    // ★ 必須加上 override 與參數 (即使你目前沒用到 owner，簽名也必須對齊)
    public override void Initialize(EntityRuntime owner)
    {
        currentStamina = maxStamina;
    }

    public bool ConsumeStamina(float amount)
    {
        if (currentStamina < amount) return false;
        currentStamina -= amount;
        OnStaminaRatioChanged?.Invoke(StaminaRatio); 
        return true;
    }

    public void RegenStamina(float amountPerSec, float deltaTime)
    {
        currentStamina = Mathf.Min(maxStamina, currentStamina + (amountPerSec * deltaTime));
        OnStaminaRatioChanged?.Invoke(StaminaRatio); 
    }
}

// ==========================================
// 特徵 B：大世界專屬的移動特徵
// ==========================================
[Serializable]
public class ExplorationTrait : EntityTrait
{
    [Header("大世界移動參數")]
    public float adventureWalkSpeed = 6f; 
    public float adventureSprintSpeed = 12f;
    
    [Header("跳躍與物理參數")]
    public float jumpForce = 8f;
    public float gravityMultiplier = 2f;
    public float fallDamageThreshold = 10f; 

    // ★ 錯誤指正：這個特徵完全沒有「執行期狀態 (沒有 NonSerialized)」
    // 所以你「根本不需要」寫 Clone() 和 Initialize()！
    // 直接刪除！基底的 MemberwiseClone 會完美幫你把這五個參數淺拷貝過去。
}

// ==========================================
// 特徵 C：計時器模組
// ==========================================
[Serializable]
public class TimerTrait : EntityTrait
{
    [NonSerialized] public float currentTimer;
    [NonSerialized] public float maxTimer;
    [NonSerialized] private bool _isTimerFinished = false;

    public float TimerRatio => maxTimer > 0 ? Mathf.Clamp01(currentTimer / maxTimer) : 0f;

    // ★ 必須加上 override
    public override EntityTrait Clone()
    {
        return new TimerTrait();
    }
    
    // ★ 必須加上 override 與參數
    public override void Initialize(EntityRuntime owner)
    {
        currentTimer = 0;
        maxTimer = 0;
        _isTimerFinished = false;
    }

    // ... 其他計時器方法保持不變 (StartTimer, ReduceTimer, TickTimer) ...
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
// 特徵 D：執行期實體錨點
// ==========================================
[Serializable]
public class RuntimeAnchorTrait : EntityTrait
{
    [NonSerialized] private IHealable activeHealable;
    [NonSerialized] private IDamageable activeDamageable;

    // ★ 必須加上 override
    public override EntityTrait Clone()
    {
        return new RuntimeAnchorTrait();
    }
    
    // ★ 必須加上 override 與參數
    public override void Initialize(EntityRuntime owner)
    {
        activeHealable = null;
        activeDamageable = null;
    }

    // ... 註冊與觸發邏輯保持不變 ...
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