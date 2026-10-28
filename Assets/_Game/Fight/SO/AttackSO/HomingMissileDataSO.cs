using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Homing Missile Data", menuName = "Combat/Homing Missile Data")]
public class HomingMissileDataSO : AttackDataSO
{
    public enum LaunchMode { 
        AngleSpread, // 傳統的給定一個角度往外射
        BezierArc    // 完美的貝茲曲線外拋
    }

    [Header("★ 障礙物與撞牆判定")]
    [Tooltip("導彈撞到這些 Layer (例如牆壁、地板) 會直接引爆，但不會算傷害")]
    public LayerMask obstacleLayer;
    
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
    
    [Header("★ 導彈專屬設定")]
    public bool showTrajectory = true; // 專屬於導彈的開關
    
    [Header("★ 預測線設定")]
    [Tooltip("雷射預測線要畫多長 (公尺)")]
    public float previewDistance = 25f; // 加上這個！

    public override Vector3[] CalculatePredictedTrajectory(Vector3 startPos, Vector3 direction)
    {
        // 如果企劃沒打勾，直接回傳空陣列
        if (!showTrajectory) return new Vector3[0]; 

        List<Vector3> points = new List<Vector3>();
        int resolution = 15; 
        
        // ★ 核心修正：把牆壁與玩家的 Layer 加起來，用作軌跡阻擋判定
        LayerMask blockMask = obstacleLayer | damageLayer;

        if (launchMode == LaunchMode.BezierArc)
        {
            Vector3 p0 = startPos;
            Vector3 p2 = p0 + direction.normalized * previewDistance; 
            
            Vector3 midPoint = (p0 + p2) / 2f;
            Vector3 perp = new Vector3(-direction.y, direction.x, 0f).normalized;
            Vector3 p1 = midPoint + perp * bezierArcOffset;

            points.Add(p0);
            Vector3 lastPoint = p0;

            for (int i = 1; i <= resolution; i++)
            {
                float t = i / (float)resolution;
                float u = 1f - t;
                Vector3 currentPoint = (u * u * p0) + (2f * u * t * p1) + (t * t * p2);
                currentPoint.z = 0f;

                // ★ 檢查：從上一個點到這一個點的線段中，有沒有撞到牆壁？
                Vector3 segmentDir = currentPoint - lastPoint;
                float segmentDist = segmentDir.magnitude;

                if (Physics.Raycast(lastPoint, segmentDir.normalized, out RaycastHit hit, segmentDist, blockMask))
                {
                    // 撞到了！把點設定在牆壁上，然後直接中斷運算 (不畫後面的線了)
                    points.Add(hit.point);
                    break; 
                }

                points.Add(currentPoint);
                lastPoint = currentPoint;
            }
        }
        else
        {
            // --- 直線模式 ---
            points.Add(startPos);
            Vector3 targetPoint = startPos + direction.normalized * previewDistance;

            // ★ 檢查：直直射過去會不會撞到牆壁？
            if (Physics.Raycast(startPos, direction.normalized, out RaycastHit hit, previewDistance, blockMask))
            {
                points.Add(hit.point); // 撞到了，線就畫到撞擊點為止
            }
            else
            {
                points.Add(targetPoint); // 沒撞到，畫好畫滿
            }
        }

        return points.ToArray();
    }
}