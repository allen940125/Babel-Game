using System;
using System.Collections.Generic; 
using Gamemanager;
using UnityEngine;

public abstract class BossBase : MonoBehaviour 
{
    public enum BossPhase { Idle, Special, Attacking, WaitingForBullets, Vulnerable }

    [System.Serializable]
    public struct BossAttackData
    {
        public string label;          
        public int bulletCount;       
        public float fireInterval;    
        public float bulletSpeedMultiple;     
    }
    
    [Header("基本設定")]
    public string bossName;
    public int maxHealth = 6;
    public int hitsPerDamage = 10;
    public Transform firePosition;

    // --- 新增：UI 血量顯示設定 ---
    [Header("UI 血量顯示")]
    [Tooltip("請放入一個掛有 HorizontalLayoutGroup 的空物件，用來裝血量圖示")]
    public GameObject healthBarContainer; 
    [Tooltip("代表一格血的圖示 Prefab (例如一顆愛心)")]
    public GameObject healthIconPrefab;

    [Header("攻擊階段參數設定 (依照血量損失順序)")]
    public List<BossAttackData> phaseDataList; 

    protected BossAttackData _currentPhaseData;
    protected int _bulletsRemainingToFire; 

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
    
    public virtual void StartBattle() 
    {
        currentHealth = maxHealth;
        
        // --- 初始化 UI ---
        InitHealthBar();
        
        UpdateDebugData(); 
        EnterPhase(BossPhase.Idle); 
    }

    // --- 新增：初始化血條 UI ---
    private void InitHealthBar()
    {
        if (healthBarContainer == null || healthIconPrefab == null)
        {
            Debug.LogWarning("請在 Inspector 設定 HealthBarContainer 和 HealthIconPrefab！");
            return;
        }

        // 1. 先清除舊的圖示 (防止重新開始戰鬥時圖示重複堆疊)
        foreach (Transform child in healthBarContainer.transform)
        {
            Destroy(child.gameObject);
        }

        // 2. 根據最大血量生成對應數量的圖示
        for (int i = 0; i < maxHealth; i++)
        {
            //Instantiate(healthIconPrefab, healthBarContainer.transform);
        }
    }

    // --- 新增：移除一個血量圖示 ---
    private void RemoveOneHealthIcon()
    {
        if (healthBarContainer == null) return;

        // 取得容器目前還有幾個子物件
        int childCount = healthBarContainer.transform.childCount;

        if (childCount > 0)
        {
            // 刪除最後一個子物件 (視覺上通常是從右邊扣血)
            Transform lastIcon = healthBarContainer.transform.GetChild(childCount - 1);
            Destroy(lastIcon.gameObject);
        }
    }

    public void TriggerBattleStart()
    {
        if (_currentPhase == BossPhase.Idle)
        {
            EnterPhase(BossPhase.Special);
        }
    }

    protected void Awake()
    {
        if (GameManager.Instance != null && GameManager.Instance.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnFightButtonClickEvent, OnFightButtonClickEvent);
        }
        animator = GetComponentInChildren<Animator>();
    }

    protected void OnDisable()
    {
        if (GameManager.Instance != null && GameManager.Instance.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.Unsubscribe<FightButtonClickEvent>(OnFightButtonClickEvent);
        }
    }

    private void OnFightButtonClickEvent(FightButtonClickEvent cmd)
    {
        if (_currentPhase == BossPhase.Idle)
        {
            EnterPhase(BossPhase.Special);
        }
    }
    
    protected virtual void Update()
    {
        _phaseTimerDisplay = phaseTimer;
        
        if (_currentPhase == BossPhase.Idle)
        {
        }
        else if (_currentPhase == BossPhase.Special)
        {
            phaseTimer -= Time.deltaTime;

            if (CheckSpecialPhaseSuccess())
            {
                Debug.Log("玩家提早破解特殊機制！");
                _wasLastSpecialBlocked = true; 
                EnterPhase(BossPhase.Vulnerable); 
                return;
            }

            if (phaseTimer <= 0)
            {
                Debug.Log("時間到！判定玩家失敗！");
                _wasLastSpecialBlocked = false; 
                EnterPhase(BossPhase.Vulnerable); 
            }
        }
        else if (_currentPhase == BossPhase.Vulnerable)
        {
        }
        else if (_currentPhase == BossPhase.Attacking)
        {
            ExecuteAttackLogic(_wasLastSpecialBlocked); 

            if (_bulletsRemainingToFire <= 0)
            {
                EnterPhase(BossPhase.WaitingForBullets);
            }
        }
        else if (_currentPhase == BossPhase.WaitingForBullets)
        {
            if (CheckBulletsCleared())
            {
                EnterPhase(BossPhase.Idle);
            }
        }
    }

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
                phaseTimer = 7.0f; 
                _wasLastSpecialBlocked = false; 
                EnterSpecialPhase(); 
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
                LoadAttackPhaseData();
                break;

            case BossPhase.WaitingForBullets:
                break;
        }
    }

    private void LoadAttackPhaseData()
    {
        int lostHealth = maxHealth - currentHealth;

        if (phaseDataList == null || phaseDataList.Count == 0)
        {
            Debug.LogError("請在 Boss Inspector 設定 Phase Data List！");
            _bulletsRemainingToFire = 5; 
            _currentPhaseData = new BossAttackData { bulletCount = 5, fireInterval = 0.5f, bulletSpeedMultiple = 5f };
            return;
        }

        int index = Mathf.Clamp(lostHealth, 0, phaseDataList.Count - 1);

        _currentPhaseData = phaseDataList[index];
        _bulletsRemainingToFire = _currentPhaseData.bulletCount;

        Debug.Log($"載入攻擊參數 (Index {index}): 數量={_currentPhaseData.bulletCount}, 速度={_currentPhaseData.bulletSpeedMultiple}");
    }

    protected void UpdateDebugData()
    {
        _currentHealthDisplay = currentHealth;
        _currentHitCountDisplay = currentHitCount;
    }

    protected abstract void ExecuteAttackLogic(bool wasBlocked); 
    protected abstract void EnterSpecialPhase();
    protected abstract bool CheckBulletsCleared(); 
    protected abstract bool CheckSpecialPhaseSuccess();

    public void TakeHit()
    {
        if (_currentPhase != BossPhase.Vulnerable) return; 

        currentHitCount++;
        UpdateDebugData(); 
        
        if(currentHitCount >= hitsPerDamage)
        {
            currentHitCount = 0;
            currentHealth--;
            
            // --- 關鍵修改：更新血量條 ---
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

    protected virtual void Die()
    {
        Debug.Log("Boss 死亡！");
        Destroy(gameObject);
    }
}