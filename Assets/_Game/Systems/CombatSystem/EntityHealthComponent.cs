using System; 
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class EntityHealthComponent : MonoBehaviour, IEntityRuntimeDependent, IDamageable, IHealable
{
    private EntityRuntime _entityData;
    
    [SerializeField] private float invincibilityDuration = 1.5f;

    public UnityEvent onTakeDamageVisuals;
    
    public event Action OnDeath;
    public event Action<int> OnDamageTaken;

    // ==========================================
    // ★ 實作介面：大腦推播資料時，立刻更新參考並註冊錨點
    // ==========================================
    public void OnRuntimeDataChanged(EntityRuntime newData)
    {
        _entityData = newData;

        // 當大腦替換時，必須將自己重新註冊到新大腦的錨點特徵上
        if (_entityData != null && _entityData.TryGetTrait(out RuntimeAnchorTrait anchor))
        {
            anchor.RegisterEntity(this.gameObject);
        }
    }

    public void TakeDamage(DamagePayload payload)
    {
        // 已刪除重複的無敵檢查
        if (_entityData == null || _entityData.CurrentHealth <= 0 || _entityData.HasState(EntityStateFlags.Invincible)) return;
        if (payload.Damage < 0) return;
        
        if (!payload.IgnoreInvincibility && _entityData.HasState(EntityStateFlags.Invincible)) return;
        
        int finalDamage = Mathf.Max(1, payload.Damage - _entityData.TotalDefense);
        _entityData.ModifyHealth(-finalDamage);
        
        onTakeDamageVisuals?.Invoke();
        OnDamageTaken?.Invoke(finalDamage);

        if (_entityData.CurrentHealth <= 0)
        {
            OnDeath?.Invoke();
            return; 
        }

        // ★ 閘門 2：只有在「不是持續傷害」的情況下，才賦予新的無敵時間
        if (!payload.IgnoreInvincibility && invincibilityDuration > 0)
        {
            StartCoroutine(InvincibilityRoutine());
        }
    }

    private IEnumerator InvincibilityRoutine()
    {
        _entityData.AddState(EntityStateFlags.Invincible);
        yield return new WaitForSeconds(invincibilityDuration);
        _entityData.RemoveState(EntityStateFlags.Invincible);
    }

    public void ReceiveHeal(HealPayload payload)
    {
        if (_entityData == null || _entityData.CurrentHealth <= 0) return;
        _entityData.ModifyHealth(payload.HealAmount);
    }
}