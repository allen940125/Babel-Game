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

    public void DrawTrajectory(
        Vector3 startPos,
        Vector3 direction,
        float[] bounceJitters = null)
    {
        if (_lineRenderer == null || bulletData == null) return;

        _lineRenderer.enabled = true;

        startPos.z = 0f;
        direction.z = 0f;

        List<Vector3> points = new List<Vector3>();
        points.Add(startPos);

        Vector3 currentPos = startPos;
        Vector3 currentDir = direction.normalized;
        float remainingDistance = bulletData.maxPredictionDistance;

        for (int i = 0; i <= bulletData.maxBounces; i++)
        {
            if (remainingDistance <= 0) break;

            int hitCount = ShapeCastUtility.PerformCast(
                currentPos,
                currentDir,
                remainingDistance,
                _hitBuffer,
                bulletData.shapeConfig,
                bulletData.bounceLayer,
                false);

            if (hitCount > 0)
            {
                System.Array.Sort(
                    _hitBuffer,
                    0,
                    hitCount,
                    Comparer<RaycastHit>.Create(
                        (a, b) => a.distance.CompareTo(b.distance)));

                bool bounced = false;

                for (int j = 0; j < hitCount; j++)
                {
                    RaycastHit hit = _hitBuffer[j];

                    if (hit.collider == null ||
                        hit.distance <= 0.0001f ||
                        hit.point == Vector3.zero)
                        continue;

                    string tag = hit.collider.tag;

                    if (tag == "Player")
                        continue;

                    if (tag == "Wall")
                    {
                        Vector3 hitPoint = hit.point;
                        hitPoint.z = 0f;

                        points.Add(hitPoint);

                        remainingDistance -= hit.distance;

                        Vector3 flatNormal =
                            new Vector3(
                                hit.normal.x,
                                hit.normal.y,
                                0f).normalized;

                        Vector3 pureReflection =
                            Vector3.Reflect(currentDir, flatNormal);

                        pureReflection.z = 0f;
                        pureReflection.Normalize();

                        float jitter =
                            (bounceJitters != null &&
                             i < bounceJitters.Length)
                                ? bounceJitters[i]
                                : 0f;

                        if (jitter != 0f)
                        {
                            currentDir =
                                Quaternion.Euler(0, 0, jitter) *
                                pureReflection;

                            if (Vector3.Dot(
                                    currentDir,
                                    flatNormal) <= 0.087f)
                            {
                                currentDir = pureReflection;
                            }
                        }
                        else
                        {
                            currentDir = pureReflection;
                        }

                        currentPos =
                            hitPoint + currentDir * 0.01f;

                        bounced = true;
                        break;
                    }
                    else
                    {
                        Vector3 endPoint = hit.point;
                        endPoint.z = 0f;

                        points.Add(endPoint);

                        bounced = true;
                        remainingDistance = 0;
                        break;
                    }
                }

                if (!bounced)
                {
                    Vector3 endPoint =
                        currentPos +
                        currentDir * remainingDistance;

                    endPoint.z = 0f;
                    points.Add(endPoint);
                    break;
                }
            }
            else
            {
                Vector3 endPoint =
                    currentPos +
                    currentDir * remainingDistance;

                endPoint.z = 0f;
                points.Add(endPoint);
                break;
            }
        }

        _lineRenderer.positionCount = points.Count;
        _lineRenderer.SetPositions(points.ToArray());
    }
}