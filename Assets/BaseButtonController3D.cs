using UnityEngine;
using Gamemanager;

public class BaseButtonController3D : InteractiveMetaEntity3D, IPointerClickHandler
{
    [Header("★ 綁定的戰鬥指令")]
    [SerializeField] protected GameCommandSO commandToExecute;

    [Header("戰鬥鎖定與次數設定")]
    [Tooltip("如果為 true，Boss 攻擊時此按鈕不會被鎖定")]
    [SerializeField] protected bool ignorePhaseLock = false;
    
    [Tooltip("此按鈕在每個階段可以使用的次數 (0 代表無限次)")]
    [SerializeField] protected int maxUsesPerPhase = 1; // ★ 預設只能按 1 次
    
    private bool _isPhaseLocked = false;
    
    private int _currentUses = 0; // 內部計數器

    protected override void Awake()
    {
        base.Awake(); 

        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterAttackingPhaseEvent, OnBossAttacking);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterIdlePhaseEvent, OnBossIdle);
        }
    }

    protected virtual void OnDestroy()
    {
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterAttackingPhaseEvent>(OnBossAttacking);
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterIdlePhaseEvent>(OnBossIdle);
        }
    }

    // ==========================================
    // 實作點擊行為
    // ==========================================
    public virtual void OnClick()
    {
        if (_isPhaseLocked)
        {
            Debug.LogWarning($"[按鈕鎖定] {gameObject.name} 在 Boss 攻擊階段無法使用！");
            return;
        }
        
        // 1. 檢查次數限制
        if (maxUsesPerPhase > 0 && _currentUses >= maxUsesPerPhase)
        {
            Debug.LogWarning($"[按鈕鎖定] {gameObject.name} 本階段使用次數已達上限 ({maxUsesPerPhase})！");
            return;
        }

        if (commandToExecute != null)
        {
            Debug.Log($"[3D按鈕觸發] 執行綁定指令: {commandToExecute.commandName}");
            commandToExecute.Execute();
            
            // 2. 增加使用次數
            _currentUses++;

            // 3. 如果用光了，立刻將按鈕變灰/鎖死 (呼叫父類的物理互動鎖)
            if (maxUsesPerPhase > 0 && _currentUses >= maxUsesPerPhase)
            {
                SetInteractable(false, false); 
            }
        }
    }

    // ==========================================
    // Boss 階段自動鎖定與「次數重置」
    // ==========================================
    private void OnBossAttacking(BossEnterAttackingPhaseEvent evt)
    {
        if (!ignorePhaseLock) 
        {
            _isPhaseLocked = true; // ★ 狀態上鎖 (自己記住不能點)
            SetInteractable(false, false); // 呼叫底層，鎖住拖曳
        }
    }

    private void OnBossIdle(BossEnterIdlePhaseEvent evt)
    {
        if (!ignorePhaseLock) 
        {
            _isPhaseLocked = false; // ★ 狀態解鎖
            SetInteractable(true, true);
        }
    }
}