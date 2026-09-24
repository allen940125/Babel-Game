using UnityEngine;

[CreateAssetMenu(fileName = "New Bullet Data", menuName = "Boss/Bullet Data")]
public class BulletDataSO : ScriptableObject
{
    [Header("物理與碰撞形狀")]
    public CollisionShapeConfig shapeConfig;
    public LayerMask bounceLayer;
    
    [Header("行為設定")]
    public float baseSpeed = 10f;
    public int maxBounces = 3;
    public float maxPredictionDistance = 50f;

    // ★ 當你在 Inspector 調整 SO 檔案的任何數字時，強制觸發編輯器重繪
    private void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }
}