using System.Collections;
using System.Collections.Generic;
using Gamemanager;
using UnityEngine;

public abstract class BossBase : MonoBehaviour, IDamageable
{
    // ★ 1. 狀態極限收斂：Idle (休眠/受傷)、Attacking (發招)、WaitingForBullets (等彈幕散去)
    public enum BossPhase { Idle, Attacking, WaitingForBullets }

    [System.Serializable]
    public struct BulletWaveData { public string note; public GameObject patternPrefab; public float delayBeforeNext; }
    [System.Serializable]
    public struct BossPhaseConfig { public string label; public List<BulletWaveData> waveList; }

    [Header("★ 資料庫綁定 (SSOT)")]
    [SerializeField] protected BaseEntityRuntimeSO bossSO;
    
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

    // --- 內部資料結構 (O(1) 效能保證) ---
    protected HashSet<GameObject> _activeBullets = new HashSet<GameObject>();
    protected HashSet<GameObject> _activePatterns = new HashSet<GameObject>();
    protected Queue<BulletWaveData> _waveQueue = new Queue<BulletWaveData>();

    protected float _waveDelayTimer;
    protected float phaseTimer;
    protected Animator animator;
    private bool _isLowHealthActive = false;

    protected virtual void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        bodySprite = GetComponentInChildren<SpriteRenderer>();

        if (bossSO != null) bossSO.Initialize(bossSO.MaxHealth, bossSO.AttackPower, bossSO.Defense);
        UpdateDebugData();
    }

    public virtual void StartBattle()
    {
        if (bossSO != null) bossSO.Initialize(bossSO.MaxHealth, bossSO.AttackPower, bossSO.Defense);
        UpdateDebugData();
        
        // ★ 絕對先手權：開戰直接給予下馬威攻擊！
        _isProvoked = true;
        EnterPhase(BossPhase.Attacking); 
    }

    public void RegisterActiveBullet(GameObject bullet) { if (bullet != null) _activeBullets.Add(bullet); }
    public void UnregisterActiveBullet(GameObject bullet) { if (bullet != null) _activeBullets.Remove(bullet); }
    public void RegisterActivePattern(GameObject pattern) { if (pattern != null) _activePatterns.Add(pattern); }
    public void UnregisterActivePattern(GameObject pattern) { if (pattern != null) _activePatterns.Remove(pattern); }

    protected virtual void Update()
    {
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
        phaseTimer -= Time.deltaTime;
        CheckLowHealthVFX();

        // 1. 防呆保護：15 秒時間一到，不管有沒有子彈，強制清空並進入等待！
        if (phaseTimer <= 0f)
        {
            Debug.Log($"<color=orange>[{bossName}] 15 秒防呆上限已到！清空場上殘留彈幕！</color>");
            ClearAllActiveProjectiles();
            EnterPhase(BossPhase.WaitingForBullets);
            return;
        }

        // ★ 2. 核心優化：如果隊列裡的波次已經全部「發射完畢」，且現在場上「已經沒有任何子彈跟發射器」！
        if (_waveQueue.Count == 0 && _activeBullets.Count == 0 && _activePatterns.Count == 0)
        {
            Debug.Log($"<color=cyan>[{bossName}] 所有子彈提早打完且場上全空！直接進入 Waiting/Idle 狀態！</color>");
            EnterPhase(BossPhase.WaitingForBullets);
            return;
        }

        // 3. 繼續正常執行發射排程
        ExecuteAttackSequence();
    }

    protected virtual void EnterPhase(BossPhase newPhase)
    {
        _currentPhase = newPhase;
        Debug.Log($"<color=yellow>{bossName} 進入階段: {newPhase}</color>");

        switch (newPhase)
        {
            case BossPhase.Idle:
                // ★ 怒火鎖歸零：回到 Idle 絕對休眠，直到受傷才會把 _isProvoked 轉為 true！
                _isProvoked = false;
                GameManager.Instance.MainGameEvent.Send(new BossEnterIdlePhaseEvent());
                if (animator) animator.Play("Idle");
                phaseTimer = 0f;
                break;

            case BossPhase.Attacking:
                GameManager.Instance.MainGameEvent.Send(new BossEnterAttackingPhaseEvent());
                if (animator) animator.Play("Attack1");
                phaseTimer = attackPhaseDuration;
                
                LoadAttackPhaseConfig();
                SpawnSpecialMechanisms(); 
                break;

            case BossPhase.WaitingForBullets:
                if (animator) animator.Play("Idle");
                break;
        }
    }

    private void LoadAttackPhaseConfig()
    {
        if (bossSO == null || phaseConfigs == null || phaseConfigs.Count == 0) return;
        float lostRatio = 1.0f - ((float)bossSO.CurrentHealth / bossSO.MaxHealth);
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
        foreach (GameObject pattern in _activePatterns) if (pattern != null) Destroy(pattern);
        _activePatterns.Clear();
        foreach (GameObject bullet in _activeBullets) if (bullet != null) Destroy(bullet);
        _activeBullets.Clear();
    }

    private void CheckLowHealthVFX()
    {
        if (bossSO == null) return;
        float healthRatio = (float)bossSO.CurrentHealth / bossSO.MaxHealth;
        if (healthRatio <= 0.2f && bossSO.CurrentHealth > 0)
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
        // 嚴格防禦：只有在 Idle 且尚未被激怒的狀態下，才能受傷！
        if (_currentPhase != BossPhase.Idle || _isProvoked || bossSO == null || bossSO.CurrentHealth <= 0)
        {
            Debug.Log($"<color=gray>[防禦阻擋] {bossName} 當前在 {_currentPhase} 高壓狀態，處於無敵防護，攻擊無效！</color>");
            return;
        }

        int finalDamage = Mathf.Max(1, payload.Damage - bossSO.Defense);
        bossSO.ModifyHealth(-finalDamage);
        UpdateDebugData();

        Debug.Log($"<color=yellow>[Boss 受傷！] 受到重擊！扣除 {finalDamage} 點血量 | 剩餘:{bossSO.CurrentHealth}/{bossSO.MaxHealth}</color>");

        if (bodySprite != null)
        {
            StopCoroutine(nameof(FlashRedEffect));
            StartCoroutine(nameof(FlashRedEffect));
        }
        GameManager.Instance.MainGameEvent.Send(new BossTakeDamageEvent() { Intensity = hitShakeIntensity, Duration = hitShakeDuration });
        GameManager.Instance.MainGameEvent.Send(new BossHealthChangedEvent() { CurrentHealth = bossSO.CurrentHealth, MaxHealth = bossSO.MaxHealth });

        if (bossSO.CurrentHealth <= 0)
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

    protected void UpdateDebugData() { if (bossSO != null) _currentHealthDisplay = bossSO.CurrentHealth; }
    protected virtual void Die() { Debug.Log("Boss 死亡！"); Destroy(gameObject); }
    
    protected abstract void SpawnSpecialMechanisms();
}