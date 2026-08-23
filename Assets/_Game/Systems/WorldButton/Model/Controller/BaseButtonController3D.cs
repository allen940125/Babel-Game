using UnityEngine;
using Gamemanager;

public class BaseButtonController3D : InteractiveMetaEntity3D, IPointerClickHandler
{
    [Header("★ 綁定的戰鬥指令")]
    [SerializeField] protected GameCommandSO commandToExecute;

    [Tooltip("此按鈕在每個階段可以使用的次數 (0 代表無限次)")]
    [SerializeField] protected int maxUsesPerPhase = 1; 
    
    private bool _isPhaseLocked = false;
    private int _currentUses = 0; 

    // ★ 注意：Awake 和 OnDestroy 都刪掉了！交給父類別統一處理事件的生命週期！

    public void OnClick()
    {
        if (_isPhaseLocked)
        {
            Debug.LogWarning($"[按鈕鎖定] {gameObject.name} 在 Boss 攻擊階段無法使用！");
            return;
        }
        
        if (maxUsesPerPhase > 0 && _currentUses >= maxUsesPerPhase)
        {
            Debug.LogWarning($"[按鈕鎖定] {gameObject.name} 本階段使用次數已達上限！");
            return;
        }

        if (commandToExecute != null)
        {
            commandToExecute.Execute();
            _currentUses++;

            if (maxUsesPerPhase > 0 && _currentUses >= maxUsesPerPhase)
            {
                SetInteractable(false, false); 
            }
        }
    }

    // ==========================================
    // 覆寫父類別的事件處理 (完美融合)
    // ==========================================
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