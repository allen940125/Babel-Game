using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameFramework.Actors
{
    [Serializable]
    public class PlayerStateReusableData
    {
        public string CurEnvironmentState;
        public string CurCombatState;

        [Header("玩家輸入")]
        public Vector2 MovementInput;
        public bool JumpInput;
        
        public float AirboreSpeed;
        
        [Header("移動速度控制 (Movement Logic)")]
        // 這是你的油門：負責處理 Walk(0.5) -> Run(1.0) 的平滑過渡
        // 真正給物理層用的變數是這個 (經過 Lerp 計算後的結果)
        public float MovementSpeedModifier = 0f; 
        
        // 這是移動狀態機設定的目標值
        public float TargetMovementSpeedModifier = 0f; 

        [Header("動作限制控制 (Action Logic)")]
        // 這是你的煞車：預設為 1.0 (無限制)
        // 攻擊時設為 0.0 (定身) 或 0.5 (減速)
        // 建議：不需要 Target 值，因為攻擊通常是「立即生效」的
        public float ActionMovementMultiplier = 1f; 

        [Header("最終計算 (Result)")]
        // 這是物理層唯一需要讀取的值
        // 公式：最終速度 = (平滑後的移動速度) * (動作限制倍率)
        public float FinalMoveSpeedModifier => MovementSpeedModifier * ActionMovementMultiplier;
        
        [Header("State Group")]
        public bool IsGrounded = false;
        public bool Stopping = false;
        public bool Landing = false;
        public bool Airborne = false;
        
        public bool CanMove = true;
        public bool CanJump = true;
        
        [Header("Grounded")]
        public float CurSpeed = 0f;

        public bool IsPreferWalkMode = false;
        public bool IsSprinting = false;
        public bool IsDashing = false;
        public bool IsMediumStopping = false;
        public bool IsHardStopping = false;
        public bool IsRolling = false;
        public bool IsHardLanding = false;

        [Header("Airborne")]
        public bool HasFinishedAirborne;
        
        public bool IsJumping = false;
        public bool IsFalling = false;
        
        [Header("Combat")]
        public bool IsInCombat = false;

        // 這是設定檔，可以序列化讓你在 Inspector 調整手感
        [SerializeField, Tooltip("按鍵預輸入的有效時間 (秒)")] 
        private float _inputBufferWindow = 0.5f;

        // 這是內部變數，用來記住時間
        // 因為這是執行期間的變數，其實不需要 SerializeField 存檔
        // 但為了 Debug 方便，我們用 Odin 讓它顯示，或是用 SerializeField 讓 Unity 顯示
        [ShowInInspector, ReadOnly] // Odin 屬性，唯讀顯示
        private float _lastAttackInputTime = -999f;

        // --- 核心邏輯 ---
    
        // 如果你有 Odin，加這個 Attribute 就可以在 Inspector 看到即時的 True/False 勾勾！
        [ShowInInspector] 
        public bool WantsToAttack 
        {
            get 
            {
                // 讀取邏輯：現在時間 - 上次時間 <= 窗口時間
                return Time.time - _lastAttackInputTime <= _inputBufferWindow;
            }
            set 
            {
                if (value == true)
                {
                    // 當外部寫入 WantsToAttack = true 時 (Input 系統)
                    // 我們記錄現在的時間戳記
                    _lastAttackInputTime = Time.time;
                }
                else
                {
                    // 當外部寫入 WantsToAttack = false 時 (狀態機消耗)
                    // 我們把時間戳記改成一個很久以前的時間，讓它過期
                    _lastAttackInputTime = -999f;
                }
            }
        }

        // 當收到攻擊訊號時呼叫這個
        public void SignalAttack()
        {
            _lastAttackInputTime = Time.time;
        }

        // 當攻擊真正發動(進入Windup)時呼叫這個，把輸入消耗掉
        public void UseAttackInput()
        {
            _lastAttackInputTime = -999f; // 設為過期時間
        }
        
        public bool IsUnderAttack = false;
        public bool CanBeInterrupted = false;
        public bool HasRecoveredFromHit = false;
        public bool IsAttackingAction = false;

        public bool AttackWindupFinished;
        public bool AttackSwingFinished;
        public bool AttackComboWindowFinished;

        public int ComboIndex;
        
        public WeaponConfig CurrentWeapon;
    
        // 取得當前段數的資料 (Helper Method)
        public AttackActionData GetCurrentAttackData()
        {
            if (CurrentWeapon == null || ComboIndex >= CurrentWeapon.ComboSteps.Count)
                return null;
            
            return CurrentWeapon.ComboSteps[ComboIndex];
        }
        
        [Header("Rotation Logic")]
        
        // 1. 當前模式
        public RotationMode CurrentRotationMode = RotationMode.OrientToMovement;

        // 2. 轉向速度倍率 (1.0 = 正常, 0.1 = 很慢, 0 = 鎖死)
        public float RotationSpeedMultiplier = 1.0f;

        // 3. 滑鼠在世界座標的位置 (這需要由 PlayerController 在 Update 裡去更新)
        public Vector3 MouseWorldPosition; 
        
        public PlayerRotationData RotationData { get; set; }
        
        [SerializeField] private Vector3 currentTargetRotation;
        [SerializeField] private Vector3 timeToReachTargetRotation;
        [SerializeField] private Vector3 dampedTargetRotationCurrentVelocity;
        [SerializeField] private Vector3 dampedTargetRotationPassedTime;
        
        /// <summary>
        /// 用於存儲目標旋轉的 Vector3 值（用來計算角色的目標旋轉方向）。
        /// </summary>
        public ref Vector3 CurrentTargetRotation
        {
            get
            {
                return ref currentTargetRotation;
            }
        }

        /// <summary>
        /// 計算到達目標旋轉所需的時間（以 Vector3 存儲，用來分別管理各軸向的時間）。
        /// </summary>
        public ref Vector3 TimeToReachTargetRotation
        {
            get
            {
                return ref timeToReachTargetRotation;
            }
        }

        /// <summary>
        /// 當前目標旋轉的速度，用於平滑過渡，確保旋轉變化不會過於突兀。
        /// </summary>
        public ref Vector3 DampedTargetRotationCurrentVelocity
        {
            get
            {
                return ref dampedTargetRotationCurrentVelocity;
            }
        }

        /// <summary>
        /// 記錄旋轉過程已經經過的時間，用於計算旋轉過渡進度。
        /// </summary>
        public ref Vector3 DampedTargetRotationPassedTime
        {
            get
            {
                return ref dampedTargetRotationPassedTime;
            }
        }
    }
    // 定義旋轉模式
    public enum RotationMode
    {
        OrientToMovement, // 看移動方向 (TPS/走路用)
        OrientToCursor    // 看滑鼠方向 (ARPG/攻擊用)
    }
}

