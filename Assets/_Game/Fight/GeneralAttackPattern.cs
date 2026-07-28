using UnityEngine;
using System.Collections;

public class GeneralAttackPattern : AttackPatternBase
{
    public enum PatternType
    {
        Circle, Shotgun, Sniper, RandomSpray, RandomRain, LinearLine, Simple
    }

    [Header("攻擊模式選擇")]
    public PatternType patternType = PatternType.Circle;

    [Header("彈幕參數")]
    public GameObject bulletPrefab;
    public int bulletCount = 20; 
    public float baseSpeed = 5f;

    [Header("生成範圍設定")]
    public Vector2 spawnAreaSize = new Vector2(5, 5); 

    [Header("時間間隔設定")]
    [Tooltip("每顆子彈發射的間隔時間 (秒)")]
    public float spawnInterval = 0.05f; 

    // --- ★ 新增：圓形/螺旋專用參數 ---
    [Header("圓形/螺旋專用參數")]
    [Tooltip("圓形半徑 (0 = 從中心點發射, >0 = 從圓周上發射)")]
    public float circleRadius = 0f; 

    [Tooltip("起始角度偏移 (想從哪個角度開始轉?)")]
    [Range(0, 360)]
    public float circleStartAngle = 0f;

    [Header("扇形/散彈專用參數")]
    public float spreadAngle = 90f;

    [Header("方向控制 (Simple / RandomSpray)")]
    public bool useRandomDirection = true; 
    public bool aimAtPlayer = false;
    [Range(0, 360)]
    public float fixedAngle = 270f;

    // --- 右鍵測試 ---
    [ContextMenu("👉 測試發射 (Debug Test)")]
    public void DebugTest()
    {
        if (!Application.isPlaying) { Debug.LogError("⛔ 請先按 Play！"); return; }
        if (spawnInterval > 0f) StartCoroutine(FireRoutine(null, baseSpeed));
        else FireAllPatterns(null, baseSpeed);
    }

    // --- 實作父類別 ---
    protected override void OnExecute(BossBase boss, float speedMultiplier, bool isAngry)
    {
        float finalSpeed = baseSpeed * speedMultiplier;
        if (isAngry) finalSpeed *= 1.5f;

        // 這裡如果你希望圓形跟著 Boss 移動，就不要 SetParent(null)
        // 但如果你希望它是「原地設置一個法陣」，就要 SetParent(null)
        // 配合我們之前加的開關 (這裡假設你要獨立)
        transform.SetParent(null); 
        
        if (boss != null) boss.RegisterActivePattern(this.gameObject);

        if (spawnInterval > 0f) StartCoroutine(FireRoutine(boss, finalSpeed));
        else {
            FireAllPatterns(boss, finalSpeed);
            Destroy(gameObject, 0.1f);
        }
    }

    // --- Coroutine ---
    private IEnumerator FireRoutine(BossBase boss, float speed)
    {
        if (patternType == PatternType.LinearLine)
        {
            yield return StartCoroutine(FireLinearLineRoutine(boss, speed));
        }
        else
        {
            for (int i = 0; i < bulletCount; i++)
            {
                FireSingleBulletByPattern(boss, speed, i);
                yield return new WaitForSeconds(spawnInterval);
            }
        }
        if (boss != null) Destroy(gameObject);
        else Debug.Log("✅ [測試結束]");
    }

    // --- 單發邏輯 ---
    private void FireSingleBulletByPattern(BossBase boss, float speed, int index)
    {
        Vector2 dir = Vector2.down; 
        Vector2 spawnPos = transform.position; // 預設生成點

        switch (patternType)
        {
            case PatternType.Circle:
                // 1. 計算每顆子彈的角度間距
                float angleStep = 360f / bulletCount;
                
                // 2. 計算當前這顆子彈的角度 (起始角度 + 第幾顆 * 間距)
                float currentAngle = circleStartAngle + (index * angleStep);
                
                // 3. 算出方向向量
                dir = AngleToVector(currentAngle);
                
                // 4. ★ 關鍵：如果有半徑，生成點要往外推
                if (circleRadius > 0)
                {
                    spawnPos = (Vector2)transform.position + (dir * circleRadius);
                }
                
                CreateBullet(boss, spawnPos, dir, speed);
                break;

            // ... (其他模式保持原本邏輯，這裡省略以節省篇幅) ...
            case PatternType.Simple:
                dir = GetDesiredDirection();
                CreateBullet(boss, transform.position, dir, speed);
                break;
            case PatternType.RandomSpray:
                dir = useRandomDirection ? Random.insideUnitCircle.normalized : GetDesiredDirection();
                CreateBullet(boss, GetRandomSpawnPos(), dir, speed);
                break;
            case PatternType.RandomRain:
                CreateBullet(boss, GetRandomSpawnPos(), Vector2.down, speed);
                break;
            case PatternType.Sniper:
                CreateBullet(boss, transform.position, GetDirToPlayer(), speed);
                break;
            case PatternType.Shotgun:
                CreateBullet(boss, transform.position, GetDirToPlayer(), speed);
                break;
        }
    }

    // --- 一次性全射邏輯 ---
    private void FireAllPatterns(BossBase boss, float speed)
    {
        switch (patternType)
        {
            case PatternType.Circle:
                // 呼叫修正後的函式
                FireCircle(boss, speed); 
                break;

            case PatternType.Simple:
                Vector2 sDir = GetDesiredDirection();
                for(int i=0; i<bulletCount; i++) CreateBullet(boss, transform.position, sDir, speed);
                break;
            case PatternType.RandomSpray: FireRandomSpray(boss, speed); break;
            case PatternType.Shotgun:     FireShotgun(boss, speed, true); break;
            case PatternType.Sniper:      FireSniper(boss, speed); break;
            case PatternType.RandomRain:  FireRandomRain(boss, speed); break;
            case PatternType.LinearLine:  if(Application.isPlaying) StartCoroutine(FireLinearLineRoutine(boss, speed)); break;
        }
    }
    
    // --- ★ 修改後的 FireCircle (一次性) ---
    private void FireCircle(BossBase boss, float speed) 
    {
        float angleStep = 360f / bulletCount;
        for (int i = 0; i < bulletCount; i++) 
        {
            // 計算角度
            float currentAngle = circleStartAngle + (i * angleStep);
            Vector2 dir = AngleToVector(currentAngle);
            
            // 計算半徑位置
            Vector2 spawnPos = transform.position;
            if (circleRadius > 0)
            {
                spawnPos = (Vector2)transform.position + (dir * circleRadius);
            }

            CreateBullet(boss, spawnPos, dir, speed); 
        }
    }

    // --- 輔助方法 ---
    private Vector2 GetDesiredDirection()
    {
        return aimAtPlayer ? GetDirToPlayer() : AngleToVector(fixedAngle);
    }
    private Vector2 AngleToVector(float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
    // ... 其他輔助方法 (GetRandomSpawnPos, GetDirToPlayer, CreateBullet) 保持不變 ...
    private void CreateBullet(BossBase boss, Vector2 spawnPos, Vector2 direction, float speed)
    {
        if (bulletPrefab == null) { Debug.LogError("❌ 沒放 Bullet Prefab！"); return; }
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        EnemyProjectileBase script = bullet.GetComponent<EnemyProjectileBase>();
        if (script != null) script.Initialize(direction, speed, boss); 
        if (boss != null) boss.RegisterActiveBullet(bullet);
    }
    private Vector2 GetRandomSpawnPos() {
        float x = Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
        float y = Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
        return (Vector2)transform.position + (Vector2)transform.right * x + (Vector2)transform.up * y;
    }
    private Vector2 GetDirToPlayer() {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) return (player.transform.position - transform.position).normalized;
        return Vector2.down;
    }
    private void FireRandomSpray(BossBase boss, float speed) {
        for (int i = 0; i < bulletCount; i++) {
            Vector2 dir = useRandomDirection ? Random.insideUnitCircle.normalized : GetDesiredDirection();
            CreateBullet(boss, GetRandomSpawnPos(), dir, speed);
        }
    }
    private void FireRandomRain(BossBase boss, float speed) {
        for (int i = 0; i < bulletCount; i++) CreateBullet(boss, GetRandomSpawnPos(), Vector2.down, speed);
    }
    private void FireShotgun(BossBase boss, float speed, bool aimAtPlayer) {
        float startAngle = 0f; Vector2 baseDir = GetDirToPlayer();
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
        startAngle = baseAngle - (spreadAngle / 2f);
        float angleStep = (bulletCount > 1) ? spreadAngle / (bulletCount - 1) : 0;
        for (int i = 0; i < bulletCount; i++) {
            float currentAngle = startAngle + (angleStep * i);
            CreateBullet(boss, transform.position, AngleToVector(currentAngle), speed);
        }
    }
    private void FireSniper(BossBase boss, float speed) {
        Vector2 targetDir = GetDirToPlayer();
        for (int i = 0; i < bulletCount; i++) {
            float randomOffset = Random.Range(-5f, 5f);
            float baseAngle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;
            CreateBullet(boss, transform.position, AngleToVector(baseAngle + randomOffset), speed);
        }
    }
    private IEnumerator FireLinearLineRoutine(BossBase boss, float speed) {
        Vector2 startPos = (Vector2)transform.position - new Vector2(spawnAreaSize.x / 2f, 0);
        Vector2 endPos = (Vector2)transform.position + new Vector2(spawnAreaSize.x / 2f, 0);
        for (int i = 0; i < bulletCount; i++) {
            float t = (bulletCount > 1) ? (float)i / (bulletCount - 1) : 0.5f;
            Vector2 spawnPos = Vector2.Lerp(startPos, endPos, t);
            float randomY = Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
            spawnPos.y += randomY;
            CreateBullet(boss, spawnPos, Vector2.down, speed);
            if (spawnInterval > 0) yield return new WaitForSeconds(spawnInterval);
        }
    }

    // --- ★ 新增 Gizmos：畫出圓形半徑 ---
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        // 畫出原本的方形範圍
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 0));

        // ★ 畫出圓形範圍 (如果是 Circle 模式)
        if (patternType == PatternType.Circle)
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = Matrix4x4.identity; // 圓形通常不隨方塊旋轉變形
            Gizmos.DrawWireSphere(transform.position, circleRadius);
        }
    }
}