using UnityEngine;

public abstract class BaseEntityRuntimeSO : ScriptableObject
{
    [Header("基礎生存數值")]
    [SerializeField] protected int maxHealth = 100;
    [SerializeField] protected int currentHealth;

    [Header("★ 基礎戰鬥數值 (SSOT)")]
    [SerializeField] protected int attackPower = 20;
    [SerializeField] protected int defense = 3;
    [Range(0f, 1f)] [SerializeField] protected float critRate = 0.2f;
    [SerializeField] protected float critMultiplier = 1.5f;
    
    [Header("★ 戰鬥狀態限制旗標")]
    [SerializeField] private bool hasHealedInThisFight = false;
    
    // 改為唯讀屬性，外部只能看，不能隨意改！
    public bool HasHealedInThisFight => hasHealedInThisFight;

    // 唯讀屬性封裝
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int AttackPower => attackPower;
    public int Defense => defense;
    public float CritRate => critRate;
    public float CritMultiplier => critMultiplier;

    public virtual void Initialize(int maxHP, int atk, int def)
    {
        maxHealth = maxHP;
        currentHealth = maxHP;
        attackPower = atk;
        defense = def;
    }

    // ★ 資料層受擊扣血介面
    public virtual void ModifyHealth(int delta)
    {
        currentHealth = Mathf.Clamp(currentHealth + delta, 0, maxHealth);
    }
    
    // 新增：由內部或受控的狀態重置方法來修改
    public void LockHealing() => hasHealedInThisFight = true;
    public virtual void ResetFightStates() => hasHealedInThisFight = false;
}