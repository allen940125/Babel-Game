using System;
using System.Collections;
using Gamemanager;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

// ★ 徹底移除了 using UnityEngine.UI！
// ★ 徹底升級為純 3D Rigidbody！
// ★ 職責精簡：主角只負責「移動、衝刺、管理體力與承受傷害」，攻擊全權交給 Meta 介面按鈕！
[RequireComponent(typeof(Rigidbody))]
public class PlayerController3D : MonoBehaviour
{
    [FormerlySerializedAs("playerSO")]
    [Header("★ 資料庫綁定 (SSOT)")]
    private EntityRuntime _entityData;
    
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
    }

    private void Start()
    {
        // ==========================================
        // ★ 核心修正 1：區域依賴注入 (向大腦借資料)
        // ==========================================
        var core = GetComponent<EntityCore>();
        if (core != null)
        {
            _entityData = core.RuntimeData;
        }
        
        if (_entityData == null) 
        {
            Debug.LogError($"[致命錯誤] {gameObject.name} 缺少 EntityCore 大腦！");
            return;
        }

        // ==========================================
        // ★ 核心修正 2：索取並快取特徵
        // ==========================================
        if (!_entityData.TryGetTrait(out _staminaTrait))
        {
            Debug.LogWarning($"[系統警告] {gameObject.name} 的 SO 沒有掛載 StaminaTrait！將無法使用體力與衝刺系統！");
        }
    }

    private bool CanMove()
    {
        if (_entityData == null) return false;
        return !_entityData.HasState(EntityStateFlags.Dashing) && 
               !_entityData.HasState(EntityStateFlags.Stunned) && 
               !_entityData.HasState(EntityStateFlags.Dead);
    }

    // ★ 核心變更：直接向快取好的 _staminaTrait 詢問數值
    private bool CanDash() => CanMove() && 
                              !_isDashCooldown && 
                              _staminaTrait != null && // 確保有拿到特徵
                              _staminaTrait.currentStamina >= _staminaTrait.dashCost && 
                              _currentInput != Vector3.zero;

    private void Update()
    {
        if (_entityData == null || _entityData.HasState(EntityStateFlags.Dead)) return;

        HandleStaminaRegen();
        HandleInput();
        UpdateVisualColor();
    }

    private void HandleStaminaRegen()
    {
        if (_staminaTrait == null) return; 

        // 檢查狀態時也是向 _entityData 問
        if (!_entityData.HasState(EntityStateFlags.Dashing) && 
            _staminaTrait.currentStamina < _staminaTrait.maxStamina && 
            Time.time >= _lastStaminaConsumeTime + staminaRegenDelay)
        {
            _staminaTrait.RegenStamina(20f, Time.deltaTime); 
            
            // 注意：這裡其實未來也可以改用特徵內部的 OnStaminaRatioChanged 事件，但先保留你的廣播邏輯
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
        // ==========================================
        // ★ 核心修正 4：向大腦貼上標籤 (鎖定移動 + 賦予無敵)
        // 這樣 EntityHealthComponent 看到 Invincible 標籤就會自動免傷了！
        // ==========================================
        _entityData.AddState(EntityStateFlags.Dashing | EntityStateFlags.Invincible);
        
        _isDashCooldown = true;
        _lastStaminaConsumeTime = Time.time;

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

        // ==========================================
        // ★ 核心修正 5：衝刺結束，向大腦撕掉標籤
        // ==========================================
        _entityData.RemoveState(EntityStateFlags.Dashing | EntityStateFlags.Invincible);
        
        _rb.linearVelocity = Vector3.zero;

        float cooldown = _staminaTrait != null ? _staminaTrait.dashCooldown : 0.5f;
        yield return new WaitForSeconds(cooldown);
        _isDashCooldown = false;
    }

    private void UpdateVisualColor()
    {
        if (_sr == null || _entityData == null || _entityData.HasState(EntityStateFlags.Invincible)) return;

        if ((float)_entityData.CurrentHealth / _entityData.MaxHealth <= 0.2f)
        {
            float t = Mathf.PingPong(Time.time * 8f, 1f);
            //_sr.color = Color.Lerp(Color.white, damageColor, t);
        }
        else if (_staminaTrait != null)
        {
            _sr.color = Color.Lerp(Color.red, Color.white, _staminaTrait.StaminaRatio);
        }
    }
}