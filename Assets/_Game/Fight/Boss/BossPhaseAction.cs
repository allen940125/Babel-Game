using UnityEngine;

[RequireComponent(typeof(BossStateMachine))]
public abstract class BossPhaseAction : MonoBehaviour
{
    [Header("觸發條件")]
    public BossStateMachine.BossPhase targetPhase;

    [Header("觸發限制 (通用)")]
    [Tooltip("指定要『跳過』前幾次的狀態觸發？ (0 代表每次都觸發)")]
    public int skipFirstNTriggers = 0;
    
    // ★ 新增：退休機制
    [Tooltip("這招最多能成功發動幾次？ (0 代表無限次)")]
    public int maxTriggerCount = 0;
    
    private int _triggerCount = 0;      // 記錄進入狀態的總次數
    private int _executeCount = 0;      // 記錄真正成功執行的次數

    protected BossStateMachine stateMachine;

    protected virtual void Awake() { stateMachine = GetComponent<BossStateMachine>(); }
    protected virtual void OnEnable() { stateMachine.OnPhaseEntered += HandlePhaseEntered; stateMachine.OnPhaseExited += HandlePhaseExited; }
    protected virtual void OnDisable() { stateMachine.OnPhaseEntered -= HandlePhaseEntered; stateMachine.OnPhaseExited -= HandlePhaseExited; }

    private void HandlePhaseEntered(BossStateMachine.BossPhase phase)
    {
        if (phase == targetPhase)
        {
            _triggerCount++;

            // 1. 檢查是否已經度過「跳過期」
            if (_triggerCount > skipFirstNTriggers)
            {
                // 2. 檢查是否超過「壽命極限」
                if (maxTriggerCount > 0 && _executeCount >= maxTriggerCount)
                {
                    Debug.Log($"<color=gray>[通用限制器] {gameObject.name} 壽命已盡 (已發動 {_executeCount} 次)，永久退休。</color>");
                    return;
                }

                _executeCount++;
                ExecuteAction();
            }
        }
    }
    
    private void HandlePhaseExited(BossStateMachine.BossPhase phase)
    {
        if (phase == targetPhase) StopAction();
    }

    protected abstract void ExecuteAction();
    protected virtual void StopAction() { } 
}