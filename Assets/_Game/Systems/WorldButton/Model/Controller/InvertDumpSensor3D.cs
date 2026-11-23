using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(KinematicTracker3D))] // ★ 強制要求掛載速度追蹤器
public class KineticDumpSensor3D : MonoBehaviour
{
    [Header("動能判定設定 (靈敏度)")]
    public float linearSpeedThreshold = 4.0f; 
    public float angularSpeedThreshold = 360.0f;

    [Header("冷卻與頻率")]
    public float cooldownTime = 0.5f;

    [Header("事件觸發")]
    public UnityEvent<Vector3> OnDumpTriggered;

    private KinematicTracker3D _tracker;
    private float _lastDumpTime = -999f;

    private void Awake()
    {
        // 抓取剛才做好的追蹤器
        _tracker = GetComponent<KinematicTracker3D>();
    }

    private void Update()
    {
        // 1. 檢查冷卻時間
        if (Time.time < _lastDumpTime + cooldownTime) return;

        // 2. 向 Tracker 索取現成的數據，不再自己算！
        bool isFlinging = _tracker.LinearSpeed >= linearSpeedThreshold;
        bool isSpinning = _tracker.AngularSpeed >= angularSpeedThreshold;

        // 3. 判斷是否滿足觸發條件
        if (isFlinging || isSpinning)
        {
            // 如果是甩出去，就用甩的速度；如果只是定在原地猛轉，就給一個模擬的噴發速度
            Vector3 finalEjectVelocity = isFlinging ? _tracker.LinearVelocity : (Random.insideUnitSphere * 5f);
            
            TriggerDump(finalEjectVelocity);
        }
    }

    private void TriggerDump(Vector3 ejectVelocity)
    {
        _lastDumpTime = Time.time;
        OnDumpTriggered?.Invoke(ejectVelocity);
    }
}