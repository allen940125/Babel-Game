using UnityEngine;

// ★ 所有模組化行為的基底
[RequireComponent(typeof(BossStateMachine))]
public abstract class BossPhaseAction : MonoBehaviour
{
    [Header("觸發條件")]
    [Tooltip("指定在哪個階段觸發此行為")]
    public BossStateMachine.BossPhase targetPhase;

    protected BossStateMachine stateMachine;

    protected virtual void Awake()
    {
        stateMachine = GetComponent<BossStateMachine>();
    }

    protected virtual void OnEnable()
    {
        stateMachine.OnPhaseEntered += HandlePhaseEntered;
        stateMachine.OnPhaseExited += HandlePhaseExited;
    }

    protected virtual void OnDisable()
    {
        stateMachine.OnPhaseEntered -= HandlePhaseEntered;
        stateMachine.OnPhaseExited -= HandlePhaseExited;
    }

    private void HandlePhaseEntered(BossStateMachine.BossPhase phase)
    {
        if (phase == targetPhase)
        {
            ExecuteAction();
        }
    }
    
    private void HandlePhaseExited(BossStateMachine.BossPhase phase)
    {
        if (phase == targetPhase)
        {
            StopAction();
        }
    }

    // 子類別實作具體的執行與停止邏輯
    protected abstract void ExecuteAction();
    protected virtual void StopAction() { } 
}