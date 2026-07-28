using UnityEngine;

[CreateAssetMenu(fileName = "SO_BossRuntimeData", menuName = "Game/Runtime Data/Boss Entity Data")]
public class BossRuntimeSO : BaseEntityRuntimeSO
{
    [Header("Boss 專屬：攻擊階段計時器")]
    [SerializeField] private float currentTimer;
    [SerializeField] private float maxTimer;

    public float CurrentTimer => currentTimer;
    public float TimerRatio => maxTimer > 0 ? Mathf.Clamp01(currentTimer / maxTimer) : 0f;

    public void StartTimer(float duration)
    {
        maxTimer = duration;
        currentTimer = duration;
    }

    // 陷阱撞到時，呼叫這行：同時扣血又扣時間！
    public void ReduceTimer(float seconds)
    {
        currentTimer = Mathf.Max(0f, currentTimer - seconds);
        Debug.Log($"[BossSO] 時間被外力扣除 {seconds} 秒！剩餘: {currentTimer:.00} s");
    }

    public bool TickTimer(float deltaTime)
    {
        if (currentTimer <= 0f) return false;
        currentTimer -= deltaTime;
        if (currentTimer <= 0f)
        {
            currentTimer = 0f;
            return true; // 時間歸零，通知 Boss 切換狀態
        }
        return false;
    }
}