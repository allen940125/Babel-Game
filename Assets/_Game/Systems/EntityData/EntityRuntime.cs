using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEntityData", menuName = "Game/Runtime Data/Entity Blueprint")]
public class EntityBlueprintSO : ScriptableObject
{
    [Header("★ 絕對核心數據 (所有生物共用)")]
    public int maxHealth = 100;
    public int attackPower = 20;
    public int defense = 3;
    [Range(0f, 1f)] public float critRate = 0.2f;
    public float critMultiplier = 1.5f;

    [Header("★ 動態特徵插槽 (由企劃自由組合)")]
    [SerializeReference]
    public List<EntityTrait> traits = new List<EntityTrait>();
}

[Serializable]
public class EntityRuntime
{
    [Header("資料來源 (唯讀藍圖)")]
    public EntityBlueprintSO Blueprint;
    
    [Header("動態狀態 (執行期可見)")]
    [SerializeField] private int _currentHealth;

    // ==========================================
    // ★ 執行期修飾器 (Modifiers) - 用來裝載 Buff/Debuff
    // ==========================================
    [SerializeField] private int _bonusAttackPower = 0;
    [SerializeField] private int _bonusDefense = 0;
    // 如果要支援百分比加成，可以再加欄位如 _percentAttackPower = 0f;

    [SerializeReference] private List<EntityTrait> _runtimeTraits = new List<EntityTrait>();
    public event Action<float> OnHealthRatioChanged;

    // ==========================================
    // ★ 唯一對外開放的數值出口 (計算結果)
    // 外部腳本只能讀取這些 Property，絕對禁止直接讀 Blueprint
    // ==========================================
    public int CurrentHealth => _currentHealth;

    // 最終最大血量 = 藍圖血量 (此處可加入等級運算或隨機數值) + Buff血量
    public int MaxHealth => Blueprint.maxHealth; 
    
    // 最終攻擊力 = 藍圖攻擊力 + Buff加成
    public int TotalAttackPower => Blueprint.attackPower + _bonusAttackPower;
    
    // 最終防禦力 = 藍圖防禦力 + Buff加成
    public int TotalDefense => Blueprint.defense + _bonusDefense;

    public float TotalCritRate => Blueprint.critRate;
    public float TotalCritMultiplier => Blueprint.critMultiplier;

    public void Initialize(EntityBlueprintSO blueprint)
    {
        Blueprint = blueprint;
        
        // 這裡就是你處理「隨機評分」或「等級曲線」的最佳時機
        // 例如：_currentHealth = blueprint.maxHealth * levelCurveModifier;
        _currentHealth = MaxHealth;
        
        // 初始化時，Buff 歸零
        _bonusAttackPower = 0;
        _bonusDefense = 0;

        _runtimeTraits.Clear();
        foreach (var trait in blueprint.traits)
        {
            var instancedTrait = trait.Clone();
            instancedTrait.Initialize(this); 
            _runtimeTraits.Add(instancedTrait);
        }
    }

    // ==========================================
    // Buff 系統操作接口
    // ==========================================
    public void AddAttackBuff(int amount)
    {
        _bonusAttackPower += amount;
        Debug.Log($"獲得攻擊力 Buff，當前總攻擊力：{TotalAttackPower}");
    }

    public void RemoveAttackBuff(int amount)
    {
        _bonusAttackPower -= amount;
    }

    public void ModifyHealth(int delta)
    {
        _currentHealth = Mathf.Clamp(_currentHealth + delta, 0, MaxHealth);
        float ratio = MaxHealth > 0 ? (float)_currentHealth / MaxHealth : 0f;
        OnHealthRatioChanged?.Invoke(ratio);
    }

    // ==========================================
    // 特徵查詢介面 (向 _runtimeTraits 查，而不是向藍圖查)
    // ==========================================
    public T GetTrait<T>() where T : EntityTrait
    {
        foreach (var trait in _runtimeTraits)
        {
            if (trait is T match) return match;
        }
        return null;
    }

    public bool TryGetTrait<T>(out T trait) where T : EntityTrait
    {
        trait = GetTrait<T>();
        return trait != null;
    }
}