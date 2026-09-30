using UnityEngine;

[CreateAssetMenu(fileName = "New Bullet Data", menuName = "Boss/Bullet Data")]
public class BulletDataSO : ScriptableObject
{
    [Header("★ 傷害設定")]
    [Tooltip("基礎傷害倍率 (最終傷害 = Entity攻擊力 * damageMultiplier)")]
    public float damageMultiplier = 1.0f;

    [Header("★ 傷害判定 (對玩家)")]
    public CollisionShapeConfig damageShape;
    public LayerMask damageLayer;

    [Header("★ 反彈判定 (對牆壁)")]
    [Tooltip("通常與視覺圖像等大，確保不會穿模")]
    public CollisionShapeConfig bounceShape;
    public LayerMask bounceLayer;
    
    [Header("行為設定")]
    public float baseSpeed = 10f;
    public int maxBounces = 3;
    public float maxPredictionDistance = 50f;
    
    [Header("★ 撞牆螢幕震動")]
    public bool enableWallHitShake = true;
    public float wallHitShakeIntensity = 0.15f;
    public float wallHitShakeDuration = 0.1f;

    [Header("★ 反彈角度擾動 (Jitter)")]
    [Range(0f, 60f)] public float maxBounceAngleJitter = 0f;
    public bool jitterFirstBounceOnly = false;

    private void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }
}