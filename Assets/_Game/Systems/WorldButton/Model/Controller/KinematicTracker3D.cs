using UnityEngine;

/// <summary>
/// 終極物理動能追蹤器：精準計算過濾後的「線速度(位移)」與「角速度(旋轉)」
/// </summary>
public class KinematicTracker3D : MonoBehaviour
{
    [Header("★ 線速度過濾 (Linear 位移)")]
    public bool trackLinearX = true;
    public bool trackLinearY = true;
    public bool trackLinearZ = false; // 預設關閉 Z 軸位移

    [Header("★ 角速度過濾 (Angular 旋轉)")]
    public bool trackAngularX = true; // Pitch (點頭方向)
    public bool trackAngularY = true; // Yaw (搖頭方向)
    public bool trackAngularZ = true; // Roll (轉方向盤方向)

    [Header("★ 即時數據 (唯讀)")]
    [SerializeField, Tooltip("當前位移速度向量 (m/s)")] 
    private Vector3 currentLinearVelocity;
    [SerializeField, Tooltip("當前位移速率 (純數字)")] 
    private float currentLinearSpeed;
    
    [Space]
    [SerializeField, Tooltip("當前旋轉速度向量 (度/秒)")] 
    private Vector3 currentAngularVelocity;
    [SerializeField, Tooltip("當前旋轉速率 (純數字)")] 
    private float currentAngularSpeed;

    // ==========================================
    // 對外開放的公開屬性 (其他腳本一律讀取這裡)
    // ==========================================
    public Vector3 LinearVelocity => currentLinearVelocity;
    public float LinearSpeed => currentLinearSpeed;
    public Vector3 AngularVelocity => currentAngularVelocity;
    public float AngularSpeed => currentAngularSpeed;

    private Vector3 _lastPosition;
    private Quaternion _lastRotation;

    private void Start()
    {
        _lastPosition = transform.position;
        _lastRotation = transform.rotation;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // ==========================================
        // 1. 計算線速度 (Linear Velocity)
        // ==========================================
        Vector3 deltaPos = transform.position - _lastPosition;
        
        // 軸向過濾 (如果沒打勾，就當作那個軸沒移動)
        if (!trackLinearX) deltaPos.x = 0f;
        if (!trackLinearY) deltaPos.y = 0f;
        if (!trackLinearZ) deltaPos.z = 0f;

        currentLinearVelocity = deltaPos / dt;
        currentLinearSpeed = currentLinearVelocity.magnitude;

        // ==========================================
        // 2. 計算角速度 (Angular Velocity)
        // ==========================================
        // 計算這一幀到上一幀的旋轉差異 (Quaternion 相除)
        Quaternion deltaRot = transform.rotation * Quaternion.Inverse(_lastRotation);
        
        // 將四元數轉換為「旋轉軸向(axis)」與「旋轉角度(angle)」
        deltaRot.ToAngleAxis(out float angle, out Vector3 axis);

        // 防呆 1：ToAngleAxis 的角度範圍是 0~360，如果超過 180 代表是反方向轉，要修正為負數
        if (angle > 180f) angle -= 360f;

        // 防呆 2：如果角度不是 0，且軸向沒有因為除以 0 變成無限大
        if (angle != 0f && !float.IsInfinity(axis.x)) 
        {
            // 算出未過濾的角速度向量 (度/秒)
            Vector3 rawAngularVel = axis.normalized * (angle / dt);
            
            // 軸向過濾 (如果你不想算玩家轉方向盤的動作，就把 trackAngularZ 關掉)
            if (!trackAngularX) rawAngularVel.x = 0f;
            if (!trackAngularY) rawAngularVel.y = 0f;
            if (!trackAngularZ) rawAngularVel.z = 0f;

            currentAngularVelocity = rawAngularVel;
            currentAngularSpeed = currentAngularVelocity.magnitude;
        }
        else
        {
            // 完全沒轉動
            currentAngularVelocity = Vector3.zero;
            currentAngularSpeed = 0f;
        }

        // ==========================================
        // 3. 記錄狀態供下一幀使用
        // ==========================================
        _lastPosition = transform.position;
        _lastRotation = transform.rotation;
    }
}