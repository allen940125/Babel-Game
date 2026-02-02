using System;
using System.Collections;
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
        [Tooltip("只是備註，方便你自己看 (例如：螺旋丸)")]
        public string note;
        
        [Tooltip("請拖入掛有 AttackPatternBase 的 Prefab")]
        public GameObject patternPrefab; 
        
        [Tooltip("這波子彈全清空後，要休息多久才放下一招？")]
        public float delayBeforeNext;    
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
    public int maxHealth = 7;
    public int hitsPerDamage = 10;
    public Transform firePosition;
    
    [Tooltip("特殊機制 (Special Phase) 的倒數時間")]
    public float specialTimer = 15.0f;

    [Header("UI 血量顯示")]
    public GameObject healthBarContainer; 

    [Header("受傷效果")]
    public SpriteRenderer bodySprite; // 請在 Inspector 拉入 Boss 的 SpriteRenderer
    public Color damageColor = Color.red; // 受傷變紅
    public float flashDuration = 0.1f;    // 閃多久
    
    [Header("震動參數")]
    public float hitShakeIntensity = 0.3f; // 受傷震多大
    public float hitShakeDuration = 0.2f;  // 受傷震多久

    // 內部變數
    private bool _isLowHealthActive = false; // 避免重複發送事件
    
    [Header("攻擊階段參數設定 (依照血量損失順序)")]
    public List<BossPhaseConfig> phaseConfigs; 

    // --- 內部運作變數 ---
    
    // 子彈管理
    protected List<GameObject> _activeBullets = new List<GameObject>(); 
    
    // ★ 新增：發射器管理 (用來追蹤還在運作的 Coroutine 發射器)
    protected List<GameObject> _activePatterns = new List<GameObject>();
    // 攻擊排程
    protected Queue<BulletWaveData> _waveQueue = new Queue<BulletWaveData>(); 
    protected float _waveDelayTimer; // 波次間的休息計時器

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
    public Camera _mainCamera; // 用於持續震動
    private Vector3 _cameraOriginalPos; // 記錄攝影機原始位置

    // --- 初始化與生命週期 ---

    protected virtual void Awake()
    {
        if (GameManager.Instance != null && GameManager.Instance.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnFightButtonClickEvent, OnFightButtonClickEvent);
        }
        animator = GetComponentInChildren<Animator>();
        if (bodySprite == null) bodySprite = GetComponentInChildren<SpriteRenderer>();
    }

    protected virtual void OnDisable()
    {
        if (GameManager.Instance != null && GameManager.Instance.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.Unsubscribe<FightButtonClickEvent>(OnFightButtonClickEvent);
        }
    }

    // 測試用：如果想讓遊戲一開始就直接進戰鬥，可以在這裡呼叫
    protected virtual void Start()
    {
        // StartBattle(); // 如果需要自動開始，把這行解開
    }

    public virtual void StartBattle() 
    {
        currentHealth = maxHealth;
        //InitHealthBar(); // 初始化血條
        UpdateDebugData(); 
        
        // 設定一開始就直接進入攻擊狀態 (下馬威)
        // 注意：如果你希望一開始是特殊機制，這裡改 BossPhase.Special
        EnterPhase(BossPhase.Attacking); 
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

    // --- 提供給 Pattern Prefab 呼叫的方法 ---
    // 當發射器生成子彈時，必須呼叫這個方法，把子彈交給 Boss 管理
    public void RegisterActiveBullet(GameObject bullet)
    {
        if (bullet != null)
        {
            _activeBullets.Add(bullet);
        }
    }

    protected virtual void Update()
    {
        _phaseTimerDisplay = phaseTimer;
        
        if (_currentPhase == BossPhase.Idle) { /* 等待觸發 */ }
        else if (_currentPhase == BossPhase.Special)
        {
            phaseTimer -= Time.deltaTime;

            if (currentHealth == 1 && _currentPhase != BossPhase.Idle)
            {
                // 1. 閃爍特效
                if (bodySprite != null)
                {
                    float t = Mathf.PingPong(Time.time * 10f, 1f);
                    bodySprite.color = Color.Lerp(Color.white, damageColor, t);
                }

                // 2. 發送持續震動事件 (只在狀態改變時發送一次)
                if (!_isLowHealthActive)
                {
                    _isLowHealthActive = true;
                    GameManager.Instance.MainGameEvent.Send(new BossLowHealthStateEvent() { IsActive = true });
                }
            }
            else
            {
                // 離開瀕死狀態 (例如補血了，或是剛開始)
                if (_isLowHealthActive)
                {
                    _isLowHealthActive = false;
                    // 確保身體顏色變回來 (如果不是受傷閃爍中)
                    if(bodySprite != null) bodySprite.color = Color.white;
                
                    // 關閉震動
                    GameManager.Instance.MainGameEvent.Send(new BossLowHealthStateEvent() { IsActive = false });
                }
            }
            
            // 1. 檢查是否提早全部隱藏 (成功)
            if (CheckSpecialPhaseSuccess())
            {
                Debug.Log("玩家提早破解特殊機制！");
                _wasLastSpecialBlocked = true; 
                ClearActiveSpecialMechanisms();
                EnterPhase(BossPhase.Vulnerable); 
                return;
            }

            // 2. 時間到
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
                
                // 不管成功失敗，清理場上機關
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
            // 雙重確認 (雖然 ExecuteAttackSequence 已經會檢查，但這裡是保險)
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
                phaseTimer = specialTimer; 
                _wasLastSpecialBlocked = false; 
                ClearActiveSpecialMechanisms(); 
                EnterSpecialPhase(); // 生成新機關 (子類別實作)
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
                // 這個狀態只是過渡，用來確認場上真的乾淨了
                break;
        }
    }

    public void RegisterActivePattern(GameObject pattern)
    {
        if (pattern != null && !_activePatterns.Contains(pattern))
        {
            _activePatterns.Add(pattern);
        }
    }
    
    // --- 攻擊邏輯 (Pattern System) ---

    private void LoadAttackPhaseConfig()
    {
        int lostHealth = maxHealth - currentHealth;

        if (phaseConfigs == null || phaseConfigs.Count == 0)
        {
            Debug.LogError("請在 Inspector 設定 Phase Configs！");
            EnterPhase(BossPhase.WaitingForBullets); 
            return;
        }

        int index = Mathf.Clamp(lostHealth, 0, phaseConfigs.Count - 1);
        BossPhaseConfig config = phaseConfigs[index];

        _waveQueue.Clear();
        foreach (var wave in config.waveList)
        {
            _waveQueue.Enqueue(wave);
        }
        
        _activeBullets.Clear(); // 確保清單乾淨
        _waveDelayTimer = 0f;   // 第一波不需要等待，立刻發射

        Debug.Log($"載入階段 {index} ({config.label})，共有 {_waveQueue.Count} 波攻擊");
    }

    private void ExecuteAttackSequence()
    {
        // 1. 清理已經銷毀的物件 (移除 null)
        _activeBullets.RemoveAll(b => b == null);
        _activePatterns.RemoveAll(p => p == null); // ★ 新增：清理已經自殺的發射器

        // 2. ★ 關鍵修改：如果場上還有子彈 OR 還有正在運作的發射器，就等待
        if (_activeBullets.Count > 0 || _activePatterns.Count > 0) return;

        // --- 以下邏輯代表：場上無子彈 且 無發射器 ---

        // 3. 處理波次間的休息時間
        if (_waveDelayTimer > 0)
        {
            _waveDelayTimer -= Time.deltaTime;
            return;
        }

        // 4. 準備發射下一波
        if (_waveQueue.Count > 0)
        {
            BulletWaveData nextWave = _waveQueue.Dequeue();
            Debug.Log($"執行波次: {nextWave.note}");
            
            if (nextWave.patternPrefab != null)
            {
                // 生成發射器
                GameObject patternObj = Instantiate(nextWave.patternPrefab, firePosition.position, Quaternion.identity);
                
                // ★ 新增：將這個發射器加入監控清單
                // 這樣只要它還沒 Destroy (代表還在射)，Boss 就不會結束攻擊階段
                _activePatterns.Add(patternObj);

                var patternScript = patternObj.GetComponent<AttackPatternBase>();
                
                if (patternScript != null)
                {
                    bool isAngry = !_wasLastSpecialBlocked;
                    
                    // ★ 記得加上這行：讓發射器獨立於 Boss (避免 Boss 轉身時帶著它跑，或被誤刪)
                    // patternObj.transform.SetParent(null); 

                    patternScript.Execute(this, 1.0f, isAngry);
                }
            }
            else
            {
                Debug.LogWarning("波次設定中缺少 Pattern Prefab！");
            }

            // 設定下一波前的休息時間
            float finalDelay = nextWave.delayBeforeNext;
            if (!_wasLastSpecialBlocked) finalDelay *= 0.5f;

            _waveDelayTimer = finalDelay;
        }
        else
        {
            // 5. 佇列空了 -> 真的結束了
            Debug.Log("所有波次結束，準備回到 Idle");
            EnterPhase(BossPhase.WaitingForBullets);
        }
    }

    // --- 特殊機制判定 ---

    protected bool CheckSpecialPhaseSuccess()
    {
        _activeSpecialMechanisms.RemoveAll(item => item == null);
        if (_activeSpecialMechanisms.Count == 0 && _activeSpecialMechanisms.Capacity != 0) return true; 

        foreach (var mechanism in _activeSpecialMechanisms)
        {
            if (!mechanism.IsCleared) return false; 
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
    
    // 子類別需要實作：檢查自己生成的子彈是否清空 (如果還有其他來源的話)
    // 但因為現在統一用 _activeBullets 管理，這裡可以簡單實作
    protected virtual bool CheckBulletsCleared()
    {
        _activeBullets.RemoveAll(b => b == null);
        return _activeBullets.Count == 0;
    }

    // --- UI 與受傷邏輯 ---

    private void RemoveOneHealthIcon()
    {
        if (healthBarContainer && healthBarContainer.transform.childCount > 0)
        {
            Destroy(healthBarContainer.transform.GetChild(healthBarContainer.transform.childCount - 1).gameObject);
        }
    }

    public void TakeHit()
    {
        if (_currentPhase != BossPhase.Vulnerable) return; 

        // 1. 視覺：閃紅光
        if (bodySprite != null)
        {
            StopCoroutine("FlashRedEffect"); 
            StartCoroutine("FlashRedEffect");
        }

        // 2. ★ 震動：發送事件 (帶參數)
        // 使用 Object Initializer 語法傳入參數
        GameManager.Instance.MainGameEvent.Send(new BossTakeDamageEvent() 
        { 
            Intensity = hitShakeIntensity, 
            Duration = hitShakeDuration 
        });

        currentHitCount++;
        UpdateDebugData(); 
        
        if(currentHitCount >= hitsPerDamage)
        {
            currentHitCount = 0;
            currentHealth--;
            RemoveOneHealthIcon(); 
            UpdateDebugData(); 
            
            if(currentHealth <= 0) 
            {
                Die();
            }
            else
            {
                EnterPhase(BossPhase.Attacking); 
            }
        }
    }

    // --- 新增：閃紅光 Coroutine ---
    private IEnumerator FlashRedEffect()
    {
        // 變紅
        bodySprite.color = damageColor;

        // 等待一下
        yield return new WaitForSeconds(flashDuration);

        // 變回白色 (原色)
        bodySprite.color = Color.white;
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

    // --- 抽象方法 ---
    // 因為攻擊邏輯已經移到 Pattern Prefab，這裡不再需要 FireBulletInstance
    protected abstract void EnterSpecialPhase();
}