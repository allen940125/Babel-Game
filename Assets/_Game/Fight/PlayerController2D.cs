using System.Collections;
using Gamemanager;
using UnityEngine;
using UnityEngine.InputSystem; 
using UnityEngine.UI; // ★ 記得加回這個！

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("狀態控制")]
    public bool canShoot = false; 

    [Header("移動參數")]
    public float moveSpeed = 5f;
    public float smoothTime = 0.08f;

    [Header("體力與衝刺設定 (UI Slider版)")] 
    // ★ 改回 Slider，直接拖拉就好，不用管座標
    public Slider staminaSlider; 

    public float maxStamina = 100f;  
    public float staminaRegenRate = 20f; 
    public float dashCost = 30f;     
    public float dashSpeed = 20f;    
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.5f;

    [Header("戰鬥參數")]
    public int maxHealth = 4;
    public float invincibilityDuration = 1.5f; 
    public Color damageColor = Color.red; 
    public GameObject healthBarContainer;
    public GameObject healthIconPrefab;
    public GameObject projectilePrefab; 
    public Transform firePoint;         
    public float projectileSpeed = 10f; 

    // 內部變數
    private Vector2 _currentInput;
    private Vector2 _currentVelocity; 
    private bool _isInvincible = false;
    private int _currentHealth;
    private float _currentStamina;
    private bool _isDashing = false; 
    private bool _canDash = true;    
    private Vector2 _dashDirection;  
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
        _currentStamina = maxStamina; 
        
        InitHealthBar();

        // ★ 初始化 Slider
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = _currentStamina;
        }

        _rb.gravityScale = 0; 
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; 
        _rb.freezeRotation = true; 
        
        if(GameManager.Instance != null && GameManager.Instance.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterVulnerablePhaseEvent, EnableShooting);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterAttackingPhaseEvent, DisableShooting);
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null && GameManager.Instance.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterVulnerablePhaseEvent>(EnableShooting);
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterAttackingPhaseEvent>(DisableShooting);
        }
    }
    private void InitHealthBar()
    {
        if (healthBarContainer == null || healthIconPrefab == null) return;
        foreach (Transform child in healthBarContainer.transform) Destroy(child.gameObject);
        int visibleHearts = maxHealth - 1;
        for (int i = 0; i < visibleHearts; i++) Instantiate(healthIconPrefab, healthBarContainer.transform);
    }
    private void RemoveOneHeart()
    {
        if (healthBarContainer != null && healthBarContainer.transform.childCount > 0)
            Destroy(healthBarContainer.transform.GetChild(healthBarContainer.transform.childCount - 1).gameObject);
    }
    private void EnableShooting(BossEnterVulnerablePhaseEvent evt) { canShoot = true; }
    private void DisableShooting(BossEnterAttackingPhaseEvent evt) { canShoot = false; }

    private void Update()
    {
        // 0. 體力恢復
        if (!_isDashing && _currentStamina < maxStamina)
        {
            _currentStamina += staminaRegenRate * Time.deltaTime;
            if (_currentStamina > maxStamina) _currentStamina = maxStamina;
            
            // ★ 更新 Slider
            if (staminaSlider != null) staminaSlider.value = _currentStamina;
        }

        if (_isDashing) return;

        if (Keyboard.current != null)
        {
            float x = 0; float y = 0;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y = 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y = -1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x = -1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x = 1;
            _currentInput = new Vector2(x, y).normalized;
        }

        if (Keyboard.current.leftShiftKey.wasPressedThisFrame && _canDash && _currentStamina >= dashCost && _currentInput != Vector2.zero)
        {
            StartCoroutine(DashRoutine());
        }

        if (canShoot && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }

        if (_sr != null)
        {
            // 情況 A: 無敵狀態
            if (_isInvincible)
            {
                // 什麼都不做，交給協程 InvincibilityRoutine 去控制閃爍
            }
            // 情況 B: 瀕死狀態 (剩1血)
            else if (_currentHealth == 1)
            {
                // 優先顯示瀕死警示 (呼吸燈閃爍)
                float t = Mathf.PingPong(Time.time * 8f, 1f);
                _sr.color = Color.Lerp(Color.white, damageColor, t);
            }
            // 情況 C: 一般狀態 (根據體力變色)
            else
            {
                // 1. 計算體力百分比 (0.0 ~ 1.0)
                float staminaRatio = _currentStamina / maxStamina;

                // 2. 顏色插值
                // staminaRatio = 0 時顯示 Red
                // staminaRatio = 1 時顯示 White
                // 中間會自動過渡 (例如 0.5 就是粉紅色)
                _sr.color = Color.Lerp(Color.red, Color.white, staminaRatio);
            }
        }
    }

    private void FixedUpdate()
    {
        if (_isDashing)
        {
            _rb.linearVelocity = _dashDirection * dashSpeed;
        }
        else
        {
            Vector2 targetVelocity = _currentInput * moveSpeed;
            _rb.linearVelocity = Vector2.SmoothDamp(_rb.linearVelocity, targetVelocity, ref _currentVelocity, smoothTime);
        }
    }

    private IEnumerator DashRoutine()
    {
        _isDashing = true;
        _canDash = false;
        
        _currentStamina -= dashCost;
        if (staminaSlider != null) staminaSlider.value = _currentStamina; // ★ 更新 Slider

        _dashDirection = _currentInput;

        yield return new WaitForSeconds(dashDuration);

        _isDashing = false;
        _rb.linearVelocity = Vector2.zero; 

        yield return new WaitForSeconds(dashCooldown);
        _canDash = true;
    }

    // ... (Shoot, TakeDamage, Die 等保持不變) ...
    private void Shoot() {
        if (projectilePrefab == null || firePoint == null) return;
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0; 
        Vector2 direction = (mouseWorldPos - firePoint.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);
        GameObject bullet = Instantiate(projectilePrefab, firePoint.position, rotation);
        if (bullet.TryGetComponent(out Rigidbody2D bulletRb)) bulletRb.linearVelocity = direction * projectileSpeed; 
    }
    public void TakeDamage(int damage) {
        if (_isInvincible || _currentHealth <= 0) return;
        _currentHealth -= damage;
        RemoveOneHeart();
        if (_currentHealth <= 0) Die();
        else StartCoroutine(InvincibilityRoutine());
    }
    private IEnumerator InvincibilityRoutine() {
        _isInvincible = true;
        if (_sr != null) _sr.color = damageColor;
        yield return new WaitForSeconds(0.1f);
        float flashInterval = 0.15f;
        float timer = 0;
        while (timer < invincibilityDuration) {
            if (_sr != null) {
                Color c = damageColor;
                c.a = (Mathf.FloorToInt(timer / flashInterval) % 2 == 0) ? 0.4f : 1f; 
                _sr.color = c;
            }
            yield return null;
            timer += Time.deltaTime;
        }
        _isInvincible = false;
        if (_sr != null) { Color finalColor = Color.white; finalColor.a = 1f; _sr.color = finalColor; }
    }
    private void Die() { Debug.Log("玩家死亡！"); if (_sr != null) _sr.color = Color.gray; this.enabled = false; }
}