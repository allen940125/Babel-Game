using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryVisualizer : MonoBehaviour
{
    // ★ 刪除這行！它不再依賴具體的 SO 了！
    // public BulletDataSO bulletData; 

    [Header("★ 視覺設定")]
    public float lineWidth = 0.05f;
    public Material lineMaterial;
    public Color lineColor = Color.red;

    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        SetupLineRenderer();
    }

    private void SetupLineRenderer()
    {
        if (_lineRenderer == null) return;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.alignment = LineAlignment.View;
        _lineRenderer.startWidth = lineWidth;
        _lineRenderer.endWidth = lineWidth;
        if (lineMaterial != null) _lineRenderer.sharedMaterial = lineMaterial;
        _lineRenderer.startColor = lineColor;
        _lineRenderer.endColor = lineColor;
        _lineRenderer.sortingOrder = 10;
    }

    // ★ 笨蛋畫筆：只負責畫點
    public void DrawTrajectory(Vector3[] points)
    {
        // ★ 收到空陣列，或是根本沒點，就關閉畫線器！
        if (_lineRenderer == null || points == null || points.Length == 0) 
        {
            if (_lineRenderer != null) _lineRenderer.enabled = false;
            return;
        }

        // 有點，就正常畫
        _lineRenderer.enabled = true;
        _lineRenderer.positionCount = points.Length;
        _lineRenderer.SetPositions(points);
    }

    // 可以加一個清除的方法
    public void ClearTrajectory()
    {
        if (_lineRenderer != null) _lineRenderer.enabled = false;
    }
}