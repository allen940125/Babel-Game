using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryVisualizer : MonoBehaviour
{
    [Header("★ 核心資料來源 (必填)")]
    public BulletDataSO bulletData;

    [Header("★ 視覺設定")]
    public float lineWidth = 0.05f;
    public Material lineMaterial;
    public Color lineColor = Color.red;

    // ==========================================
    // ★ 新增：獨立運作模式 (Standalone Mode)
    // ==========================================
    [Header("★ 獨立運作模式 (供靜態機關或除錯使用)")]
    [Tooltip("勾選後，此腳本將每幀主動依照自身的 Transform 發射射線。若由 Spawner 或 Bullet 控制，請保持關閉！")]
    public bool autoUpdate = false;
    
    private LineRenderer _lineRenderer;
    private RaycastHit[] _hitBuffer = new RaycastHit[5];
    
    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        SetupLineRenderer();
    }
    
    // ★ 新增：讓腳本具備主動執行的能力
    private void Update()
    {
        if (autoUpdate)
        {
            // 獨立模式下，強制使用自己的位置與正上方(Y軸)作為參數
            DrawTrajectory(transform.position, transform.up, null);
        }
    }

    private void SetupLineRenderer()
    {
        _lineRenderer.useWorldSpace = true;
        // ★ 嚴格修正：對於 2.5D 遊戲，線條必須永遠面向攝影機 (View)
        // 絕對不能使用 TransformZ，否則父物件旋轉時線條會跟著麻花般扭曲
        _lineRenderer.alignment = LineAlignment.View; 
        
        _lineRenderer.startWidth = lineWidth; 
        _lineRenderer.endWidth = lineWidth;
        if (lineMaterial != null) _lineRenderer.sharedMaterial = lineMaterial;
        _lineRenderer.startColor = lineColor;
        _lineRenderer.endColor = lineColor;
        _lineRenderer.sortingOrder = 10; 
    }

    public void DrawTrajectory(Vector3 startPos, Vector3 direction, float[] bounceJitters = null)
    {
        if (_lineRenderer == null || bulletData == null) return;

        List<Vector3> points = new List<Vector3>();
        points.Add(startPos);

        Vector3 currentPos = startPos;
        Vector3 currentDir = direction.normalized;
        float remainingDistance = bulletData.maxPredictionDistance;

        for (int i = 0; i < bulletData.maxBounces + 1; i++)
        {
            int hitCount = ShapeCastUtility.PerformCast(currentPos, currentDir, remainingDistance, _hitBuffer, bulletData.shapeConfig, bulletData.bounceLayer, false);
            
            if (hitCount > 0)
            {
                System.Array.Sort(_hitBuffer, 0, hitCount, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));
                RaycastHit hit = _hitBuffer[0];

                if (hit.collider != null && hit.distance > 0.001f)
                {
                    points.Add(hit.point);
                    currentPos = hit.point;
                    currentDir = Vector3.Reflect(currentDir, hit.normal);
                    currentDir.z = 0; 
                    currentDir.Normalize();
                    remainingDistance -= hit.distance;
                    continue;
                }
            }
            
            points.Add(currentPos + currentDir * remainingDistance);
            break;
        }

        _lineRenderer.positionCount = points.Count;
        _lineRenderer.SetPositions(points.ToArray());
    }
    
    // ==========================================
    // ★ 新增：編輯器即時預覽 (Editor Gizmos)
    // 讓你在 Scene 視窗擺放 Prefab 時，不用按 Play 就能看到紅線怎麼折射
    // ==========================================
    private void OnDrawGizmosSelected()
    {
        if (bulletData == null) return;
        
        // 為了在 Editor 畫線，我們直接利用現有的演算法，但改用 Gizmos.DrawLine
        Gizmos.color = lineColor;
        
        Vector3 currentPos = transform.position;
        
        // 如果你有加入 override 角度，這裡也要算進去
        Vector3 currentDir = transform.forward; 
        // if (angleOffset != 0f) currentDir = Quaternion.Euler(0, 0, angleOffset) * currentDir;
        
        // 套用覆寫長度與次數
        // float remainingDistance = overrideLength ? customLength : bulletData.maxPredictionDistance;
        // int bounces = overrideBounces ? customMaxBounces : bulletData.maxBounces;
        
        // 這裡示範讀取基礎 SO 資料 (若有 override 請自行替換變數)
        float remainingDistance = bulletData.maxPredictionDistance;
        int bounces = bulletData.maxBounces;

        for (int i = 0; i < bounces + 1; i++)
        {
            int hitCount = ShapeCastUtility.PerformCast(currentPos, currentDir, remainingDistance, _hitBuffer, bulletData.shapeConfig, bulletData.bounceLayer, false);
            
            if (hitCount > 0)
            {
                System.Array.Sort(_hitBuffer, 0, hitCount, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));
                RaycastHit hit = _hitBuffer[0];

                if (hit.collider != null && hit.distance > 0.001f)
                {
                    Gizmos.DrawLine(currentPos, hit.point); // 畫出這一段
                    currentPos = hit.point;
                    
                    Vector3 pureReflection = Vector3.Reflect(currentDir, hit.normal);
                    pureReflection.z = 0; 
                    pureReflection.Normalize();
                    currentDir = pureReflection.normalized;
                    
                    remainingDistance -= hit.distance;
                    continue;
                }
            }
            
            // 畫出最後一段沒撞到東西的線
            Gizmos.DrawLine(currentPos, currentPos + currentDir * remainingDistance);
            break;
        }
    }
}