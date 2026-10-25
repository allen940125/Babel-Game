using UnityEngine;

[RequireComponent(typeof(EnemyBullet), typeof(Rigidbody))]
public class PlayerThrowableController : MonoBehaviour
{
    [Header("飛行控制")]
    public float activeDuration = 2.0f; // 最長飛行時間
    public float airFriction = 15f;     // 空氣阻力 (數值越大減速越快)
    public float dropSpeedThreshold = 2f; // 速度低於多少時直接掉落

    private float _throwTime;
    
    private EnemyBullet _bulletScript;
    private Rigidbody _rb;
    private Collider _col;
    private ItemPickupTrigger _pickupTrigger;
    
    private bool _isFlying = false;

    private void Awake()
    {
        _bulletScript = GetComponent<EnemyBullet>();
        _rb = GetComponent<Rigidbody>();
        _col = GetComponentInChildren<Collider>();
        _pickupTrigger = GetComponent<ItemPickupTrigger>();
    }

    public void Throw(Vector3 direction, float speed)
    {
        _isFlying = true;
        _throwTime = Time.time;
    
        // 1. 防呆檢查：確保元件都有抓到
        if (_bulletScript == null) 
        {
            Debug.LogError($"[錯誤] {gameObject.name} 缺少 EnemyBullet 腳本！");
            return;
        }
        if (_col == null)
        {
            Debug.LogError($"[錯誤] {gameObject.name} 缺少 Collider 元件！");
            return;
        }

        // 2. 初始化子彈
        _bulletScript.Initialize(null, 1f, false);
    
        // 3. 切換狀態
        _col.enabled = false; 
        if (_pickupTrigger != null) _pickupTrigger.enabled = false;
    
        // 4. 發射
        _bulletScript.SetTrajectory(direction, speed, false);
    }

    private void Update()
    {
        if (!_isFlying) return;

        // ★ 核心介入：每幀強迫 EnemyBullet 減速 (模擬空氣阻力)
        // 使用 Mathf.MoveTowards 讓速度平滑降到 0
        _bulletScript.currentSpeed = Mathf.MoveTowards(_bulletScript.currentSpeed, 0f, airFriction * Time.deltaTime);

        // ★ 判斷掉落條件：時間到了，或是速度慢到一個極限了
        if (Time.time > _throwTime + activeDuration || _bulletScript.currentSpeed <= dropSpeedThreshold)
        {
            ForceConvertToPhysical();
        }
    }

    private void ForceConvertToPhysical()
    {
        _isFlying = false;
        
        // 1. 紀錄最後殘餘的動能
        Vector3 leftoverVelocity = _bulletScript.currentDirection * _bulletScript.currentSpeed;
        
        // 2. 拔掉 EnemyBullet 的控制權
        _bulletScript.enabled = false;
        
        // 3. 把控制權交還給 Unity 物理引擎
        _col.enabled = true;
        _col.isTrigger = false;
        _rb.isKinematic = false;
        _rb.useGravity = true;
        
        // 4. 動能無縫接軌，讓它自然掉落
        _rb.linearVelocity = leftoverVelocity; 
        _rb.AddTorque(Random.insideUnitSphere * 15f, ForceMode.Impulse);
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (!_isFlying && _rb.linearVelocity.magnitude < 0.5f)
        {
            if (_pickupTrigger != null) _pickupTrigger.enabled = true;
        }
    }
}