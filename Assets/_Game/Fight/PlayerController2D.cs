using System;
using System.Collections;
using Gamemanager;
using UnityEngine;
using UnityEngine.InputSystem;

// ★ 徹底移除了 using UnityEngine.UI！
// ★ 徹底升級為純 3D Rigidbody！
// ★ 職責精簡：主角只負責「移動、衝刺、管理體力與承受傷害」，攻擊全權交給 Meta 介面按鈕！
[RequireComponent(typeof(Rigidbody))]
public class PlayerController3D : MonoBehaviour
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
    [SerializeField] private EntityRuntimeSO playerSO;
    
    // ★ 新增：用來快取 (Cache) 體力特徵的變數
    private StaminaTrait _staminaTrait;

    [Header("操控與硬核生存參數")]
    public float smoothTime = 0.08f;
    public float staminaRegenDelay = 0.8f;
    
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

        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

        if (playerSO == null) 
        {
            Debug.LogError($"[致命錯誤] {gameObject.name} 未指派 EntityRuntimeSO！");
            return;
        }

        // ==========================================
        // ★ 核心變更：在 Awake 階段索取並快取特徵！
        // ==========================================
        if (!playerSO.TryGetTrait(out _staminaTrait))
        {
            Debug.LogWarning($"[系統警告] {gameObject.name} 的 SO 沒有掛載 StaminaTrait！將無法使用體力與衝刺系統！");
        }
    }

    private bool CanMove() => !currentState.HasFlag(PlayerStateFlags.Dashing) && 
                              !currentState.HasFlag(PlayerStateFlags.Stunned) && 
                              !currentState.HasFlag(PlayerStateFlags.Dead);

    // ★ 核心變更：直接向快取好的 _staminaTrait 詢問數值
    private bool CanDash() => CanMove() && 
                              !_isDashCooldown && 
                              _staminaTrait != null && // 確保有拿到特徵
                              _staminaTrait.currentStamina >= _staminaTrait.dashCost && 
                              _currentInput != Vector3.zero;

    private void Update()
    {
        if (currentState.HasFlag(PlayerStateFlags.Dead)) return;

        HandleStaminaRegen();
        HandleInput();
        UpdateVisualColor();
    }

    private void HandleStaminaRegen()
    {
        // ★ 核心變更：防呆檢查
        if (_staminaTrait == null) return; 

        if (!currentState.HasFlag(PlayerStateFlags.Dashing) && 
            _staminaTrait.currentStamina < _staminaTrait.maxStamina && 
            Time.time >= _lastStaminaConsumeTime + staminaRegenDelay)
        {
            // 向特徵呼叫回復體力
            _staminaTrait.RegenStamina(20f, Time.deltaTime); 

            GameManager.Instance.MainGameEvent.Send(new PlayerStaminaChangedEvent()
            {
                CurrentStamina = _staminaTrait.currentStamina,
                MaxStamina = _staminaTrait.maxStamina
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
            // ★ 核心變更：讀取特徵的 dashSpeed
            float dashSpd = _staminaTrait != null ? _staminaTrait.dashSpeed : 20f;
            _rb.linearVelocity = _dashDirection * dashSpd;
        }
        else if (CanMove())
        {
            // ★ 核心變更：讀取特徵的 moveSpeed
            float moveSpd = _staminaTrait != null ? _staminaTrait.moveSpeed : 5f;
            Vector3 targetVelocity = _currentInput * moveSpd;
            _rb.linearVelocity = Vector3.SmoothDamp(_rb.linearVelocity, targetVelocity, ref _currentVelocity, smoothTime);
        }
        else
        {
            _rb.linearVelocity = Vector3.zero;
        }
    }

    private IEnumerator DashRoutine()
    {
        currentState |= PlayerStateFlags.Dashing;
        _isDashCooldown = true;
        _lastStaminaConsumeTime = Time.time;

        // ★ 核心變更：呼叫特徵扣除體力
        if (_staminaTrait != null) 
        {
            _staminaTrait.ConsumeStamina(_staminaTrait.dashCost);

            GameManager.Instance.MainGameEvent.Send(new PlayerStaminaChangedEvent()
            {
                CurrentStamina = _staminaTrait.currentStamina,
                MaxStamina = _staminaTrait.maxStamina
            });
        }

        _dashDirection = _currentInput;

        float duration = _staminaTrait != null ? _staminaTrait.dashDuration : 0.2f;
        yield return new WaitForSeconds(duration);

        currentState &= ~PlayerStateFlags.Dashing;
        _rb.linearVelocity = Vector3.zero;

        float cooldown = _staminaTrait != null ? _staminaTrait.dashCooldown : 0.5f;
        yield return new WaitForSeconds(cooldown);
        _isDashCooldown = false;
    }

    private void UpdateVisualColor()
    {
        if (_sr == null || currentState.HasFlag(PlayerStateFlags.Invincible) || playerSO == null) return;

        // 瀕死判定：核心血量依然在 EntityRuntimeSO 裡，所以直接用 playerSO 讀取
        if ((float)playerSO.CurrentHealth / playerSO.MaxHealth <= 0.2f)
        {
            float t = Mathf.PingPong(Time.time * 8f, 1f);
            //_sr.color = Color.Lerp(Color.white, damageColor, t);
        }
        // ★ 核心變更：體力比例向 _staminaTrait 讀取
        else if (_staminaTrait != null)
        {
            _sr.color = Color.Lerp(Color.red, Color.white, _staminaTrait.StaminaRatio);
        }
    }
}