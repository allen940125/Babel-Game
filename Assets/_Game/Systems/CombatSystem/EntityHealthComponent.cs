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
        // 1. 基本防呆
        if (_entityData == null || _entityData.CurrentHealth <= 0 || _isLocalInvincible) return;
        if (payload.Damage < 0) return;

        // 2. ★ 讀取大腦標籤 (GAS-Lite)：只要有 Invincible 標籤，一律免傷！
        if (_entityData.HasState(EntityStateFlags.Invincible))
        {
            Debug.Log($"<color=gray>[防禦阻擋] {gameObject.name} 具有無敵標籤，攻擊無效！</color>");
            return;
        }

        // 3. 結算扣血
        int finalDamage = Mathf.Max(1, payload.Damage - _entityData.TotalDefense);
        _entityData.ModifyHealth(-finalDamage);
        
        // 4. 視覺表現
        onTakeDamageVisuals?.Invoke();

        // ==========================================
        // ★ 5. 致命關鍵：扣血成功後，大喊通知所有訂閱者 (例如 BossStateMachine)！
        // ==========================================
        OnDamageTaken?.Invoke(finalDamage);

        // 6. 受傷後的短暫無敵 (如果是玩家或小怪)
        if (invincibilityDuration > 0 && _entityData.CurrentHealth > 0)
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