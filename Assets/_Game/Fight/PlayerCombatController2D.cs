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

    [Header("逃跑用速度")]
    public float MoveSpeedMultiplier = 1f;

    private bool _isDashCooldown = false;
    private float _lastStaminaConsumeTime;
    
    private Vector3 _currentInput;
    private Vector3 _currentVelocity;
    private Vector3 _dashDirection;
    
    // 在變數宣告區加入 Animator
    private Rigidbody _rb;
    private Animator _animator; // ★ 新增這行

    [Header("視覺設定")]
    [Tooltip("把你的 3D 模型或圖片物件 (Visual_Pivot) 拖進來")]
    public Transform visualPivot;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        
        // ★ 抓取子物件 (Visual_Pivot) 身上的 Animator
        _animator = GetComponentInChildren<Animator>(); 
        
        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
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
            // ★ 修改：從 _entityData 裡面讀取 MoveSpeedMultiplier
            float buffMultiplier = _entityData != null ? _entityData.MoveSpeedMultiplier : 1.0f;
            float baseMoveSpd = _staminaTrait != null ? _staminaTrait.moveSpeed : 5f;
            
            float finalMoveSpd = baseMoveSpd * buffMultiplier; // ★ 計算最終速度
            Vector3 targetVelocity = _currentInput * finalMoveSpd;
            
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
            _staminaTrait.ConsumeStamina(_staminaTrait.dashCost);

        _dashDirection = _currentInput;
        float duration = _staminaTrait != null ? _staminaTrait.dashDuration : 0.2f;

        // ==========================================
        // ★ 開始硬寫：視覺 360 度旋轉邏輯
        // ==========================================
        if (visualPivot != null)
        {
            // 決定旋轉方向：如果向左衝(x < 0)，轉 +360 度；向右衝轉 -360 度
            float spinDirection = (_currentInput.x < 0) ? 1f : -1f;
            
            // 記錄轉動前的原始角度，確保轉完不會歪掉
            Quaternion startRotation = visualPivot.localRotation;
            
            float elapsed = 0f;
            
            // 在 duration (衝刺時間) 內，每一幀去旋轉它
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // 計算當前時間的進度百分比 (0.0 ~ 1.0)
                float percent = elapsed / duration;
                
                // 算出當下應該轉幾度
                float currentAngle = Mathf.Lerp(0f, 360f * spinDirection, percent);
                
                // 套用到 Z 軸 (2.5D 通常是轉 Z 軸，如果是 3D 往前翻跟斗可能是轉 X 軸，可自行修改)
                visualPivot.localRotation = startRotation * Quaternion.Euler(0, 0, currentAngle);
                
                // 暫停一幀，下一幀繼續迴圈
                yield return null; 
            }
            
            // 衝刺結束，強制精準歸位，防止小數點誤差導致模型稍微傾斜
            visualPivot.localRotation = startRotation;
        }
        else
        {
            // 如果沒綁定模型，就乖乖等時間過去
            yield return new WaitForSeconds(duration);
        }
        // ==========================================

        _entityData.RemoveState(EntityStateFlags.Dashing | EntityStateFlags.Invincible);
        _rb.linearVelocity = Vector3.zero;

        float cooldown = _staminaTrait != null ? _staminaTrait.dashCooldown : 0.5f;
        yield return new WaitForSeconds(cooldown);
        _isDashCooldown = false;
    }
}