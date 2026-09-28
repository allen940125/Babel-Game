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

    [Header("★ 自動運算")]
    [Tooltip("勾選後，TrajectoryVisualizer 會在 Update 中自行計算並更新預測線。")]
    public bool autoUpdateTrajectory = false;

    private LineRenderer _lineRenderer;
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[16];

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        SetupLineRenderer();
    }

    private void Update()
    {
        if (!autoUpdateTrajectory) return;

        // ★ 自己取得目前的位置與方向
        Vector3 startPos = transform.position;
        Vector3 direction = transform.up;

        // ★ 自己啟動預測
        DrawTrajectory(startPos, direction, null);
    }

    private void SetupLineRenderer()
    {
        if (_lineRenderer == null) return;

        _lineRenderer.useWorldSpace = true;
        _lineRenderer.alignment = LineAlignment.View;

        _lineRenderer.startWidth = lineWidth;
        _lineRenderer.endWidth = lineWidth;

        if (lineMaterial != null)
            _lineRenderer.sharedMaterial = lineMaterial;
        else if (_lineRenderer.sharedMaterial == null)
            _lineRenderer.sharedMaterial =
                new Material(Shader.Find("Sprites/Default"));

        _lineRenderer.startColor = lineColor;
        _lineRenderer.endColor = lineColor;
        _lineRenderer.sortingOrder = 10;
    }

    public void DrawTrajectory(Vector3 startPos, Vector3 direction, float[] bounceJitters = null)
    {
        if (_lineRenderer == null || bulletData == null) return;

        _lineRenderer.enabled = true;

        startPos.z = 0f;
        direction.z = 0f;

        Vector3 currentPos = startPos;
        Vector3 currentDir = direction.normalized;
        
        // ★ 核心修正 1：必須紀錄「還剩下多少距離可以畫」
        float remainingDistance = bulletData.maxPredictionDistance;

        List<Vector3> points = new List<Vector3>();
        points.Add(currentPos);

        // ★ 核心修正 2：恢復迴圈，讓畫線次數與 bulletData.maxBounces 絕對同步
        for (int i = 0; i <= bulletData.maxBounces; i++)
        {
            if (remainingDistance <= 0.0001f) break;

            int hitCount = ShapeCastUtility.PerformCast(
                currentPos,
                currentDir,
                remainingDistance, // 只掃描剩下的距離
                _hitBuffer,
                bulletData.bounceShape,
                bulletData.bounceLayer,
                false
            );

            RaycastHit nearestHit = default;
            bool foundWall = false;

            if (hitCount > 0)
            {
                System.Array.Sort(_hitBuffer, 0, hitCount, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));

                for (int j = 0; j < hitCount; j++)
                {
                    RaycastHit hit = _hitBuffer[j];
                    if (hit.collider == null || hit.distance <= 0.0001f) continue;

                    nearestHit = hit;
                    foundWall = true;
                    break;
                }
            }

            // 沒撞到牆：把剩下的距離全部畫完，結束運算
            if (!foundWall)
            {
                Vector3 endPoint = currentPos + currentDir * remainingDistance;
                endPoint.z = 0f;
                points.Add(endPoint);
                break;
            }

            // 撞到牆：記錄碰撞點
            Vector3 hitPoint = currentPos + currentDir * nearestHit.distance;
            hitPoint.z = 0f;
            points.Add(hitPoint);

            // ★ 核心修正 3：扣除已經畫過的距離
            remainingDistance -= nearestHit.distance;

            // 計算反射與法線防呆
            Vector3 flatNormal = new Vector3(nearestHit.normal.x, nearestHit.normal.y, 0f).normalized;
            if (flatNormal.sqrMagnitude < 0.0001f) flatNormal = -currentDir;

            Vector3 reflection = Vector3.Reflect(currentDir, flatNormal).normalized;
            float jitter = (bounceJitters != null && i < bounceJitters.Length) ? bounceJitters[i] : 0f;

            if (jitter != 0f)
            {
                reflection = Quaternion.Euler(0f, 0f, jitter) * reflection;
                reflection.z = 0f;
                reflection.Normalize();
                if (Vector3.Dot(reflection, flatNormal) <= 0.087f)
                {
                    reflection = Vector3.Reflect(currentDir, flatNormal).normalized;
                }
            }

            // ★ 核心修正 4：下一個起點沿著法線往外推，避免下一圈掃描卡進牆內
            currentPos = hitPoint + flatNormal * 0.01f;
            currentDir = reflection;
        }

        _lineRenderer.positionCount = points.Count;
        _lineRenderer.SetPositions(points.ToArray());
    }
}