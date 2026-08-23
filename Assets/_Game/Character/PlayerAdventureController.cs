using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization; // ★ 1. 確保引入新版 InputSystem

[RequireComponent(typeof(Rigidbody))]
public class PlayerAdventureController : MonoBehaviour
{
    [Header("即時狀態監控 (GAS-Lite 閘門)")]
    [SerializeField] private PlayerStateFlags currentState = PlayerStateFlags.Normal;

    [FormerlySerializedAs("playerSO")]
    [Header("資料庫綁定 (SSOT)")]
    [SerializeField] private EntityRuntime player;
    
    private StaminaTrait _stamina;
    private ExplorationTrait _exploration;
    private Rigidbody _rb;

    // ★ 2. 新增：暫存當前幀的輸入向量與按鍵狀態
    private Vector3 _currentInput;
    private bool _isJumpPressedThisFrame;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = true; 
        _rb.constraints = RigidbodyConstraints.FreezeRotation; 

        if (player != null)
        {
            player.TryGetTrait(out _stamina);
            player.TryGetTrait(out _exploration);
        }
    }

    // ==========================================
    // ★ GAS-Lite 核心：行為閘門 (Action Gates)
    // ==========================================
    
    private bool CanMove() => !currentState.HasFlag(PlayerStateFlags.Dead) && 
                              !currentState.HasFlag(PlayerStateFlags.Stunned) &&
                              !currentState.HasFlag(PlayerStateFlags.Dashing);

    private bool CanJump() => CanMove() && 
                              !currentState.HasFlag(PlayerStateFlags.Airborne);

    private void Update()
    {
        if (currentState.HasFlag(PlayerStateFlags.Dead)) return;

        // ★ 3. 嚴格執行順序：先讀取輸入 -> 檢查環境 -> 執行動作
        ReadInput();         
        CheckGroundedState(); 
        HandleMovement();
        HandleJump();
    }

    // ==========================================
    // 行為實作
    // ==========================================

    /// <summary>
    /// 統一處理新版 Input System 的輪詢
    /// </summary>
    private void ReadInput()
    {
        _currentInput = Vector3.zero;
        _isJumpPressedThisFrame = false;

        if (Keyboard.current != null)
        {
            // 處理方向鍵與 WASD
            float x = 0; float z = 0; // ★ 注意：3D 大世界是在 X-Z 平面上移動！
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) z = 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) z = -1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x = -1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x = 1;
            
            _currentInput = new Vector3(x, 0f, z).normalized;

            // 處理跳躍按鍵 (等同於舊版的 Input.GetKeyDown)
            _isJumpPressedThisFrame = Keyboard.current.spaceKey.wasPressedThisFrame;
        }
    }

    private void CheckGroundedState()
    {
        // 假設你用射線往腳下打 0.1f 距離來判斷是否貼地 (需確保中心點在腳底)
        bool isGrounded = Physics.Raycast(transform.position, Vector3.down, 0.1f);

        if (isGrounded)
        {
            currentState &= ~PlayerStateFlags.Airborne;
        }
        else
        {
            currentState |= PlayerStateFlags.Airborne;
        }
    }

    private void HandleMovement()
    {
        if (!CanMove()) return;

        float speed = _exploration != null ? _exploration.adventureWalkSpeed : 5f;
        
        // ★ 4. 直接套用剛才在 ReadInput 計算好的 _currentInput
        Vector3 targetVelocity = _currentInput * speed;
        
        targetVelocity.y = _rb.linearVelocity.y; // 保留重力墜落速度
        _rb.linearVelocity = targetVelocity;
    }

    private void HandleJump()
    {
        // ★ 5. 使用暫存的跳躍按鍵狀態進行閘門判定
        if (_isJumpPressedThisFrame && CanJump())
        {
            if (_exploration != null && _stamina != null && _stamina.ConsumeStamina(10f))
            {
                _rb.AddForce(Vector3.up * _exploration.jumpForce, ForceMode.Impulse);
                
                currentState |= PlayerStateFlags.Airborne;
                Debug.Log("消耗體力進行跳躍！");
            }
        }
    }
}