using UnityEngine;

[CreateAssetMenu(fileName = "New Homing Missile Data", menuName = "Combat/Homing Missile Data")]
public class HomingMissileDataSO : AttackDataSO
{
    public enum LaunchMode { 
        AngleSpread, // 傳統的給定一個角度往外射
        BezierArc    // 完美的貝茲曲線外拋
    }

    [Header("★ 導彈飛行與追蹤參數")]
    public float speed = 8f;
    public float homingStrength = 100f; 
    public float lifeTime = 5f;
    
    [Tooltip("第一階段(外拋/貝茲)的時間。時間結束後進入第二階段(追蹤)")]
    public float homingDelay = 0.5f;    

    [Header("★ 第一階段：發射模式設定")]
    public LaunchMode launchMode = LaunchMode.AngleSpread;

    [Header("↳ 若選 AngleSpread (角度散射)")]
    public float initialArcAngle = 60f;
    public bool randomArcDirection = true;
    public bool arcToRight = true;

    [Header("↳ 若選 BezierArc (貝茲弧線包夾)")]
    [Tooltip("弧線向外擴展的距離。正數往右凸，負數往左凸")]
    public float bezierArcOffset = 5f;
    [Tooltip("是否隨機左右反轉，以形成 ( ) 的包夾網")]
    public bool randomBezierFlip = true;

    //[Header("★ 彈頭 (爆炸特效)")]
    //public GameObject payloadPrefab;
}