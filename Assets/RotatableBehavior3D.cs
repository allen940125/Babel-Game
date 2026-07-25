using UnityEngine;

public class RotatableBehavior3D : MonoBehaviour, IRotateHandler3D
{
    [Header("旋轉模式")]
    [SerializeField] private bool useArcball = true;

    [Header("傳統繞軸設定 (Yaw/Pitch)")]
    [SerializeField] private bool invertX = false;
    [SerializeField] private bool invertY = false;
    [SerializeField] private Vector3 yawAxis = Vector3.up;
    [SerializeField] private Vector3 pitchAxis = Vector3.right;
    [SerializeField] private float sensitivity = 0.5f;

    // ---- Arcball 核心狀態 ----
    private Quaternion _initialRotation; // 右鍵按下瞬間的原始旋轉絕對值
    private Vector3 _arcballStartVector; // 從物體中心指向點擊點的初始單位向量
    private float _arcballRadius;        // 虛擬旋轉球的半徑

    public void OnRotateStart(Vector3 hitPoint)
    {
        if (!enabled) return;

        // 1. 永遠記錄按下瞬間的絕對旋轉值，防止 Delta 累加造成的幾何級數暴走
        _initialRotation = transform.rotation;

        if (useArcball)
        {
            // 2. 計算虛擬包圍球半徑
            _arcballRadius = Vector3.Distance(hitPoint, transform.position);
            // 3. 記錄初始參考向量 V0
            _arcballStartVector = (hitPoint - transform.position).normalized;
        }
    }

    public void OnRotate(Ray cameraRay, Vector2 screenDelta)
    {
        if (!enabled) return;

        if (useArcball)
        {
            // 透過射線與虛擬球體交點，取得當前滑鼠在球面上的目標向量 V1
            Vector3 currentVec = GetArcballVector(cameraRay);
            
            // 計算從 V0 到 V1 的四元數旋轉差
            Quaternion deltaRot = Quaternion.FromToRotation(_arcballStartVector, currentVec);
            
            // ★ 嚴格數學規則：總差值 (deltaRot) 必須乘上初始狀態 (_initialRotation)，絕對不能乘 transform.rotation！
            transform.rotation = deltaRot * _initialRotation;
        }
        else
        {
            // 傳統繞軸平滑運算 (依賴螢幕位移差)
            float dirX = invertX ? -1f : 1f;
            float dirY = invertY ? -1f : 1f;
            float yawAmount = -screenDelta.x * dirX * sensitivity;
            float pitchAmount = screenDelta.y * dirY * sensitivity;

            transform.Rotate(yawAxis, yawAmount, Space.World);
            transform.Rotate(pitchAxis, pitchAmount, Space.World);
        }
    }

    /// <summary>
    /// 射線與虛擬球體交點運算 (Shoemake Arcball 幾何投影)
    /// </summary>
    private Vector3 GetArcballVector(Ray ray)
    {
        Vector3 centerToOrigin = ray.origin - transform.position;
        float a = Vector3.Dot(ray.direction, ray.direction);
        float b = 2.0f * Vector3.Dot(centerToOrigin, ray.direction);
        float c = Vector3.Dot(centerToOrigin, centerToOrigin) - (_arcballRadius * _arcballRadius);
        
        float discriminant = (b * b) - (4 * a * c);

        if (discriminant < 0)
        {
            // 【邊緣保護機制】當滑鼠移出虛擬球體外時，將點投影到球體對著攝影機的輪廓邊緣
            // 避免判別式為負數時旋轉卡死，維持 Trackball 的連續操作體驗
            float tClosest = -b / (2.0f * a);
            Vector3 closestPointOnRay = ray.origin + ray.direction * tClosest;
            return (closestPointOnRay - transform.position).normalized;
        }
        else
        {
            // 命中虛擬球體表面，解出最靠近攝影機的交點 t
            float t = (-b - Mathf.Sqrt(discriminant)) / (2.0f * a);
            Vector3 hitPoint = ray.origin + ray.direction * t;
            return (hitPoint - transform.position).normalized;
        }
    }
}