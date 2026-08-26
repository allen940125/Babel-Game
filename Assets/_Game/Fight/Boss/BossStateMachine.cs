using System.Collections;
using System.Collections.Generic;
using Gamemanager;
using UnityEngine;

// ★ 職責精簡：本類別絕對不處理扣血，只負責「狀態切換」、「彈幕發射」與「階段計時」
public abstract class BossStateMachine : MonoBehaviour
{
    public enum BossPhase { Idle, Attacking, WaitingForBullets }

    [System.Serializable]
    public struct BulletWaveData { public string note; public GameObject patternPrefab; public float delayBeforeNext; }
    [System.Serializable]
    public struct BossPhaseConfig { public string label; public List<BulletWaveData> waveList; }

    [Header("基本設定")]
    public string bossName;
    public Transform firePosition;
    [Tooltip("攻擊階段最長防呆時間 (秒)。")]
    public float attackPhaseDuration = 15.0f;

    [Header("攻擊階段參數設定")]
    public List<BossPhaseConfig> phaseConfigs;

    [Header("即時觀察數據 (唯讀)")]
    [SerializeField] protected BossPhase _currentPhase = BossPhase.Attacking;
    [SerializeField] protected float _phaseTimerDisplay;
    [SerializeField] protected bool _isProvoked = false;
    private bool _isBattleActive = false;

    // --- 內部資料結構 ---
    protected HashSet<GameObject> _activeBullets = new HashSet<GameObject>();
    protected HashSet<GameObject> _activePatterns = new HashSet<GameObject>();
    protected Queue<BulletWaveData> _waveQueue = new Queue<BulletWaveData>();
    protected List<GameObject> _activeMapMechanisms = new List<GameObject>();
    
    protected float _waveDelayTimer;
    protected float phaseTimer;
    protected Animator animator;

    // ★ 放棄直接序列化 EntityRuntime，改為從 EntityCore 借用
    private EntityRuntime _bossData;
    
    // ★ 開放給子類別 (如 BossCleaner) 讀取的通道
    protected EntityRuntime BossData => _bossData;

    protected virtual void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        _isBattleActive = false;
        _isProvoked = true;
    }

    protected virtual void Start()
    {
        // 1. 拿大腦資料
        var core = GetComponent<EntityCore>();
        if (core != null) _bossData = core.RuntimeData;
        
        // 2. 尋找通用的 HealthComponent，並注入 Boss 專屬邏輯
        var healthComp = GetComponent<EntityHealthComponent>();
        if (healthComp != null)
        {
            // ★ 刪除這行！我們現在改用標籤系統了，不需要這個掛鉤了！
            // healthComp.OnCheckInvincibility += IsInvinciblePhase;
            
            // ★ 這行保留！這才是負責觸發反擊的通知
            healthComp.OnDamageTaken += HandleBossDamaged;
        }
        else
        {
            Debug.LogError($"[致命錯誤] {bossName} 缺少 EntityHealthComponent！");
        }
        GameManager.Instance.MainGameMediator.RegisterCurrentBoss(_bossData);
    }

    public virtual void StartBattle()
    {
        _isBattleActive = true;
        _isProvoked = true;
        
        // ★ 通知全域中繼站，讓 UI 知道要去哪裡抓血條資料
        if (_bossData != null)
        {
            GameManager.Instance.MainGameMediator.RegisterCurrentBoss(_bossData);
        }
        
        EnterPhase(BossPhase.Attacking); 
    }

    // ==========================================
    // ★ 提供給 BossHealthComponent 呼叫的兩個溝通介面
    // ==========================================
    
    
    // 提供給 HealthComponent 扣完血後執行的邏輯
    private void HandleBossDamaged(int finalDamage)
    {
        if (_bossData.CurrentHealth <= 0)
        {
            Die();
        }
        else
        {
            // 觸發反擊
            ProvokeToCounterAttack();
        }
    }
    
    protected virtual void Die() 
    { 
        Debug.Log("Boss 死亡！"); 
        //Destroy(gameObject); 
    }

    /// <summary>
    /// 讓受擊腳本通知：扣血成功，請立即啟動反擊！
    /// </summary>
    public void ProvokeToCounterAttack()
    {
        _isProvoked = true;
        Debug.Log($"<color=green>[{bossName}] 休眠中被物理痛毆！立刻展開下一輪大範圍攻擊！</color>");
        EnterPhase(BossPhase.Attacking);
    }

    // ==========================================
    // 註冊與清理機制 (子彈與地圖道具)
    // ==========================================
    
    public void RegisterActiveBullet(GameObject bullet) { if (bullet != null) _activeBullets.Add(bullet); }
    public void UnregisterActiveBullet(GameObject bullet) { if (bullet != null) _activeBullets.Remove(bullet); }
    public void RegisterActivePattern(GameObject pattern) { if (pattern != null) _activePatterns.Add(pattern); }
    public void UnregisterActivePattern(GameObject pattern) { if (pattern != null) _activePatterns.Remove(pattern); }

    public void RegisterMapMechanism(GameObject obj)
    {
        if (obj != null) _activeMapMechanisms.Add(obj);
    }

    protected void ClearAllMapMechanisms()
    {
        if (_activeMapMechanisms == null || _activeMapMechanisms.Count == 0) return;
        for (int i = _activeMapMechanisms.Count - 1; i >= 0; i--)
        {
            if (_activeMapMechanisms[i] != null)
            {
                _activeMapMechanisms[i].SetActive(false);
                Destroy(_activeMapMechanisms[i]);
            }
        }
        _activeMapMechanisms.Clear();
    }

    protected void ClearAllActiveProjectiles()
    {
        _waveQueue.Clear();
        foreach (GameObject pattern in _activePatterns)
        {
            if (pattern != null) { pattern.SetActive(false); Destroy(pattern); }
        }
        _activePatterns.Clear();
        foreach (GameObject bullet in _activeBullets)
        {
            if (bullet != null) { bullet.SetActive(false); Destroy(bullet); }
        }
        _activeBullets.Clear();
    }

    // ==========================================
    // 狀態機核心運作邏輯
    // ==========================================
    
    protected virtual void Update()
    {
        if (!_isBattleActive) return;
        
        _phaseTimerDisplay = phaseTimer;

        if (_currentPhase == BossPhase.Attacking)
        {
            HandleAttackingPhaseUpdate();
        }
        else if (_currentPhase == BossPhase.WaitingForBullets)
        {
            if (_activeBullets.Count == 0 && _activePatterns.Count == 0)
            {
                EnterPhase(BossPhase.Idle);
            }
        }
    }

    private void HandleAttackingPhaseUpdate()
    {
        bool isTimeUp = false;
        
        // 將計時器交給 SO 運算，接收歸零訊號
        if (_bossData != null && _bossData.TryGetTrait(out TimerTrait timerTrait))
        {
            isTimeUp = timerTrait.TickTimer(Time.deltaTime);
            _phaseTimerDisplay = timerTrait.currentTimer;
        }
        else
        {
            phaseTimer -= Time.deltaTime;
            _phaseTimerDisplay = phaseTimer;
            isTimeUp = (phaseTimer <= 0f);
        }

        if (isTimeUp)
        {
            Debug.Log($"<color=orange>[{bossName}] 攻擊倒數歸零！清空全場彈幕與殘留道具！</color>");
            ClearAllActiveProjectiles();
            ClearAllMapMechanisms(); 
            EnterPhase(BossPhase.WaitingForBullets);
            return;
        }

        if (_waveQueue.Count == 0 && _activeBullets.Count == 0 && _activePatterns.Count == 0)
        {
            ClearAllMapMechanisms(); 
            EnterPhase(BossPhase.WaitingForBullets);
            return;
        }

        ExecuteAttackSequence();
    }

    protected virtual void EnterPhase(BossPhase newPhase)
    {
        _currentPhase = newPhase;
        Debug.Log($"<color=yellow>{bossName} 進入階段: {newPhase}</color>");

        switch (newPhase)
        {
            case BossPhase.Idle:
                _isProvoked = false;
                _bossData?.RemoveState(EntityStateFlags.Invincible);
                GameManager.Instance.MainGameEvent.Send(new BossEnterIdlePhaseEvent());
                if (animator) animator.Play("Idle");
                
                if (_bossData != null && _bossData.TryGetTrait(out TimerTrait idleTimer)) 
                {
                    idleTimer.StartTimer(0f);
                }
                break;

            case BossPhase.Attacking:
                GameManager.Instance.MainGameEvent.Send(new BossEnterAttackingPhaseEvent());
                _bossData?.AddState(EntityStateFlags.Invincible);
                if (animator) animator.Play("Attack1");
                
                if (_bossData != null && _bossData.TryGetTrait(out TimerTrait attackTimer)) 
                {
                    attackTimer.StartTimer(attackPhaseDuration);
                }
                else 
                {
                    phaseTimer = attackPhaseDuration;
                }
                
                LoadAttackPhaseConfig();
                ClearAllMapMechanisms();
                SpawnSpecialMechanisms(); 
                break;

            case BossPhase.WaitingForBullets:
                if (animator) animator.Play("Idle");
                break;
        }
    }

    private void LoadAttackPhaseConfig()
    {
        if (_bossData == null || phaseConfigs == null || phaseConfigs.Count == 0) return;
        
        float lostRatio = 1.0f - ((float)_bossData.CurrentHealth / _bossData.MaxHealth);
        int index = Mathf.Clamp(Mathf.FloorToInt(lostRatio * phaseConfigs.Count), 0, phaseConfigs.Count - 1);
        BossPhaseConfig config = phaseConfigs[index];

        _waveQueue.Clear();
        foreach (var wave in config.waveList) _waveQueue.Enqueue(wave);

        _activeBullets.Clear();
        _activePatterns.Clear();
        _waveDelayTimer = 0f;
    }

    private void ExecuteAttackSequence()
    {
        if (_activeBullets.Count > 0 || _activePatterns.Count > 0) return;
        if (_waveDelayTimer > 0) { _waveDelayTimer -= Time.deltaTime; return; }

        if (_waveQueue.Count > 0)
        {
            BulletWaveData nextWave = _waveQueue.Dequeue();
            if (nextWave.patternPrefab != null)
            {
                GameObject patternObj = Instantiate(nextWave.patternPrefab, firePosition.position, Quaternion.identity);
                RegisterActivePattern(patternObj);
                if (patternObj.TryGetComponent(out AttackPatternBase patternScript))
                {
                    patternScript.Execute(this, 1.0f, true);
                }
            }
            _waveDelayTimer = nextWave.delayBeforeNext;
        }
    }

    // 由子類別 (如 BossCleaner) 決定要生成什麼特殊的輔助機關
    protected abstract void SpawnSpecialMechanisms();
}