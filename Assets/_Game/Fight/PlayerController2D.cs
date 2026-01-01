using System.Collections;
using Gamemanager; // 引用事件系統
using UnityEngine;
using UnityEngine.InputSystem; 

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("狀態控制")]
    public bool canShoot = false; // 控制是否可以射擊 (預設 false，等 Boss 虛弱才開)

    [Header("移動參數")]
    public float moveSpeed = 5f;
    [Tooltip("數值越小反應越快，數值越大慣性越大 (建議 0.05 ~ 0.1)")]
    public float smoothTime = 0.08f;

    [Header("戰鬥參數")]
    public int maxHealth = 3;
    public float invincibilityDuration = 1.5f; 

    [Header("射擊參數")]
    public GameObject projectilePrefab; 
    public Transform firePoint;         
    public float projectileSpeed = 10f; 

    // 內部變數
    private Vector2 _currentInput;
    private Vector2 _currentVelocity; 
    private bool _isInvincible = false;
    private int _currentHealth;

    // 元件快取
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Camera _mainCamera; 

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponentInChildren<SpriteRenderer>(); 
        
        if (Camera.main != null) _mainCamera = Camera.main;
        else _mainCamera = Object.FindFirstObjectByType<Camera>();

        _currentHealth = maxHealth;

        _rb.gravityScale = 0; 
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; 
        _rb.freezeRotation = true; 
        
        // --- 事件註冊區 ---
        if(GameManager.Instance != null && GameManager.Instance.MainGameEvent != null)
        {
            // 1. 當 Boss 進入虛弱 (Vulnerable) -> 解鎖攻擊 (EnableShooting)
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterVulnerablePhaseEvent, EnableShooting);
            
            // 2. 當 Boss 進入待機 (Idle) -> 上鎖攻擊 (DisableShooting)
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterAttackingPhaseEvent, DisableShooting);
        }
    }

    private void OnDisable() // 注意：這裡是 OnDisable 不是 virtual void OnDisable
    {
        if (GameManager.Instance != null && GameManager.Instance.MainGameEvent != null)
        {
            // 記得取消訂閱，對應上面的函式
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterVulnerablePhaseEvent>(EnableShooting);
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterAttackingPhaseEvent>(DisableShooting);
        }
    }
    
    // --- 事件回調函式 ---

    // 解鎖攻擊 (對應 Boss 虛弱)
    private void EnableShooting(BossEnterVulnerablePhaseEvent evt)
    {
        canShoot = true;
        Debug.Log("玩家攻擊解鎖！(Boss 虛弱)");
    }

    // 上鎖攻擊 (對應 Boss 待機/休息)
    private void DisableShooting(BossEnterAttackingPhaseEvent evt)
    {
        canShoot = false;
        Debug.Log("玩家攻擊鎖定！(Boss 攻擊)");
    }

    private void Update()
    {
        // 1. 移動輸入 (移動通常不需要鎖，如果需要也可以加 !canShoot return)
        if (Keyboard.current != null)
        {
            float x = 0;
            float y = 0;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y = 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y = -1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x = -1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x = 1;

            _currentInput = new Vector2(x, y).normalized;
        }

        // 2. 射擊輸入
        // --- 關鍵修改：加上 && canShoot ---
        if (canShoot && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }
    }

    private void FixedUpdate()
    {
        Vector2 targetVelocity = _currentInput * moveSpeed;
        _rb.linearVelocity = Vector2.SmoothDamp(_rb.linearVelocity, targetVelocity, ref _currentVelocity, smoothTime);
    }

    private void Shoot()
    {
        if (projectilePrefab == null || firePoint == null) return;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0; 

        Vector2 direction = (mouseWorldPos - firePoint.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        GameObject bullet = Instantiate(projectilePrefab, firePoint.position, rotation);

        if (bullet.TryGetComponent(out Rigidbody2D bulletRb))
        {
            bulletRb.linearVelocity = direction * projectileSpeed; 
        }
    }

    public void TakeDamage(int damage)
    {
        if (_isInvincible || _currentHealth <= 0) return;
        _currentHealth -= damage;
        if (_currentHealth <= 0) Die();
        else StartCoroutine(InvincibilityRoutine());
    }

    private IEnumerator InvincibilityRoutine()
    {
        _isInvincible = true;
        float flashInterval = 0.1f;
        float timer = 0;
        while (timer < invincibilityDuration)
        {
            if (_sr != null)
            {
                Color c = _sr.color;
                c.a = (c.a == 1f) ? 0.2f : 1f; 
                _sr.color = c;
            }
            yield return new WaitForSeconds(flashInterval);
            timer += flashInterval;
        }
        if (_sr != null)
        {
            Color finalColor = _sr.color;
            finalColor.a = 1f;
            _sr.color = finalColor;
        }
        _isInvincible = false;
    }

    private void Die()
    {
        Debug.Log("玩家死亡！");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("EnemyBullet"))
        {
            TakeDamage(1);
            Destroy(other.gameObject);
        }
    }
}