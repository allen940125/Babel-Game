using UnityEngine;

// ==========================================
// 1. 核心攻擊基類 (所有攻擊型態的公約數)
// ★ 絕對不要加上 [CreateAssetMenu]，它不能被單獨實例化
// ==========================================
using UnityEngine;

public abstract class AttackDataSO : ScriptableObject
{
    [Header("★ 核心傷害設定")]
    [Tooltip("基礎傷害倍率 (最終傷害 = Entity攻擊力 * damageMultiplier)")]
    public float damageMultiplier = 1.0f;
    
    [Tooltip("勾選後，此傷害發送時將帶有無視無敵的標記")]
    public bool ignoreInvincibility = false;
    
    [Header("★ 持續穿透傷害 (Tick)")]
    public bool useDamageTick = false;
    public float damageTickInterval = 0.25f;
    
    [Header("★ 目標與形狀判定")]
    public LayerMask damageLayer;
    public CollisionShapeConfig damageShape; // ★ 統一移至此處

    [Header("★ 通用特效")]
    public GameObject hitEffectPrefab;
    
    [Header("★ 撞牆震動回饋")]
    public bool enableCameraShake = true;
    public float shakeIntensity = 0.15f;
    public float shakeDuration = 0.1f;
}