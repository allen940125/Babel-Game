using UnityEngine;
using System.Collections.Generic;

public class BossCleaner : BossBase
{
    [Header("Cleaner 專屬設定")]
    public GameObject laserPrefab; 
    public GameObject eyePrefab;
    
    private List<GameObject> _activeBullets = new List<GameObject>();
    private List<GameObject> _activeEyes = new List<GameObject>();
    
    private float _fireTimer;   

    // --- 修改：EnterPhase 不需要覆寫了，因為父類別已經處理好數據載入 ---
    // (除非你有額外的 Cleaner 專屬初始化要做，否則可以刪掉 EnterPhase)
    protected override void EnterPhase(BossPhase newPhase)
    {
        base.EnterPhase(newPhase);
        if (newPhase == BossPhase.Attacking)
        {
            _fireTimer = 0; // 重置計時器
        }
    }

    protected override void ExecuteAttackLogic(bool wasBlocked)
    {
        // 1. 如果子彈射完了，就跳出
        if (_bulletsRemainingToFire <= 0) return;

        // 2. 倒數計時
        _fireTimer -= Time.deltaTime;

        if (_fireTimer <= 0)
        {
            // 3. 準備發射參數
            // 基本參數來自父類別 List
            float finalSpeedMultiple = _currentPhaseData.bulletSpeedMultiple;
            float finalInterval = _currentPhaseData.fireInterval;
            Color finalColor = Color.white;

            // 4. 根據「是否被擋下」進行微調 (Angry Mode)
            if (!wasBlocked)
            {
                // 懲罰機制：速度變快、間隔變短、顏色變紅
                finalSpeedMultiple *= 1.5f;        
                finalInterval *= 0.5f;     
                finalColor = Color.red;
            }

            // 5. 發射！
            FireBullet(finalColor, finalSpeedMultiple);

            // 6. 重置計時器 & 扣除彈藥
            _fireTimer = finalInterval;
            _bulletsRemainingToFire--;
        }
    }

    void FireBullet(Color bulletColor, float speedMultiple)
    {
        GameObject bulletObj = Instantiate(laserPrefab, firePosition.position, Quaternion.identity);
        
        var sprite = bulletObj.GetComponentInChildren<SpriteRenderer>();
        if(sprite) sprite.color = bulletColor;
        
        EnemyBullet bulletScript = bulletObj.GetComponent<EnemyBullet>();
        if (bulletScript != null)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            
            // 使用傳入的 speed 初始化
            // 注意：這裡我們需要修改 EnemyBullet.Initialize 讓它接受固定速度，或者你把 range 設為 0
            // 簡單做法：如果這裡指定了 speed，就忽略 EnemyBullet 內部的 Range
            bulletScript.Initialize(randomDir, speedMultiple); 
        }

        _activeBullets.Add(bulletObj);
    }

    protected override bool CheckSpecialPhaseSuccess()
    {
        _activeEyes.RemoveAll(eye => eye == null);
        if (_activeEyes.Count == 0 && _activeEyes.Capacity != 0) return true;
        return false;
    }

    protected override void EnterSpecialPhase()
    {
        foreach(var eye in _activeEyes) if(eye != null) Destroy(eye);
        _activeEyes.Clear();
        for(int i=0; i<3; i++)
        {
            Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle * 3f;
            GameObject eye = Instantiate(eyePrefab, pos, Quaternion.identity);
            _activeEyes.Add(eye);
        }
    }

    protected override bool CheckBulletsCleared()
    {
        _activeBullets.RemoveAll(item => item == null);
        return _activeBullets.Count == 0;
    }
}