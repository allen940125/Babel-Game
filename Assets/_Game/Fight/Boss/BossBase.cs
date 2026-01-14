using System;
using System.Collections.Generic; 
using Gamemanager; // 引用你的事件系統
using UnityEngine;

public abstract class BossBase : MonoBehaviour 
{
    public enum BossPhase { Idle, Special, Attacking, WaitingForBullets, Vulnerable }

    // --- 定義：單一波次的攻擊細節 ---
    [System.Serializable]
    public struct BulletWaveData
    {
        public string note;
        public GameObject bulletPrefab;
        public int count;
        public float interval;
        public float speed;
    }

    // --- 定義：每個血量階段的設定 (包含多個波次) ---
    [System.Serializable]
    public struct BossPhaseConfig
    {
        public string label; // 例如 "滿血階段"
        public List<BulletWaveData> waveList; // 這個階段的攻擊排程
    }
    
    [Header("基本設定")]
    public string bossName;
    public int maxHealth = 6;
    public int hitsPerDamage = 10;
    public Transform firePosition;
    public float specialTimer;

    [Header("UI 血量顯示")]
    public GameObject healthBarContainer; 
    //public GameObject healthIconPrefab;

    [Header("攻擊階段參數設定 (依照血量損失順序)")]
    public List<BossPhaseConfig> phaseConfigs; 

    // --- 內部運作變數 ---
    protected Queue<BulletWaveData> _waveQueue = new Queue<BulletWaveData>(); // 攻擊波次佇列
    protected BulletWaveData _currentWave; // 當前正在執行的波次
    protected int _currentWaveRemainingCount; // 當前波次還剩幾發
    protected float _fireTimer; // 射擊計時器

    // 特殊機制管理 (通用)
    protected List<BossSpecialMechanism> _activeSpecialMechanisms = new List<BossSpecialMechanism>();

    [Header("觀察數據 (唯讀)")]
    [SerializeField] protected BossPhase _currentPhase = BossPhase.Idle; 
    [SerializeField] protected float _phaseTimerDisplay;                 
    [SerializeField] protected int _currentHealthDisplay;                
    [SerializeField] protected int _currentHitCountDisplay;              
    [SerializeField] protected bool _wasLastSpecialBlocked = false;      

    protected float phaseTimer;
    protected int currentHealth;
    protected int currentHitCount;
    protected Animator animator;

    // --- 初始化與生命週期 ---

    protected virtual void Awake()
    {
        if (GameManager.Instance != null && GameManager.Instance.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnFightButtonClickEvent, OnFightButtonClickEvent);
        }
        animator = GetComponentInChildren<Animator>();
    }

    protected virtual void OnDisable()
    {
        if (GameManager.Instance != null && GameManager.Instance.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.Unsubscribe<FightButtonClickEvent>(OnFightButtonClickEvent);
        }
    }

    public virtual void StartBattle() 
    {
        currentHealth = maxHealth;
        //InitHealthBar();
        UpdateDebugData(); 
        EnterPhase(BossPhase.Idle); 
    }

    // UI 按鈕觸發
    private void OnFightButtonClickEvent(FightButtonClickEvent cmd) => TriggerBattleStart();
    
    public void TriggerBattleStart()
    {
        if (_currentPhase == BossPhase.Idle)
        {
            EnterPhase(BossPhase.Special);
        }
    }

    protected virtual void Update()
    {
        _phaseTimerDisplay = phaseTimer;
        
        if (_currentPhase == BossPhase.Idle) { /* 等待觸發 */ }
        else if (_currentPhase == BossPhase.Special)
        {
            phaseTimer -= Time.deltaTime;

            // 情況 2: 時間到
            if (phaseTimer <= 0)
            {
                bool success = CheckSpecialPhaseSuccess();
                if (success)
                {
                    Debug.Log("時間到，全部隱藏 -> 成功！");
                    _wasLastSpecialBlocked = true;
                }
                else
                {
                    Debug.Log("時間到，失敗 (爆走預定)！");
                    _wasLastSpecialBlocked = false;
                }
                
                // --- 新增：清理場上機關 ---
                // 不管成功還是失敗，時間到了都要把剩下的機關清掉
                ClearActiveSpecialMechanisms();

                EnterPhase(BossPhase.Vulnerable); 
            }
        }
        else if (_currentPhase == BossPhase.Vulnerable) { /* 等待玩家攻擊 TakeHit */ }
        else if (_currentPhase == BossPhase.Attacking)
        {
            // 執行攻擊排程
            ExecuteAttackSequence();
        }
        else if (_currentPhase == BossPhase.WaitingForBullets)
        {
            if (CheckBulletsCleared())
            {
                EnterPhase(BossPhase.Idle);
            }
        }
    }

    // --- 狀態切換核心 ---

    protected virtual void EnterPhase(BossPhase newPhase)
    {
        _currentPhase = newPhase; 
        Debug.Log($"<color=yellow>{bossName} 進入階段: {newPhase}</color>");

        switch (newPhase)
        {
            case BossPhase.Idle:
                GameManager.Instance.MainGameEvent.Send(new BossEnterIdlePhaseEvent());
                if(animator) animator.Play("Idle");
                break;

            case BossPhase.Special:
                phaseTimer = specialTimer; // 特殊機制時間
                _wasLastSpecialBlocked = false; 
                ClearActiveSpecialMechanisms(); // 清空上一輪的機關
                EnterSpecialPhase(); // 生成新機關
                break;

            case BossPhase.Vulnerable:
                GameManager.Instance.MainGameEvent.Send(new BossEnterVulnerablePhaseEvent());
                if(animator) animator.Play("Idle"); 
                currentHitCount = 0; 
                UpdateDebugData();
                break;

            case BossPhase.Attacking:
                GameManager.Instance.MainGameEvent.Send(new BossEnterAttackingPhaseEvent());
                if(animator) animator.Play("Attack1");
                LoadAttackPhaseConfig(); // 載入這回合的攻擊波次
                break;

            case BossPhase.WaitingForBullets:
                break;
        }
    }

    // --- 攻擊邏輯 (Wave System) ---

    private void LoadAttackPhaseConfig()
    {
        int lostHealth = maxHealth - currentHealth;

        if (phaseConfigs == null || phaseConfigs.Count == 0)
        {
            Debug.LogError("請在 Inspector 設定 Phase Configs！");
            EnterPhase(BossPhase.WaitingForBullets); // 沒設定就跳過
            return;
        }

        int index = Mathf.Clamp(lostHealth, 0, phaseConfigs.Count - 1);
        BossPhaseConfig config = phaseConfigs[index];

        _waveQueue.Clear();
        foreach (var wave in config.waveList)
        {
            _waveQueue.Enqueue(wave);
        }

        Debug.Log($"載入階段 {index} ({config.label})，共有 {_waveQueue.Count} 波攻擊");
        PrepareNextWave();
    }

    private void PrepareNextWave()
    {
        if (_waveQueue.Count > 0)
        {
            _currentWave = _waveQueue.Dequeue();
            _currentWaveRemainingCount = _currentWave.count;
            _fireTimer = 0f; // 立即開始
            Debug.Log($"執行波次: {_currentWave.note}");
        }
        else
        {
            // 所有波次結束
            EnterPhase(BossPhase.WaitingForBullets);
        }
    }

    private void ExecuteAttackSequence()
    {
        if (_currentWaveRemainingCount <= 0)
        {
            PrepareNextWave();
            return;
        }

        _fireTimer -= Time.deltaTime;

        if (_fireTimer <= 0)
        {
            // 計算參數 (狂暴判定)
            float finalSpeed = _currentWave.speed;
            float finalInterval = _currentWave.interval;
            Color finalColor = Color.white;

            if (!_wasLastSpecialBlocked) // 失敗懲罰
            {
                finalSpeed *= 1.5f;
                finalInterval *= 0.5f;
                finalColor = Color.red;
            }

            // 呼叫子類別生成
            if (_currentWave.bulletPrefab != null)
            {
                FireBulletInstance(_currentWave.bulletPrefab, finalSpeed, finalColor);
            }

            _currentWaveRemainingCount--;
            _fireTimer = finalInterval;
        }
    }

    // --- 特殊機制判定 ---

    protected bool CheckSpecialPhaseSuccess()
    {
        _activeSpecialMechanisms.RemoveAll(item => item == null);
        if (_activeSpecialMechanisms.Count == 0 && _activeSpecialMechanisms.Capacity != 0) return true; // 全空視為成功

        foreach (var mechanism in _activeSpecialMechanisms)
        {
            if (!mechanism.IsCleared) return false; // 只要有一個沒隱藏就算失敗
        }
        return true;
    }

    protected void ClearActiveSpecialMechanisms()
    {
        foreach (var item in _activeSpecialMechanisms)
        {
            if (item != null) Destroy(item.gameObject);
        }
        _activeSpecialMechanisms.Clear();
    }

    // --- UI 與受傷邏輯 ---

    // private void InitHealthBar()
    // {
    //     if (!healthBarContainer || !healthIconPrefab) return;
    //     foreach (Transform child in healthBarContainer.transform) Destroy(child.gameObject);
    //     for (int i = 0; i < maxHealth; i++) Instantiate(healthIconPrefab, healthBarContainer.transform);
    // }

    private void RemoveOneHealthIcon()
    {
        if (healthBarContainer && healthBarContainer.transform.childCount > 0)
            Destroy(healthBarContainer.transform.GetChild(healthBarContainer.transform.childCount - 1).gameObject);
    }

    public void TakeHit()
    {
        if (_currentPhase != BossPhase.Vulnerable) return; 

        currentHitCount++;
        UpdateDebugData(); 
        
        if(currentHitCount >= hitsPerDamage)
        {
            currentHitCount = 0;
            currentHealth--;
            RemoveOneHealthIcon(); // UI 更新
            UpdateDebugData(); 
            
            if(currentHealth <= 0) 
            {
                Die();
            }
            else
            {
                EnterPhase(BossPhase.Attacking); // 被打痛了 -> 反擊
            }
        }
    }

    protected void UpdateDebugData()
    {
        _currentHealthDisplay = currentHealth;
        _currentHitCountDisplay = currentHitCount;
    }

    protected virtual void Die()
    {
        Debug.Log("Boss 死亡！");
        Destroy(gameObject);
    }

    // --- 抽象方法 (子類別實作) ---
    protected abstract void FireBulletInstance(GameObject prefab, float speed, Color color);
    protected abstract void EnterSpecialPhase();
    protected abstract bool CheckBulletsCleared(); 
}