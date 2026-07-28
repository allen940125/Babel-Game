using System;
using System.Collections;
using Gamemanager;
using UnityEngine;
using UnityEngine.InputSystem;

// ★ 徹底移除了 using UnityEngine.UI！
// ★ 徹底升級為純 3D Rigidbody！
// ★ 職責精簡：主角只負責「移動、衝刺、管理體力與承受傷害」，攻擊全權交給 Meta 介面按鈕！
[RequireComponent(typeof(Rigidbody))]
public class PlayerController3D : MonoBehaviour, IDamageable
{
    // ==========================================
    // 第一部分：GAS-Lite 狀態標籤系統
    // ==========================================
    [Flags]
    public enum PlayerStateFlags
    {
        None = 0,
        Normal = 1 << 0,
        Dashing = 1 << 1,     // 衝刺中 (鎖定一般移動)
        Invincible = 1 << 2,  // 無敵狀態
        Stunned = 1 << 3,     // 眩暈/被控制
        Dead = 1 << 4         // 死亡
    }

    [Header("即時狀態監控 (唯讀)")]
    [SerializeField] private PlayerStateFlags currentState = PlayerStateFlags.Normal;

    [Header("★ 資料庫綁定 (SSOT)")]
    [Tooltip("請拖入 SO_CurrentPlayer_Runtime.asset")]
    [SerializeField] private PlayerRuntimeSO playerSO;
    
    [Header("操控與硬核生存參數")]
    public float smoothTime = 0.08f;
    [Tooltip("消耗體力後，需等待幾秒才開始回復")]
    public float staminaRegenDelay = 0.8f;
    public float invincibilityDuration = 1.5f;
    public Color damageColor = Color.red;

    // --- 內部物理與狀態暫存 ---
    private bool _isDashCooldown = false;
    private float _lastStaminaConsumeTime;
    
    private Vector3 _currentInput;
    private Vector3 _currentVelocity;
    private Vector3 _dashDirection;
    
    private Rigidbody _rb;
    private SpriteRenderer _sr;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _sr = GetComponentInChildren<SpriteRenderer>();

        // ★ 核心物理規範：強力鎖死 Z 軸空間移動與 X/Y 軸旋轉
        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

        if (playerSO == null) Debug.LogError($"[致命錯誤] {gameObject.name} 未指派 PlayerRuntimeSO！");
    }

    // ==========================================
    // 第二部分：行為閘門判定 (Action Gates)
    // ==========================================
    
    private bool CanMove() => !currentState.HasFlag(PlayerStateFlags.Dashing) && 
                              !currentState.HasFlag(PlayerStateFlags.Stunned) && 
                              !currentState.HasFlag(PlayerStateFlags.Dead);

    // ★ 檢查體力時，直接向 playerSO 查詢！
    private bool CanDash() => CanMove() && 
                              !_isDashCooldown && 
                              playerSO != null && playerSO.CurrentStamina >= playerSO.DashCost && 
                              _currentInput != Vector3.zero;

    private bool CanTakeDamage() => !currentState.HasFlag(PlayerStateFlags.Invincible) && 
                                    !currentState.HasFlag(PlayerStateFlags.Dead);

    // ==========================================
    // 第三部分：遊戲主循環與輸入
    // ==========================================

    private void Update()
    {
        if (currentState.HasFlag(PlayerStateFlags.Dead)) return;

        HandleStaminaRegen();
        HandleInput();
        UpdateVisualColor();
    }

    private void HandleStaminaRegen()
    {
        if (playerSO == null) return;

        // ★ 硬核延遲回復：距離上次消耗體力超過 staminaRegenDelay 秒，才叫 SO 回復體力
        if (!currentState.HasFlag(PlayerStateFlags.Dashing) && 
            playerSO.CurrentStamina < playerSO.MaxStamina && 
            Time.time >= _lastStaminaConsumeTime + staminaRegenDelay)
        {
            playerSO.RegenStamina(20f, Time.deltaTime); // 20f 可進一步封裝入 SO

            GameManager.Instance.MainGameEvent.Send(new PlayerStaminaChangedEvent()
            {
                CurrentStamina = playerSO.CurrentStamina,
                MaxStamina = playerSO.MaxStamina
            });
        }
    }

    private void HandleInput()
    {
        if (Keyboard.current != null)
        {
            float x = 0; float y = 0;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y = 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y = -1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x = -1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x = 1;
            
            _currentInput = new Vector3(x, y, 0f).normalized;
        }

        // 僅保留衝刺輸入，已移除所有攻擊與射擊監聽
        if (Keyboard.current.leftShiftKey.wasPressedThisFrame && CanDash())
        {
            StartCoroutine(DashRoutine());
        }
    }

    private void FixedUpdate()
    {
        if (transform.position.z != 0f)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
        }

        if (currentState.HasFlag(PlayerStateFlags.Dashing))
        {
            _rb.linearVelocity = _dashDirection * (playerSO != null ? playerSO.DashSpeed : 20f);
        }
        else if (CanMove())
        {
            float speed = playerSO != null ? playerSO.MoveSpeed : 5f;
            Vector3 targetVelocity = _currentInput * speed;
            _rb.linearVelocity = Vector3.SmoothDamp(_rb.linearVelocity, targetVelocity, ref _currentVelocity, smoothTime);
        }
        else
        {
            _rb.linearVelocity = Vector3.zero;
        }
    }

    // ==========================================
    // 第四部分：生存動作實作 (Actions)
    // ==========================================

    private IEnumerator DashRoutine()
    {
        currentState |= PlayerStateFlags.Dashing;
        _isDashCooldown = true;
        _lastStaminaConsumeTime = Time.time;

        if (playerSO != null) playerSO.ConsumeStamina(playerSO.DashCost);

        GameManager.Instance.MainGameEvent.Send(new PlayerStaminaChangedEvent()
        {
            CurrentStamina = playerSO.CurrentStamina,
            MaxStamina = playerSO.MaxStamina
        });

        _dashDirection = _currentInput;

        yield return new WaitForSeconds(playerSO != null ? playerSO.DashDuration : 0.2f);

        currentState &= ~PlayerStateFlags.Dashing;
        _rb.linearVelocity = Vector3.zero;

        yield return new WaitForSeconds(playerSO != null ? playerSO.DashCooldown : 0.5f);
        _isDashCooldown = false;
    }

    // ==========================================
    // 第五部分：受擊委託與狀態反饋
    // ==========================================

    // ★ 實作 IDamageable：既然沒有戰鬥攻擊組件了，直接跟 playerSO 索取防禦力並結算！
    public void TakeDamage(DamagePayload payload)
    {
        if (!CanTakeDamage() || playerSO == null) return;

        // 1. 標準減傷公式：實際扣血 = max(1, 傳入傷害 - 自身防禦力)
        int finalDamage = Mathf.Max(1, payload.Damage - playerSO.Defense);
        playerSO.ModifyHealth(-finalDamage);

        Debug.Log($"<color=red>[玩家受傷] 來自 {payload.Source.name} | 承受傷害:{finalDamage} | 剩餘血量:{playerSO.CurrentHealth}/{playerSO.MaxHealth}</color>");

        // 2. 廣播 UI 事件
        GameManager.Instance.MainGameEvent.Send(new PlayerHealthChangedEvent()
        {
            CurrentHealth = playerSO.CurrentHealth,
            MaxHealth = playerSO.MaxHealth
        });

        // 3. 裁定生死
        if (playerSO.CurrentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityRoutine());
        }
    }

    private IEnumerator InvincibilityRoutine()
    {
        currentState |= PlayerStateFlags.Invincible;
        
        float flashInterval = 0.15f;
        float timer = 0;
        while (timer < invincibilityDuration)
        {
            if (_sr != null)
            {
                Color c = damageColor;
                c.a = (Mathf.FloorToInt(timer / flashInterval) % 2 == 0) ? 0.4f : 1f;
                _sr.color = c;
            }
            yield return null;
            timer += Time.deltaTime;
        }

        currentState &= ~PlayerStateFlags.Invincible;
    }

    private void Die()
    {
        Debug.Log("玩家死亡！");
        currentState = PlayerStateFlags.Dead;
        if (_sr != null) _sr.color = Color.gray;
        _rb.linearVelocity = Vector3.zero;
        this.enabled = false;
    }

    private void UpdateVisualColor()
    {
        if (_sr == null || currentState.HasFlag(PlayerStateFlags.Invincible) || playerSO == null) return;

        // ★ 瀕死判定：當前血量小於或等於最大血量的 20%
        if ((float)playerSO.CurrentHealth / playerSO.MaxHealth <= 0.2f)
        {
            float t = Mathf.PingPong(Time.time * 8f, 1f);
            _sr.color = Color.Lerp(Color.white, damageColor, t);
        }
        else
        {
            _sr.color = Color.Lerp(Color.red, Color.white, playerSO.StaminaRatio);
        }
    }
}