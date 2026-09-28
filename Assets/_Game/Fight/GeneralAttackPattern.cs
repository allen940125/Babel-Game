using UnityEngine;
using System.Collections.Generic;

public class GeneralAttackPattern : BulletSpawnerBase
{
    public enum PatternType
    {
        Circle,      // 環狀全方位發射
        Shotgun,     // 針對特定角度的扇形散射
        Sniper,      // 鎖定玩家方向發射 (可帶些微誤差)
        RandomSpray, // 指定範圍內隨機亂射
        RandomRain,  // 範圍內隨機生成並全部垂直向下
        LinearLine,  // 橫向排成一列發射
        Simple,       // 單發固定方向
        BoxPerimeter // ★ 新增：方形外框邊緣
    }

    [Header("攻擊模式選擇")]
    public PatternType patternType = PatternType.Circle;

    [Header("彈幕數量")]
    public int bulletCount = 20;

    [Header("生成範圍設定 (適用: RandomSpray, RandomRain, LinearLine)")]
    public Vector2 spawnAreaSize = new Vector2(5, 5);

    [Header("圓形專用參數 (適用: Circle)")]
    [Tooltip("圓形半徑 (0 = 都在同一個點重疊發射, >0 = 在圓周線上發射)")]
    public float circleRadius = 0f;
    [Tooltip("起始旋轉角度")]
    [Range(0, 360)] public float circleStartAngle = 0f;

    [Header("扇形專用參數 (適用: Shotgun)")]
    public float spreadAngle = 90f;

    [Header("狙擊專用參數 (適用: Sniper)")]
    [Tooltip("瞄準玩家時的隨機誤差角度 (±值)")]
    public float sniperSpreadAngle = 5f;

    [Header("方向控制 (適用: Simple, RandomSpray, Shotgun, Sniper)")]
    public bool useRandomDirection = true;
    public bool aimAtPlayer = false;
    [Range(0, 360)] public float fixedAngle = 270f;
    
    [Header("方形邊緣專用參數 (適用: BoxPerimeter)")]
    [Tooltip("預設為向外圍空曠處發射，勾選此項則改為向中心點集中射擊")]
    public bool fireInwardToCenter = false;

    // ==========================================
    // ★ 核心：實作 BulletSpawnerBase 唯一的抽象方法
    //    只算數學，不 Instantiate、不 StartCoroutine
    // ==========================================
    protected override List<SpawnData> CalculateAllSpawnData()
    {
        List<SpawnData> list = new List<SpawnData>();

        switch (patternType)
        {
            // ---------- 環狀 ----------
            case PatternType.Circle:
            {
                float angleStep = 360f / bulletCount;
                for (int i = 0; i < bulletCount; i++)
                {
                    float currentAngle = circleStartAngle + (i * angleStep);
                    Vector2 dir = AngleToVector(currentAngle);
                    Vector2 pos = transform.position;
                    if (circleRadius > 0) pos += dir * circleRadius;
                    list.Add(new SpawnData { position = pos, direction = dir });
                }
                break;
            }

            // ---------- 扇形 ----------
            case PatternType.Shotgun:
            {
                Vector2 baseDir = GetDesiredDirection();
                float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
                float startAngle = baseAngle - (spreadAngle / 2f);
                float step = (bulletCount > 1) ? spreadAngle / (bulletCount - 1) : 0f;
                for (int i = 0; i < bulletCount; i++)
                {
                    float currentAngle = startAngle + (step * i);
                    list.Add(new SpawnData { position = transform.position, direction = AngleToVector(currentAngle) });
                }
                break;
            }

            // ---------- 單發固定方向 ----------
            case PatternType.Simple:
            {
                Vector2 dir = GetDesiredDirection();
                for (int i = 0; i < bulletCount; i++)
                    list.Add(new SpawnData { position = transform.position, direction = dir });
                break;
            }

            // ---------- 狙擊 ----------
            case PatternType.Sniper:
            {
                Vector2 targetDir = GetDirToPlayer();
                float baseAngle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;
                for (int i = 0; i < bulletCount; i++)
                {
                    float offset = Random.Range(-sniperSpreadAngle, sniperSpreadAngle);
                    list.Add(new SpawnData { position = transform.position, direction = AngleToVector(baseAngle + offset) });
                }
                break;
            }

            // ---------- 隨機亂射 ----------
            case PatternType.RandomSpray:
            {
                for (int i = 0; i < bulletCount; i++)
                {
                    Vector2 dir = useRandomDirection ? Random.insideUnitCircle.normalized : GetDesiredDirection();
                    list.Add(new SpawnData { position = GetRandomSpawnPos(), direction = dir });
                }
                break;
            }

            // ---------- 隨機雨 ----------
            case PatternType.RandomRain:
            {
                for (int i = 0; i < bulletCount; i++)
                    list.Add(new SpawnData { position = GetRandomSpawnPos(), direction = Vector2.down });
                break;
            }

            // ---------- 橫向排成一列 ----------
            case PatternType.LinearLine:
            {
                // 以 transform.position 為絕對中心，往左右 (X軸) 兩側延伸出線段
                Vector2 startPos = (Vector2)transform.position - new Vector2(spawnAreaSize.x / 2f, 0);
                Vector2 endPos   = (Vector2)transform.position + new Vector2(spawnAreaSize.x / 2f, 0);

                for (int i = 0; i < bulletCount; i++)
                {
                    // 1. 計算生成點位置
                    float t = (bulletCount > 1) ? (float)i / (bulletCount - 1) : 0.5f;
                    Vector2 spawnPos = Vector2.Lerp(startPos, endPos, t);
        
                    // 加上 Y 軸的隨機抖動
                    spawnPos.y += Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);

                    // 2. 方向判定邏輯 (與 BoxPerimeter 標準化)
                    Vector2 dir;
                    if (aimAtPlayer) 
                    {
                        dir = GetDirToPlayer();
                    }
                    else if (useRandomDirection) 
                    {
                        dir = Random.insideUnitCircle.normalized;
                    }
                    else 
                    {
                        // 如果你想讓他往「遠離中心」的方向射擊 (向外輻射)
                        // 將生成點減去中心點 (transform.position) 即可得到向外的向量
                        // 若兩點重合，則退回預設的 fixedAngle
                        Vector2 offsetFromCenter = spawnPos - (Vector2)transform.position;
            
                        if (offsetFromCenter != Vector2.zero) 
                        {
                            dir = offsetFromCenter.normalized;
                            // 若你有加 fireInwardToCenter 開關，這裡同樣可以反轉向量
                            // if (fireInwardToCenter) dir = -dir; 
                        }
                        else
                        {
                            dir = AngleToVector(fixedAngle);
                        }
                    }

                    list.Add(new SpawnData { position = spawnPos, direction = dir });
                }
                break;
            }
            
            case PatternType.BoxPerimeter:
            {
                float w = spawnAreaSize.x;
                float h = spawnAreaSize.y;
                float perimeter = 2f * (w + h);     // 算出矩形總周長
                float step = perimeter / bulletCount; // 每顆子彈分配到的長度間距

                for (int i = 0; i < bulletCount; i++)
                {
                    float d = i * step; // 這顆子彈在周長上的距離落點
                    Vector2 localPos = Vector2.zero;

                    // 依據距離 d，判斷它落在四個邊的哪一條，逆時針繞一圈
                    if (d < w) 
                    {
                        // 下邊 (從左下到右下)
                        localPos = new Vector2(-w / 2f + d, -h / 2f);
                    }
                    else if (d < w + h) 
                    {
                        // 右邊 (從右下到右上)
                        localPos = new Vector2(w / 2f, -h / 2f + (d - w));
                    }
                    else if (d < 2 * w + h) 
                    {
                        // 上邊 (從右上到左上)
                        localPos = new Vector2(w / 2f - (d - w - h), h / 2f);
                    }
                    else 
                    {
                        // 左邊 (從左上到左下)
                        localPos = new Vector2(-w / 2f, h / 2f - (d - 2 * w - h));
                    }

                    Vector2 worldPos = (Vector2)transform.position + localPos;
        
                    // --- 方向判定邏輯 ---
                    Vector2 dir;
                    if (aimAtPlayer) 
                    {
                        dir = GetDirToPlayer();
                    }
                    else if (useRandomDirection) 
                    {
                        dir = Random.insideUnitCircle.normalized;
                    }
                    else if (localPos != Vector2.zero)
                    {
                        // 如果沒有開特殊鎖定，預設往外輻射 (空曠處)
                        dir = localPos.normalized; 
                        // 如果勾選了向內集中，則反轉向量，朝向中心點
                        if (fireInwardToCenter) dir = -dir; 
                    }
                    else
                    {
                        dir = AngleToVector(fixedAngle);
                    }

                    list.Add(new SpawnData { position = worldPos, direction = dir });
                }
                break;
            }
        }

        return list;
    }

    // ==========================================
    // Debug 測試：直接呼叫基底 Execute 即可
    // ==========================================
    [ContextMenu("👉 測試發射 (Debug Test)")]
    public void DebugTest()
    {
        if (!Application.isPlaying) { Debug.LogError("⛔ 請先按 Play！"); return; }
        // 走完整流程（含預警、逐發、結束）
        Execute(null, 1f, false);
    }

    // ==========================================
    // ★ 編輯器預覽：複用 CalculateAllSpawnData
    //    保證「預覽」與「實戰」用的是同一份數學！
    // ==========================================
    protected override void OnGeneratePreview(Transform previewContainer)
    {
        var dataList = CalculateAllSpawnData();
        foreach (var data in dataList)
        {
            CreatePreviewDummy(previewContainer, data.position, data.direction);
        }
        Debug.Log($"<color=cyan>[預覽成功]</color> 共 {dataList.Count} 顆預覽假彈。");
    }

    // ==========================================
    // 輔助方法
    // ==========================================
    private Vector2 GetDesiredDirection()
    {
        return aimAtPlayer ? GetDirToPlayer() : AngleToVector(fixedAngle);
    }

    private Vector2 AngleToVector(float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    private Vector2 GetRandomSpawnPos()
    {
        float x = Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
        float y = Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
        return (Vector2)transform.position + (Vector2)transform.right * x + (Vector2)transform.up * y;
    }

    private Vector2 GetDirToPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) return (player.transform.position - transform.position).normalized;
        return Vector2.down;
    }

    // ==========================================
    // Gizmos
    // ==========================================
    private void OnDrawGizmosSelected()
    {
        // 方形生成範圍
        Gizmos.color = Color.green;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 0));

        // 圓形範圍
        if (patternType == PatternType.Circle && circleRadius > 0)
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireSphere(transform.position, circleRadius);
        }

        // 扇形指示線
        if (patternType == PatternType.Shotgun)
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = Matrix4x4.identity;
            Vector2 baseDir = aimAtPlayer ? Vector2.down : AngleToVector(fixedAngle);
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
            float half = spreadAngle / 2f;
            Vector3 left  = AngleToVector(baseAngle - half) * 2f;
            Vector3 right = AngleToVector(baseAngle + half) * 2f;
            Gizmos.DrawLine(transform.position, transform.position + left);
            Gizmos.DrawLine(transform.position, transform.position + right);
        }

        // 固定方向指示線
        if (patternType == PatternType.Simple || patternType == PatternType.Sniper)
        {
            Gizmos.color = Color.magenta;
            Gizmos.matrix = Matrix4x4.identity;
            Vector3 dir = aimAtPlayer ? Vector3.down : (Vector3)AngleToVector(fixedAngle);
            Gizmos.DrawLine(transform.position, transform.position + dir * 2f);
        }
        
        // (在原本的 OnDrawGizmosSelected 裡面)
        if (patternType == PatternType.RandomSpray || patternType == PatternType.RandomRain || patternType == PatternType.LinearLine || patternType == PatternType.BoxPerimeter)
        {
            Gizmos.color = Color.green;
            Matrix4x4 rotationMatrix_ = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.matrix = rotationMatrix_;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 0));
        }
    }
}