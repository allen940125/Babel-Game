using UnityEngine;
using Gamemanager;
using UnityEngine.Events;

public class BaseButtonController3D : InteractiveMetaEntity3D, IPointerClickHandler
{
    [Header("★ 綁定的戰鬥指令")]
    [SerializeField] protected GameCommandSO commandToExecute;

    [Tooltip("此按鈕在每個階段可以使用的次數 (0 代表無限次)")]
    [SerializeField] protected int maxUsesPerPhase = 1; 
    
    [Header("★ 視覺與狀態事件")]
    public UnityEvent OnExhausted; // 新增：次數耗盡時觸發
    public UnityEvent OnReset;     // 新增：階段重置時觸發
    public UnityEvent OnCommandExecuted; // ★ 新增：廣播按鈕成功執行
    
    // ==========================================
    // ★ 新增：開放給外部 (如中介層 Adapter) 控制的純粹開關
    // 按鈕本身不知道為什麼被鎖，也不知道為什麼效能被打折，它只看這兩個值辦事。
    // ==========================================
    public bool IsExternallyLocked { get; set; } = false;
    public float EfficiencyMultiplier { get; set; } = 1.0f;
    
    private bool _isPhaseLocked = false;
    private int _currentUses = 0; 

    // ★ 注意：Awake 和 OnDestroy 都刪掉了！交給父類別統一處理事件的生命週期！

    public void OnClick()
    {
        // 1. 檢查是否被 Boss 階段鎖定
        if (_isPhaseLocked)
        {
            Debug.LogWarning($"[按鈕鎖定] {gameObject.name} 在 Boss 攻擊階段無法使用！");
            return;
        }

        // ★ 2. 新增：檢查是否被外部 (如損壞中介層) 強制鎖定
        if (IsExternallyLocked)
        {
            Debug.LogWarning($"[按鈕鎖定] {gameObject.name} 目前處於外部鎖定狀態 (可能已損壞)！");
            return;
        }
        
        // 3. 檢查次數
        if (maxUsesPerPhase > 0 && _currentUses >= maxUsesPerPhase)
        {
            Debug.LogWarning($"[按鈕鎖定] {gameObject.name} 本階段使用次數已達上限！");
            return;
        }

        if (commandToExecute != null)
        {
            // 將自己傳入，指令腳本可以透過 instigator.GetComponent<BaseButtonController3D>().EfficiencyMultiplier 取得打折係數
            commandToExecute.Execute(this.gameObject);
            _currentUses++;

            OnCommandExecuted?.Invoke(); // ★ 新增：廣播給外部中介腳本知道
            
            if (maxUsesPerPhase > 0 && _currentUses >= maxUsesPerPhase)
            {
                OnExhausted?.Invoke();
            }
        }
    }

    // ==========================================
    // 覆寫父類別的事件處理 (完美融合)
    // ==========================================

    // ★ 新增：處理轉場時的按鈕鎖定
    protected override void OnBossTransition(BossEnterTransitionPhaseEvent evt)
    {
        base.OnBossTransition(evt); 
        
        if (!ignorePhaseLock) 
        {
            _isPhaseLocked = true; // 轉場一開始，立刻讓按鈕點不下去
        }
    }
    
    protected override void OnBossAttacking(BossEnterAttackingPhaseEvent evt)
    {
        // 1. 呼叫 base，讓父類別去關閉拖曳跟旋轉
        base.OnBossAttacking(evt); 
        
        // 2. 執行子類別特有的邏輯：把自己鎖起來不給點
        if (!ignorePhaseLock) 
        {
            _isPhaseLocked = true; 
        }
    }

    protected override void OnBossIdle(BossEnterIdlePhaseEvent evt)
    {
        // 1. 呼叫 base，讓父類別去開啟拖曳跟旋轉
        base.OnBossIdle(evt);
        
        // 2. 執行子類別特有的邏輯：解鎖並重置次數
        if (!ignorePhaseLock) 
        {
            _isPhaseLocked = false; 
        }
        _currentUses = 0; 
    }
}