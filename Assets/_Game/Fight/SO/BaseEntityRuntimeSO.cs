using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEntityData", menuName = "Game/Runtime Data/Entity Data")]
public class EntityRuntimeSO : ScriptableObject
{
    [Header("★ 絕對核心數據 (所有生物共用)")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private int attackPower = 20;
    [SerializeField] int defense = 3;
    [Range(0f, 1f)] [SerializeField] float critRate = 0.2f;
    [SerializeField] float critMultiplier = 1.5f;
    
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int AttackPower => attackPower;
    public int Defense => defense;
    public float CritRate => critRate;
    public float CritMultiplier => critMultiplier;

    [Header("★ 動態特徵插槽 (由企劃自由組合)")]
    [SerializeReference]
    public List<IEntityTrait> traits = new List<IEntityTrait>();

    // ★ 宣告資料變更的純 C# 事件 (回傳當前比例 0~1)
    public event Action<float> OnHealthRatioChanged;
    
    // ==========================================
    // 核心生命週期與方法
    // ==========================================
    public virtual void Initialize(int maxHP, int atk, int def)
    {
        maxHealth = maxHP;
        currentHealth = maxHP;
        attackPower = atk;
        defense = def;

        // 通知所有特徵進行初始化
        foreach (var trait in traits)
        {
            trait.Initialize();
        }
    }

    public void ModifyHealth(int delta)
    {
        currentHealth = Mathf.Clamp(currentHealth + delta, 0, maxHealth);
        
        // ★ 當血量改變時，通知所有正在監看這份 SO 的 UI！
        float ratio = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
        OnHealthRatioChanged?.Invoke(ratio);
    }

    // ==========================================
    // 特徵查詢介面 (極限解耦的核心)
    // ==========================================
    public T GetTrait<T>() where T : class, IEntityTrait
    {
        foreach (var trait in traits)
        {
            if (trait is T match) return match;
        }
        return null;
    }

    // ★ 新增：TryGet 模式，將「查詢」與「判定」合而為一
    public bool TryGetTrait<T>(out T trait) where T : class, IEntityTrait
    {
        trait = GetTrait<T>();
        return trait != null;
    }
}

