// using UnityEngine;
//
// [CreateAssetMenu(fileName = "SO_BossRuntimeData", menuName = "Game/Runtime Data/Boss Entity Data")]
// public class BossRuntimeSO : BaseEntityRuntimeSO
// {
//     [Header("★ Boss 專屬：攻擊階段計時器")]
//     [SerializeField] private float currentTimer;
//     [SerializeField] private float maxTimer;
//     
//     // ★ 核心修復：防止歸零後返回 false 的致能鎖
//     private bool _isTimerFinished = false;
//
//     public float CurrentTimer => currentTimer;
//     public float TimerRatio => maxTimer > 0 ? Mathf.Clamp01(currentTimer / maxTimer) : 0f;
//
//     public void StartTimer(float duration)
//     {
//         maxTimer = duration;
//         currentTimer = duration;
//         _isTimerFinished = false;
//     }
//
//     // ★ 道具吃到時調用：不管扣多少，只要扣到 <= 0，立刻標記為結束！
//     public void ReduceTimer(float seconds)
//     {
//         if (_isTimerFinished) return;
//         
//         currentTimer = Mathf.Max(0f, currentTimer - seconds);
//         Debug.Log($"<color=cyan>[BossSO] 攻擊時間被外力扣除 {seconds} 秒！剩餘: {currentTimer:.00} s</color>");
//         
//         if (currentTimer <= 0f)
//         {
//             currentTimer = 0f;
//             _isTimerFinished = true; // 標記為完成，供下一幀 Tick 抓取！
//         }
//     }
//
//     // ★ 嚴格仲裁：確保歸零那一瞬間絕對能回傳 true
//     public bool TickTimer(float deltaTime)
//     {
//         if (_isTimerFinished)
//         {
//             _isTimerFinished = false; // 觸發一次後立刻重置，防止重複觸發
//             return true; 
//         }
//
//         if (currentTimer <= 0f) return false;
//
//         currentTimer -= deltaTime;
//         if (currentTimer <= 0f)
//         {
//             currentTimer = 0f;
//             _isTimerFinished = true;
//             return true;
//         }
//         return false;
//     }
// }