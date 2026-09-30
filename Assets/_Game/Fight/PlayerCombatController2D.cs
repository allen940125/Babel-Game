using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// 命名修正：明確標示這是「戰鬥時」且基於「2D 平面邏輯」的控制器
[RequireComponent(typeof(Rigidbody))]
public class PlayerCombatController2D : MonoBehaviour, IEntityRuntimeDependent
{
    private EntityRuntime _entityData;
    private StaminaTrait _staminaTrait;

    [Header("操控與硬核生存參數")]
    public float smoothTime = 0.08f;
    public float staminaRegenDelay = 0.8f;
    
    private bool _isDashCooldown = false;
    private float _lastStaminaConsumeTime;
    
    private Vector3 _currentInput;
    private Vector3 _currentVelocity;
    private Vector3 _dashDirection;
    
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        // 鎖定 Z 軸，確立其 2D 物理本質
        _rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
    }

    // ==========================================
    // ★ 徹底解耦：刪除 Start，強制透過介面被動接收資料
    // ==========================================
    public void OnRuntimeDataChanged(EntityRuntime newData)
    {
        _entityData = newData;
        if (_entityData != null)
        {
            if (!_entityData.TryGetTrait(out _staminaTrait))
            {
                Debug.LogWarning($"[系統警告] {gameObject.name} 缺少 StaminaTrait，衝刺系統將被禁用！");
            }
        }
    }

    private bool CanMove()
    {
        if (_entityData == null) return false;
        return !_entityData.HasState(EntityStateFlags.Dashing) && 
               !_entityData.HasState(EntityStateFlags.Stunned) && 
               !_entityData.HasState(EntityStateFlags.Dead);
    }

    private bool CanDash() => CanMove() && 
                              !_isDashCooldown && 
                              _staminaTrait != null && 
                              _staminaTrait.currentStamina >= _staminaTrait.dashCost && 
                              _currentInput != Vector3.zero;

    private void Update()
    {
        if (_entityData == null || _entityData.HasState(EntityStateFlags.Dead)) return;

        HandleStaminaRegen();
        HandleInput();
    }

    private void HandleStaminaRegen()
    {
        if (_staminaTrait == null) return; 

        if (!_entityData.HasState(EntityStateFlags.Dashing) && 
            _staminaTrait.currentStamina < _staminaTrait.maxStamina && 
            Time.time >= _lastStaminaConsumeTime + staminaRegenDelay)
        {
            // ★ 解耦修正：只管呼叫數據改變，刪除所有 GameManager 的廣播。
            // 讓 UI 去監聽 StaminaTrait.OnStaminaRatioChanged 即可。
            _staminaTrait.RegenStamina(20f, Time.deltaTime); 
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

        if (Keyboard.current.spaceKey.wasPressedThisFrame && CanDash())
        {
            StartCoroutine(DashRoutine());
        }
    }

    private void FixedUpdate()
    {
        // 防呆校正
        if (transform.position.z != 0f)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
        }

        if (_entityData != null && _entityData.HasState(EntityStateFlags.Dashing))
        {
            float dashSpd = _staminaTrait != null ? _staminaTrait.dashSpeed : 20f;
            _rb.linearVelocity = _dashDirection * dashSpd;
        }
        else if (CanMove())
        {
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
        _entityData.AddState(EntityStateFlags.Dashing | EntityStateFlags.Invincible);
        
        _isDashCooldown = true;
        _lastStaminaConsumeTime = Time.time;

        if (_staminaTrait != null) 
        {
            // ★ 解耦修正：同上，只負責消耗數值，不再插手 UI 廣播
            _staminaTrait.ConsumeStamina(_staminaTrait.dashCost);
        }

        _dashDirection = _currentInput;

        float duration = _staminaTrait != null ? _staminaTrait.dashDuration : 0.2f;
        yield return new WaitForSeconds(duration);

        _entityData.RemoveState(EntityStateFlags.Dashing | EntityStateFlags.Invincible);
        _rb.linearVelocity = Vector3.zero;

        float cooldown = _staminaTrait != null ? _staminaTrait.dashCooldown : 0.5f;
        yield return new WaitForSeconds(cooldown);
        _isDashCooldown = false;
    }
}