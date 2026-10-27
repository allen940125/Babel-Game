using UnityEngine;
using UnityEngine.Events;

public class KineticDumpSensor3D : MonoBehaviour
{
    [Header("動能判定設定 (靈敏度)")]
    public float linearSpeedThreshold = 4.0f; 
    public float angularSpeedThreshold = 360.0f;

    [Header("冷卻與頻率")]
    public float cooldownTime = 0.5f;

    [Header("事件觸發")]
    // ★ 變更：改用帶 Vector3 參數的 UnityEvent，用來傳遞甩動的慣性向量
    public UnityEvent<Vector3> OnDumpTriggered;

    private Vector3 _lastPosition;
    private Quaternion _lastRotation;
    private float _lastDumpTime = -999f;

    private void Start() => UpdateLastTransforms();

    private void Update()
    {
        if (Time.time < _lastDumpTime + cooldownTime)
        {
            UpdateLastTransforms();
            return;
        }
        CheckKineticEnergy();
    }

    private void CheckKineticEnergy()
    {
        // ★ 變更：不只算距離，我們直接算出帶有方向的「速度向量 (Velocity)」
        Vector3 linearVelocity = (transform.position - _lastPosition) / Time.deltaTime;
        float currentLinearSpeed = linearVelocity.magnitude;

        float angleChanged = Quaternion.Angle(transform.rotation, _lastRotation);
        float currentAngularSpeed = angleChanged / Time.deltaTime;

        bool isFlinging = currentLinearSpeed >= linearSpeedThreshold;
        bool isSpinning = currentAngularSpeed >= angularSpeedThreshold;

        if (isFlinging || isSpinning)
        {
            // 如果是純原地自轉，給一個隨機向外噴發的速度；如果是甩動，就用甩動的速度
            Vector3 finalEjectVelocity = isFlinging ? linearVelocity : (Random.insideUnitSphere * 5f);
            
            TriggerDump(finalEjectVelocity, currentLinearSpeed, currentAngularSpeed);
        }

        UpdateLastTransforms();
    }

    private void UpdateLastTransforms()
    {
        _lastPosition = transform.position;
        _lastRotation = transform.rotation;
    }

    private void TriggerDump(Vector3 ejectVelocity, float linearSpeed, float angularSpeed)
    {
        _lastDumpTime = Time.time;
        if (OnDumpTriggered != null)
        {
            OnDumpTriggered.Invoke(ejectVelocity);
        }
    }
}