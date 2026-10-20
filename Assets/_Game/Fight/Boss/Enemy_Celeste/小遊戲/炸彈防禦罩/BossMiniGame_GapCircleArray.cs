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
    
    // ★ 核心改動：使用 Vector2 取代原本的 float
    [Header("★ 隨機或固定設定 (X=最小, Y=最大)")]
    [Tooltip("缺口的角度大小。若要固定大小，將 X 與 Y 設為相同數值 (例如 X=60, Y=60)")]
    public Vector2 gapAngleRange = new Vector2(60f, 60f);
    
    [Tooltip("缺口的朝向角度。若要隨機方向，設為 X=0, Y=360。固定朝上則設為 X=90, Y=90")]
    public Vector2 gapDirectionRange = new Vector2(0f, 360f);

    [Header("★ 子物件行為設定")]
    public bool lookAtCenter = true;

    private List<GameObject> _spawnedNodes = new List<GameObject>();

    // 儲存這一次遊戲決定的實際數值
    private float _actualGapAngle;
    private float _actualGapDirection;

    private void Start()
    {
        // 遊戲開始時，骰一次隨機數並記下來
        _actualGapAngle = Random.Range(gapAngleRange.x, gapAngleRange.y);
        _actualGapDirection = Random.Range(gapDirectionRange.x, gapDirectionRange.y);
        
        GenerateCircleWithGap();
    }

    [ContextMenu("重新生成陣列 (Debug)")]
    public void GenerateCircleWithGap()
    {
        ClearNodes();
        if (nodePrefab == null) return;

        Vector3 centerPos = (centerPoint != null) ? centerPoint.position : transform.position;

        // ★ 使用剛才決定好的實際數值
        // 如果是在 Editor 模式下點擊 [ContextMenu]，就預設拿 X 值來預覽
        float activeAngle = Application.isPlaying ? _actualGapAngle : gapAngleRange.x;
        float activeDirection = Application.isPlaying ? _actualGapDirection : gapDirectionRange.x;

        float activeAngleRange = 360f - activeAngle;
        float stepAngle = (nodeCount > 1) ? (activeAngleRange / (nodeCount - 1)) : 0f;
        float startAngle = activeDirection + (activeAngle / 2f);

        for (int i = 0; i < nodeCount; i++)
        {
            float currentAngle = startAngle + (i * stepAngle);
            float currentRad = currentAngle * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(Mathf.Cos(currentRad), Mathf.Sin(currentRad), 0f) * radius;
            Vector3 spawnPos = centerPos + offset;

            GameObject newNode = Instantiate(nodePrefab, spawnPos, Quaternion.identity, this.transform);

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

    public void ClearNodes()
    {
        foreach (var node in _spawnedNodes)
        {
            if (node != null) Destroy(node);
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

        Gizmos.color = Color.yellow;
        DrawGizmoCircle(centerPos, radius);

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        
        // Editor 預覽一律用 X (最小值) 作為基準，避免每幀 Random 導致畫面狂閃
        float previewAngle = gapAngleRange.x;
        float previewDirection = gapDirectionRange.x;

        float startAngle = previewDirection - (previewAngle / 2f);
        float endAngle = previewDirection + (previewAngle / 2f);
        
        Vector3 gapStartPos = centerPos + new Vector3(Mathf.Cos(startAngle * Mathf.Deg2Rad), Mathf.Sin(startAngle * Mathf.Deg2Rad), 0f) * radius;
        Vector3 gapEndPos = centerPos + new Vector3(Mathf.Cos(endAngle * Mathf.Deg2Rad), Mathf.Sin(endAngle * Mathf.Deg2Rad), 0f) * radius;
        
        Gizmos.DrawLine(centerPos, gapStartPos);
        Gizmos.DrawLine(centerPos, gapEndPos);
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