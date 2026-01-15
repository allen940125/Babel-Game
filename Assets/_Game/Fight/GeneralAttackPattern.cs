using UnityEngine;

public class GeneralAttackPattern : AttackPatternBase
{
    public enum PatternType
    {
        Circle,     // 全方位圓形擴散
        Shotgun,    // 扇形散彈 (瞄準玩家)
        Sniper,     // 精準狙擊 (單發或多發直線)
        RandomSpray // 隨機亂噴
    }

    [Header("攻擊模式選擇")]
    public PatternType patternType = PatternType.Circle;

    [Header("彈幕參數")]
    public GameObject bulletPrefab; // 子彈 Prefab
    public int bulletCount = 10;    // 射幾發
    public float baseSpeed = 5f;    // 基礎速度
    
    [Header("扇形/散彈專用參數")]
    [Tooltip("散開的角度 (例如 90度)")]
    public float spreadAngle = 90f; 

    // 實作父類別的方法
    protected override void OnExecute(BossBase boss, float speedMultiplier, bool isAngry)
    {
        // 1. 計算最終速度 (包含憤怒加成)
        float finalSpeed = baseSpeed * speedMultiplier;
        if (isAngry) finalSpeed *= 1.5f;

        // 2. 根據 Enum 決定怎麼射
        switch (patternType)
        {
            case PatternType.Circle:
                FireCircle(boss, finalSpeed);
                break;

            case PatternType.Shotgun:
                FireShotgun(boss, finalSpeed, true); // true = 瞄準玩家
                break;
                
            case PatternType.Sniper:
                FireSniper(boss, finalSpeed);
                break;

            case PatternType.RandomSpray:
                FireRandom(boss, finalSpeed);
                break;
        }
        
        //--- 新增：射完就自我銷毀 ---
        Destroy(gameObject, 0.1f);
    }

    // --- 具體的發射邏輯 ---

    // 1. 圓形擴散
    private void FireCircle(BossBase boss, float speed)
    {
        float angleStep = 360f / bulletCount;
        
        for (int i = 0; i < bulletCount; i++)
        {
            float angle = i * angleStep;
            // 數學公式：角度轉向量
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            
            CreateBullet(boss, dir, speed);
        }
    }

    // 2. 扇形/散彈 (可瞄準玩家)
    private void FireShotgun(BossBase boss, float speed, bool aimAtPlayer)
    {
        float startAngle = 0f;
        Vector2 baseDir = Vector2.down; // 預設向下

        // 如果要瞄準玩家，先算出「朝向玩家的角度」
        if (aimAtPlayer)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                baseDir = (player.transform.position - transform.position).normalized;
            }
        }

        // 算出基礎角度 (Atan2 回傳的是弧度，要轉度數)
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
        
        // 扇形的起點角度 = 中心角度 - (總角度 / 2)
        startAngle = baseAngle - (spreadAngle / 2f);
        
        // 每顆子彈的間隔角度
        float angleStep = (bulletCount > 1) ? spreadAngle / (bulletCount - 1) : 0;

        for (int i = 0; i < bulletCount; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            Vector2 dir = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad));
            
            CreateBullet(boss, dir, speed);
        }
    }

    // 3. 狙擊 (單點連射)
    private void FireSniper(BossBase boss, float speed)
    {
        // 找出玩家方向
        Vector2 targetDir = Vector2.down;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            targetDir = (player.transform.position - transform.position).normalized;
        }

        // 這邊示範稍微有一點點隨機偏移，才不會每次都重疊在一起
        for (int i = 0; i < bulletCount; i++)
        {
            // 稍微偏移一點點角度 (例如 -5度 到 5度 之間)
            float randomOffset = Random.Range(-5f, 5f);
            float baseAngle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;
            float finalAngle = baseAngle + randomOffset;
            
            Vector2 dir = new Vector2(Mathf.Cos(finalAngle * Mathf.Deg2Rad), Mathf.Sin(finalAngle * Mathf.Deg2Rad));
            
            CreateBullet(boss, dir, speed);
        }
    }
    
    // 4. 隨機亂噴
    private void FireRandom(BossBase boss, float speed)
    {
        for (int i = 0; i < bulletCount; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            CreateBullet(boss, dir, speed);
        }
    }

    // --- 輔助：生成子彈並註冊 ---
    private void CreateBullet(BossBase boss, Vector2 direction, float speed)
    {
        if (bulletPrefab == null) return;

        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        EnemyBullet script = bullet.GetComponent<EnemyBullet>();
        
        if (script != null)
        {
            // 這裡可以傳入預設的穿透設定，或是你在這個腳本再加一個 bool 變數來控制
            script.Initialize(direction, speed); 
        }

        // 註冊給 Boss 管理
        boss.RegisterActiveBullet(bullet);
    }
}