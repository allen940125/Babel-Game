using System.Collections;
using System.Collections.Generic;
using Gamemanager;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class BossBase : MonoBehaviour, IDamageable
{
    // ★ 1. 狀態極限收斂：Idle (休眠/受傷)、Attacking (發招)、WaitingForBullets (等彈幕散去)
    public enum BossPhase { Idle, Attacking, WaitingForBullets }

    [System.Serializable]
    public struct BulletWaveData { public string note; public GameObject patternPrefab; public float delayBeforeNext; }
    [System.Serializable]
    public struct BossPhaseConfig { public string label; public List<BulletWaveData> waveList; }

    [FormerlySerializedAs("bossSO")]
    [Header("★ 資料庫綁定 (SSOT)")]
    [SerializeField] protected EntityRuntime boss; // ★ 嚴格改為 BossRuntimeSO！
    
    [Header("基本設定")]
    public string bossName;
    public Transform firePosition;
    [Tooltip("攻擊階段最長防呆時間 (秒)。若時間內沒打完將強制清空彈幕")]
    public float attackPhaseDuration = 15.0f;

    [Header("受傷效果")]
    public SpriteRenderer bodySprite;
    public Color damageColor = Color.red;
    public float flashDuration = 0.1f;
    public float hitShakeIntensity = 0.3f;
    public float hitShakeDuration = 0.2f;

    [Header("攻擊階段參數設定")]
    public List<BossPhaseConfig> phaseConfigs;

    [Header("即時觀察數據 (唯讀)")]
    [SerializeField] protected BossPhase _currentPhase = BossPhase.Attacking;
    [SerializeField] protected float _phaseTimerDisplay;
    [SerializeField] protected int _currentHealthDisplay;
    
    [Header("★ 怒火反擊旗標 (唯讀)")]
    [Tooltip("在 Idle 時為 false，只有被玩家物理痛毆扣血後才會變為 true，隨即反擊！")]
    [SerializeField] protected bool _isProvoked = false;
    private bool _isBattleActive = false;

    // --- 內部資料結構 (O(1) 效能保證) ---
    protected HashSet<GameObject> _activeBullets = new HashSet<GameObject>();
    protected HashSet<GameObject> _activePatterns = new HashSet<GameObject>();
    protected Queue<BulletWaveData> _waveQueue = new Queue<BulletWaveData>();

    protected List<GameObject> _activeMapMechanisms = new List<GameObject>();
    
    protected float _waveDelayTimer;
    protected float phaseTimer;
    protected Animator animator;
    private bool _isLowHealthActive = false;

    protected virtual void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        bodySprite = GetComponentInChildren<SpriteRenderer>();

        _isBattleActive = false;     // 鎖住 Update 與受傷判定_isProvoked = true;
        
        if (boss != null) boss.Initialize(boss.Blueprint);
        UpdateDebugData();
    }

    public virtual void StartBattle()
    {
        _isBattleActive = true;        // 解開 AI 鎖
        _isProvoked = true;            // 確保下馬威狀態
        
        if (boss != null) boss.Initialize(boss.Blueprint);
        UpdateDebugData();
        
        EnterPhase(BossPhase.Attacking); 
    }

    public void RegisterActiveBullet(GameObject bullet) { if (bullet != null) _activeBullets.Add(bullet); }
    public void UnregisterActiveBullet(GameObject bullet) { if (bullet != null) _activeBullets.Remove(bullet); }
    public void RegisterActivePattern(GameObject pattern) { if (pattern != null) _activePatterns.Add(pattern); }
    public void UnregisterActivePattern(GameObject pattern) { if (pattern != null) _activePatterns.Remove(pattern); }

    protected virtual void Update()
    {
        if (!_isBattleActive) return;
        
        _phaseTimerDisplay = phaseTimer;
        
        Debug.Log(_activeBullets + "剩餘子彈");

        if (_currentPhase == BossPhase.Attacking)
        {
            HandleAttackingPhaseUpdate();
        }
        else if (_currentPhase == BossPhase.WaitingForBullets)
        {
            // ★ 當場上所有的發射器與子彈都清空後，正式回到 Idle 讓玩家佈置與痛毆！
            if (_activeBullets.Count == 0 && _activePatterns.Count == 0)
            {
                EnterPhase(BossPhase.Idle);
            }
        }
    }

    private void HandleAttackingPhaseUpdate()
    {
        CheckLowHealthVFX();

        // ★ 1. 將計時器交給 SO 運算，接收歸零訊號
        bool isTimeUp = false;
        if (boss != null && boss.TryGetTrait(out TimerTrait timerTrait))
        {
            isTimeUp = timerTrait.TickTimer(Time.deltaTime);
            _phaseTimerDisplay = timerTrait.currentTimer;
        }
        else
        {
            // 防呆備用機制：如果企劃忘記掛載 TimerTrait，退回使用本地計時器
            phaseTimer -= Time.deltaTime;
            _phaseTimerDisplay = phaseTimer;
            isTimeUp = (phaseTimer <= 0f);
        }

        // ★ 2. 當時間到（或被道具扣到 0）時，安全且乾淨地結束！
        if (isTimeUp)
        {
            Debug.Log($"<color=orange>[{bossName}] 攻擊倒數歸零（被道具加速或超時）！清空全場彈幕與殘留道具！</color>");
            ClearAllActiveProjectiles();
            ClearAllMapMechanisms(); 
            EnterPhase(BossPhase.WaitingForBullets);
            return;
        }

        // 3. 子彈打完且場上清空的提早結束邏輯
        if (_waveQueue.Count == 0 && _activeBullets.Count == 0 && _activePatterns.Count == 0)
        {
            Debug.Log($"<color=cyan>[{bossName}] 子彈提早打完且場上全空！直接進入 Waiting/Idle！</color>");
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
                GameManager.Instance.MainGameEvent.Send(new BossEnterIdlePhaseEvent());
                if (animator) animator.Play("Idle");
                
                // ★ 索取並重置計時器特徵
                if (boss != null && boss.TryGetTrait(out TimerTrait idleTimer)) 
                {
                    idleTimer.StartTimer(0f);
                }
                break;

            case BossPhase.Attacking:
                GameManager.Instance.MainGameEvent.Send(new BossEnterAttackingPhaseEvent());
                if (animator) animator.Play("Attack1");
                
                // ★ 索取並啟動計時器特徵
                if (boss != null && boss.TryGetTrait(out TimerTrait attackTimer)) 
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
        if (boss == null || phaseConfigs == null || phaseConfigs.Count == 0) return;
        float lostRatio = 1.0f - ((float)boss.CurrentHealth / boss.MaxHealth);
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

    protected void ClearAllActiveProjectiles()
    {
        _waveQueue.Clear();
        foreach (GameObject pattern in _activePatterns)
        {
            if (pattern != null) 
            {
                pattern.SetActive(false);
                Destroy(pattern);
            }
        }
        _activePatterns.Clear();

        foreach (GameObject bullet in _activeBullets)
        {
            if (bullet != null) 
            {
                bullet.SetActive(false);
                Destroy(bullet);
            }
        }
        _activeBullets.Clear();
    }

    // ★ 4. 新增：專門清空地圖機關的方法
    protected void ClearAllMapMechanisms()
    {
        if (_activeMapMechanisms == null || _activeMapMechanisms.Count == 0) return;

        for (int i = _activeMapMechanisms.Count - 1; i >= 0; i--)
        {
            if (_activeMapMechanisms[i] != null)
            {
                // 先關閉物件互動，再行銷毀，徹底杜絕銷毀當下的射線或碰撞殘留報錯
                _activeMapMechanisms[i].SetActive(false);
                Destroy(_activeMapMechanisms[i]);
            }
        }
        _activeMapMechanisms.Clear();
    }

    // ★ 5. 提供給子類別 (Cleaner) 註冊地圖道具的介面
    public void RegisterMapMechanism(GameObject obj)
    {
        if (obj != null) _activeMapMechanisms.Add(obj);
    }

    private void CheckLowHealthVFX()
    {
        if (boss == null) return;
        float healthRatio = (float)boss.CurrentHealth / boss.MaxHealth;
        if (healthRatio <= 0.2f && boss.CurrentHealth > 0)
        {
            if (bodySprite != null) bodySprite.color = Color.Lerp(Color.white, damageColor, Mathf.PingPong(Time.time * 10f, 1f));
            if (!_isLowHealthActive)
            {
                _isLowHealthActive = true;
                GameManager.Instance.MainGameEvent.Send(new BossLowHealthStateEvent() { IsActive = true });
            }
        }
        else if (_isLowHealthActive)
        {
            _isLowHealthActive = false;
            if (bodySprite != null) bodySprite.color = Color.white;
            GameManager.Instance.MainGameEvent.Send(new BossLowHealthStateEvent() { IsActive = false });
        }
    }

    // ==========================================
    // ★ 核心落實：在 Idle 狀態下挨打，立即轉入攻擊！
    // ==========================================
    public void TakeDamage(DamagePayload payload)
    {
        // 對話中或戰鬥未啟動，直接忽略所有傷害
        if (!_isBattleActive) 
        {
            Debug.Log($"<color=grey>[{bossName}] 還在對話階段，無敵狀態，攻擊無效！</color>");
            return;
        }
        
        // 嚴格防禦：只有在 Idle 且尚未被激怒的狀態下，才能受傷！
        if (_currentPhase != BossPhase.Idle || _isProvoked || boss == null || boss.CurrentHealth <= 0)
        {
            Debug.Log($"<color=gray>[防禦阻擋] {bossName} 當前在 {_currentPhase} 高壓狀態，處於無敵防護，攻擊無效！</color>");
            return;
        }

        int finalDamage = Mathf.Max(1, payload.Damage - boss.TotalDefense);
        boss.ModifyHealth(-finalDamage);
        UpdateDebugData();

        Debug.Log($"<color=yellow>[Boss 受傷！] 受到重擊！扣除 {finalDamage} 點血量 | 剩餘:{boss.CurrentHealth}/{boss.MaxHealth}</color>");

        if (bodySprite != null)
        {
            StopCoroutine(nameof(FlashRedEffect));
            StartCoroutine(nameof(FlashRedEffect));
        }
        GameManager.Instance.MainGameEvent.Send(new BossTakeDamageEvent() { Intensity = hitShakeIntensity, Duration = hitShakeDuration });
        GameManager.Instance.MainGameEvent.Send(new BossHealthChangedEvent() { CurrentHealth = boss.CurrentHealth, MaxHealth = boss.MaxHealth });

        if (boss.CurrentHealth <= 0)
        {
            Die();
            return;
        }

        // ★ 核心閉環：挨打後把怒火致能鎖打開，直接進入反擊！
        _isProvoked = true;
        Debug.Log($"<color=green>[{bossName}] 休眠中被物理痛毆 (_isProvoked=true)！立刻展開下一輪大範圍攻擊！</color>");
        EnterPhase(BossPhase.Attacking);
    }

    private IEnumerator FlashRedEffect()
    {
        bodySprite.color = damageColor;
        yield return new WaitForSeconds(flashDuration);
        if (bodySprite != null) bodySprite.color = Color.white;
    }

    protected void UpdateDebugData() { if (boss != null) _currentHealthDisplay = boss.CurrentHealth; }
    protected virtual void Die() { Debug.Log("Boss 死亡！"); Destroy(gameObject); }
    
    protected abstract void SpawnSpecialMechanisms();
}