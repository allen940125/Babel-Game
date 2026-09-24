using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 中介抽象層：專門負責「預警 -> 生成子彈」的標準流程。
/// 任何會實際射出子彈的 Pattern 都應該繼承此類別，而不是直接繼承 AttackPatternBase。
/// </summary>
public abstract class BulletSpawnerBase : AttackPatternBase
{
    [Header("★ 生成參數 (全發射器共用)")]
    public GameObject bulletPrefab;
    public float baseSpeed = 5f;
    [Tooltip("每顆子彈發射的間隔時間 (秒)。若為 0 則瞬間齊射")]
    public float spawnInterval = 0.05f;

    // ★ 已經刪除重複定義的 SpawnData！現在這裡統一使用父類別 (AttackPatternBase) 的 SpawnData。

    // 實作 AttackPatternBase 的介面
    protected override void OnExecute(BossStateMachine boss, float speedMultiplier, bool isAngry)
    {
        float finalSpeed = baseSpeed * speedMultiplier;
        if (isAngry) finalSpeed *= 1.5f;

        transform.SetParent(null); 
        if (boss != null) boss.RegisterActivePattern(this.gameObject);

        // 啟動範本流程
        StartCoroutine(ExecuteSpawnerSequence(boss, finalSpeed));
    }

    // ★ 強制要求子類別實作此方法：純算數學，不要 Instantiate！
    protected abstract List<SpawnData> CalculateAllSpawnData();

    private IEnumerator ExecuteSpawnerSequence(BossStateMachine boss, float speed)
    {
        // 階段 1：呼叫子類別的數學運算，取得所有彈幕資料
        List<SpawnData> dataList = CalculateAllSpawnData();

        // 階段 2：呼叫最底層 AttackPatternBase 的預警系統，並等待它結束
        yield return StartCoroutine(ShowWarningsAndWait(dataList));

        // 階段 3：照著同一份資料發射子彈
        if (spawnInterval > 0f)
        {
            foreach (var data in dataList)
            {
                CreateBullet(boss, data.position, data.direction, speed);
                yield return new WaitForSeconds(spawnInterval);
            }
        }
        else
        {
            foreach (var data in dataList)
            {
                CreateBullet(boss, data.position, data.direction, speed);
            }
        }

        // 階段 4：結束並銷毀發射器
        FinishPattern();
    }

    private void CreateBullet(BossStateMachine boss, Vector2 spawnPos, Vector2 direction, float speed)
    {
        if (bulletPrefab == null) 
        { 
            Debug.LogError($"❌ [{name}] 沒放 Bullet Prefab！", this); 
            return; 
        }
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        EnemyProjectileBase script = bullet.GetComponent<EnemyProjectileBase>();
        
        if (script != null) script.Initialize(direction, speed, boss); 
        if (boss != null) boss.RegisterActiveBullet(bullet);
    }
}