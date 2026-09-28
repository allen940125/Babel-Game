using System; // ★ 確保有 using System 才能用 Action
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class EntityHealthComponent : MonoBehaviour, IDamageable, IHealable
{
    private EntityRuntime _entityData;
    
    [SerializeField] private float invincibilityDuration = 1.5f;
    private bool _isLocalInvincible = false; 

    public UnityEvent onTakeDamageVisuals;
    
    // ★ 新增：純粹的死亡廣播插槽
    public event Action OnDeath;

    // ==========================================
    // ★ 核心通訊插槽：讓 BossStateMachine 可以監聽反擊時機
    // ==========================================
    public event Action<int> OnDamageTaken;

    private void Start()
    {
        var core = GetComponent<EntityCore>();
        if (core != null) _entityData = core.RuntimeData;
        
        if (_entityData != null && _entityData.TryGetTrait(out RuntimeAnchorTrait anchor))
        {
            anchor.RegisterEntity(this.gameObject);
        }
    }

    public void TakeDamage(DamagePayload payload)
    {
        if (_entityData == null || _entityData.CurrentHealth <= 0 || _isLocalInvincible) return;
        if (payload.Damage < 0) return;

        if (_entityData.HasState(EntityStateFlags.Invincible)) return;

        int finalDamage = Mathf.Max(1, payload.Damage - _entityData.TotalDefense);
        _entityData.ModifyHealth(-finalDamage);
        
        onTakeDamageVisuals?.Invoke();
        OnDamageTaken?.Invoke(finalDamage);

        // ★ 核心追加：判定死亡並廣播
        if (_entityData.CurrentHealth <= 0)
        {
            OnDeath?.Invoke();
            return; // 死了就不需要處理後續的無敵時間
        }

        if (invincibilityDuration > 0)
        {
            StartCoroutine(InvincibilityRoutine());
        }
    }

    private IEnumerator InvincibilityRoutine()
    {
        _isLocalInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        _isLocalInvincible = false;
    }

    public void ReceiveHeal(HealPayload payload)
    {
        if (_entityData == null || _entityData.CurrentHealth <= 0) return;
        _entityData.ModifyHealth(payload.HealAmount);
    }
}