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
    if (_lineRenderer == null || bulletData == null)
        return;

    _lineRenderer.enabled = true;

    startPos.z = 0f;
    direction.z = 0f;

    Vector3 currentPos = startPos;
    Vector3 currentDir = direction.normalized;

    List<Vector3> points = new List<Vector3>();
    points.Add(currentPos);

    // ==================================================
    // 第一段：尋找第一次撞牆
    // ==================================================

    int hitCount = ShapeCastUtility.PerformCast(
        currentPos,
        currentDir,
        bulletData.maxPredictionDistance,
        _hitBuffer,
        bulletData.bounceShape,
        bulletData.bounceLayer,
        false
    );

    RaycastHit nearestHit = default;
    bool foundWall = false;

    if (hitCount > 0)
    {
        System.Array.Sort(
            _hitBuffer,
            0,
            hitCount,
            Comparer<RaycastHit>.Create(
                (a, b) => a.distance.CompareTo(b.distance)
            )
        );

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _hitBuffer[i];

            if (hit.collider == null)
                continue;

            if (hit.distance <= 0.0001f)
                continue;

            nearestHit = hit;
            foundWall = true;
            break;
        }
    }

    // ==================================================
    // 沒撞到牆：直接畫出去
    // ==================================================

    if (!foundWall)
    {
        Vector3 endPoint =
            currentPos +
            currentDir * bulletData.maxPredictionDistance;

        endPoint.z = 0f;
        points.Add(endPoint);

        _lineRenderer.positionCount = points.Count;
        _lineRenderer.SetPositions(points.ToArray());
        return;
    }

    // ==================================================
    // 第一次碰撞點
    // ==================================================

    Vector3 hitPoint =
        currentPos +
        currentDir * nearestHit.distance;

    hitPoint.z = 0f;

    points.Add(hitPoint);

    // ==================================================
    // 計算反射方向
    // ==================================================

    Vector3 flatNormal =
        new Vector3(
            nearestHit.normal.x,
            nearestHit.normal.y,
            0f
        ).normalized;

    if (flatNormal.sqrMagnitude < 0.0001f)
        flatNormal = -currentDir;

    Vector3 reflection =
        Vector3.Reflect(
            currentDir,
            flatNormal
        ).normalized;

    // ==================================================
    // 反彈擾動
    // ==================================================

    float jitter = 0f;

    if (bounceJitters != null &&
        bounceJitters.Length > 0)
    {
        jitter = bounceJitters[0];
    }

    if (jitter != 0f)
    {
        reflection =
            Quaternion.Euler(0f, 0f, jitter) *
            reflection;

        reflection.z = 0f;
        reflection.Normalize();

        // 避免擾動後又鑽回牆裡
        if (Vector3.Dot(reflection, flatNormal) <= 0.087f)
        {
            reflection =
                Vector3.Reflect(
                    currentDir,
                    flatNormal
                ).normalized;
        }
    }

    // ==================================================
    // 反彈後起點
    // ==================================================

    currentPos =
        hitPoint +
        flatNormal * 0.02f;

    currentDir = reflection;

    // ==================================================
    // 第二段：只畫反射後的路徑
    // ==================================================

    Vector3 secondEndPoint =
        currentPos +
        currentDir * bulletData.maxPredictionDistance;

    secondEndPoint.z = 0f;

    points.Add(secondEndPoint);

    // ==================================================
    // 輸出
    // ==================================================

    _lineRenderer.positionCount = points.Count;
    _lineRenderer.SetPositions(points.ToArray());
}
}