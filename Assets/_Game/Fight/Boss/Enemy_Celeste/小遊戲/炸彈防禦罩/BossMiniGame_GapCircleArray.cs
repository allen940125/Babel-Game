using System.Collections.Generic;
using UnityEngine;

public class BossMiniGame_GapCircleArray : MonoBehaviour
{
    [Header("★ 生成設定")]
    public GameObject nodePrefab;
    [Tooltip("圓形的中心點 (若不填，預設為本物件位置)")]
    public Transform centerPoint;

    [Header("★ 陣列幾何設定")]
    public float radius = 5f;
    public int nodeCount = 10;

    [Header("★ 缺口設定")]
    [Tooltip("缺口的角度大小。若要固定大小，將 X 與 Y 設為相同數值 (例如 X=60, Y=60)")]
    public Vector2 gapAngleRange = new Vector2(60f, 60f);

    [Tooltip("★ 禁區：缺口方向永遠不會落在這個角度區間。\n" +
             "可跨 0 度（例如 X=315, Y=45 表示 315°~360° 與 0°~45° 都不會是缺口方向）。\n" +
             "缺口方向會在禁區以外的角度隨機骰出。")]
    public Vector2 gapDirectionRange = new Vector2(315f, 45f);

    [Header("★ 子物件行為設定")]
    public bool lookAtCenter = true;

    private List<GameObject> _spawnedNodes = new List<GameObject>();
    private float _actualGapAngle;
    private float _actualGapDirection;
    private bool _hasGenerated = false;

    private void Start()
    {
        GenerateCircleWithGap();
    }

    [ContextMenu("重新生成陣列 (Debug)")]
    public void GenerateCircleWithGap()
    {
        ClearNodes();
        if (nodePrefab == null) return;

        _actualGapAngle = Random.Range(gapAngleRange.x, gapAngleRange.y);
        _actualGapDirection = RollDirectionOutsideExclusion();
        _hasGenerated = true;

        Vector3 centerPos = (centerPoint != null) ? centerPoint.position : transform.position;

        float activeAngleRange = 360f - _actualGapAngle;
        float stepAngle = (nodeCount > 1) ? (activeAngleRange / (nodeCount - 1)) : 0f;
        float startAngle = _actualGapDirection + (_actualGapAngle / 2f);

        for (int i = 0; i < nodeCount; i++)
        {
            float currentAngle = startAngle + (i * stepAngle);
            float currentRad = currentAngle * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(Mathf.Cos(currentRad), Mathf.Sin(currentRad), 0f) * radius;
            Vector3 spawnPos = centerPos + offset;

            GameObject newNode = Instantiate(nodePrefab, spawnPos, Quaternion.identity, this.transform);
            newNode.name = $"GapNode_{i}";

            if (lookAtCenter)
            {
                Vector3 directionToCenter = (centerPos - spawnPos).normalized;
                if (directionToCenter != Vector3.zero)
                {
                    float angleToCenter = Mathf.Atan2(directionToCenter.y, directionToCenter.x) * Mathf.Rad2Deg;
                    newNode.transform.rotation = Quaternion.Euler(0, 0, angleToCenter - 90f);
                }
            }

            _spawnedNodes.Add(newNode);
        }
    }

    // ★ 缺口方向在「禁區」之外隨機
    private float RollDirectionOutsideExclusion()
    {
        float rawStart = gapDirectionRange.x;
        float rawEnd = gapDirectionRange.y;

        // 邊界：整圈都禁（例如誤填 0~360）
        if (Mathf.Abs(rawEnd - rawStart) >= 359.99f)
        {
            Debug.LogWarning($"[{name}] gapDirectionRange 設為 {rawStart}~{rawEnd}，覆蓋整圈，退回禁區起點。");
            return NormalizeAngle(rawStart);
        }

        float start = NormalizeAngle(rawStart);
        float end = NormalizeAngle(rawEnd);
        bool wraps = start > end;  // 是否跨 0 度（例如 315 > 45）
        float excludedSize = wraps ? (360f - start + end) : (end - start);
        float allowedSize = 360f - excludedSize;

        if (allowedSize <= 0.001f)
        {
            Debug.LogWarning($"[{name}] gapDirectionRange 禁區覆蓋整圈，退回禁區起點。");
            return start;
        }

        // 允許區間 = 從禁區終點 (end) 沿正向走到禁區起點 (start)
        float roll = Random.Range(0f, allowedSize);
        return NormalizeAngle(end + roll);
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    public void ClearNodes()
    {
        foreach (var node in _spawnedNodes)
        {
            if (node == null) continue;
            if (Application.isPlaying)
                Destroy(node);
            else
                DestroyImmediate(node);
        }
        _spawnedNodes.Clear();
    }

    private void OnDestroy() => ClearNodes();

    // ==========================================
    // 編輯器預覽
    // ==========================================
    private void OnDrawGizmos()
    {
        Vector3 centerPos = (centerPoint != null) ? centerPoint.position : transform.position;

        // 黃色：圓形
        Gizmos.color = Color.yellow;
        DrawGizmoCircle(centerPos, radius);

        // 紅色：禁區扇形（缺口永遠不會覆蓋這裡）
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        DrawExclusionFan(centerPos, radius);

        // 綠色：實際缺口（已生成才顯示）
        if (_hasGenerated)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
            DrawGapBoundary(centerPos, _actualGapDirection, _actualGapAngle, radius);
        }
    }

    private void DrawExclusionFan(Vector3 center, float r)
    {
        float rawStart = gapDirectionRange.x;
        float rawEnd = gapDirectionRange.y;

        // 整圈禁區
        if (Mathf.Abs(rawEnd - rawStart) >= 359.99f)
        {
            DrawGizmoCircle(center, r);
            return;
        }

        float start = NormalizeAngle(rawStart);
        float end = NormalizeAngle(rawEnd);
        bool wraps = start > end;
        float span = wraps ? (360f - start + end) : (end - start);

        // 禁區只有一個點，不畫
        if (span <= 0.001f && !wraps) return;

        Vector3 pStart = center + AngleToDir(start) * r;
        Vector3 pEnd = center + AngleToDir(end) * r;
        Gizmos.DrawLine(center, pStart);
        Gizmos.DrawLine(center, pEnd);

        // 沿圓周連線
        int segments = Mathf.Max(8, Mathf.RoundToInt(span / 5f));
        Vector3 prev = pStart;
        for (int i = 1; i <= segments; i++)
        {
            float a = NormalizeAngle(start + (i * span / segments));
            Vector3 next = center + AngleToDir(a) * r;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }

    private void DrawGapBoundary(Vector3 center, float direction, float gapAngle, float r)
    {
        float startAngle = direction - (gapAngle / 2f);
        float endAngle = direction + (gapAngle / 2f);
        Gizmos.DrawLine(center, center + AngleToDir(startAngle) * r);
        Gizmos.DrawLine(center, center + AngleToDir(endAngle) * r);
    }

    private Vector3 AngleToDir(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
    }

    private void DrawGizmoCircle(Vector3 center, float r)
    {
        int segments = 36;
        float angleStep = 360f / segments;
        Vector3 prevPos = center + new Vector3(r, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float rad = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPos = center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * r;
            Gizmos.DrawLine(prevPos, newPos);
            prevPos = newPos;
        }
    }
}