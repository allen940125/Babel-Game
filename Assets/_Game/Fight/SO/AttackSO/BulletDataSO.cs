using UnityEngine;

// ==========================================
// 2. 飛行子彈專屬資料 (具備物理移動、反彈、碰撞形狀)
// ==========================================
[CreateAssetMenu(fileName = "New Bullet Data", menuName = "Combat/Bullet Data")]
public class BulletDataSO : AttackDataSO
{
    [Header("★ 反彈判定 (對牆壁)")]
    [Tooltip("通常與視覺圖像等大，確保不會穿模")]
    public CollisionShapeConfig bounceShape;
    public LayerMask bounceLayer;
    
    [Header("★ 飛行與反彈行為")]
    public float baseSpeed = 10f;
    public int maxBounces = 3;
    public float maxPredictionDistance = 50f;

    [Header("★ 反彈擾動 (Jitter)")]
    [Range(0f, 60f)] public float maxBounceAngleJitter = 0f;
    public bool jitterFirstBounceOnly = false;
    
    [Header("★ 撞牆震動回饋")]
    public bool enableWallHitShake = true;
    public float wallHitShakeIntensity = 0.15f;
    public float wallHitShakeDuration = 0.1f;
}