using UnityEngine;
using Gamemanager;

public class OneTurnSpeedModifier : MonoBehaviour
{
    private EntityRuntime _targetRuntime;
    private float _multiplier;

    public void ApplyBuff(EntityRuntime targetData, float multiplier)
    {
        if (targetData == null)
        {
            Destroy(this);
            return;
        }

        _targetRuntime = targetData;
        _multiplier = multiplier;
        
        // 1. 修改資料層的數值
        _targetRuntime.MultiplyMoveSpeed(_multiplier);

        // 2. 訂閱：當 Boss 回到發呆階段，解除 Buff
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterIdlePhaseEvent, OnBossIdle);
        }
    }

    private void OnBossIdle(BossEnterIdlePhaseEvent evt)
    {
        // 1. 資料層還原速度 (除回去)
        if (_targetRuntime != null)
        {
            _targetRuntime.MultiplyMoveSpeed(1f / _multiplier);
            Debug.Log("[狀態還原] 回合結束，玩家資料層速度倍率已恢復正常。");
        }
        
        // 2. 任務完成，自我銷毀
        Destroy(this);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterIdlePhaseEvent>(OnBossIdle);
        }
    }
}