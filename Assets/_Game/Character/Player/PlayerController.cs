using System;
using Gamemanager;
using UnityEngine;
// using UnityEngine.InputSystem; // 這裡甚至不需要 InputSystem 了，因為被移出去了

namespace GameFramework.Actors
{
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerSO data;
        [SerializeField] private PlayerStateReusableData reusableData;
        [SerializeField] private PlayerAnimationData playerAnimationData;
        
        // 新增：引用瞄準組件
        [SerializeField] private PlayerAimingInput _aimingInput; 

        [Header("Debug / Testing")]
        [SerializeField] private GroundTrigger groundTrigger;

        // 內部組件
        private GameObject _player;
        private Animator _animator;
        private Rigidbody _rigidbody;
        private Transform _mainCameraTransform;
        
        // HFSM
        private PlayerRootHFSM _playerRootHFSM;
        private PlayerStateContext _playerStateContext;

        public PlayerStateContext PlayerStateContext => _playerStateContext;

        private void Awake()
        {
            InitializeComponents();
            InitializeStateMachine();
            SubscribeInputEvents();
        }
        
        private void Update()
        {
            // 1. 更新狀態機
            _playerRootHFSM.Tick();
            
            // 2. 更新環境資訊
            reusableData.IsGrounded = groundTrigger.IsGrounded;

            // 3. 處理瞄準輸入 (委派給專門的組件)
            // 這樣 Controller 就不知道「怎麼算」，只知道「要算」
            if (_aimingInput != null)
            {
                _aimingInput.HandleAimingInput(reusableData, transform);
            }
        }

        private void FixedUpdate()
        {
            _playerRootHFSM.GetCurrentState().PhysicsUpdate();
        }

        #region Initialization & Events

        private void InitializeComponents()
        {
            playerAnimationData.Initialize();
            
            _player = gameObject;
            _animator = GetComponentInChildren<Animator>();
            _rigidbody = GetComponent<Rigidbody>();

            if (Camera.main != null) _mainCameraTransform = Camera.main.transform;
            
            // 自動抓取 AimingInput (如果掛在同一個物件上)
            if (_aimingInput == null) _aimingInput = GetComponent<PlayerAimingInput>();
        }

        private void InitializeStateMachine()
        {
            _playerStateContext = new PlayerStateContext(_player, _animator, _rigidbody, data, reusableData, playerAnimationData, _mainCameraTransform);
            _playerRootHFSM = new PlayerRootHFSM(_playerStateContext);
        }

        private void SubscribeInputEvents()
        {
            var events = GameManager.Instance.MainGameEvent;
            
            events.SetSubscribe(events.OnMovementKeyPressedEvent, cmd => { reusableData.MovementInput = cmd.MoveInput; });
            events.SetSubscribe(events.OnJumpPressedEvent, cmd => { reusableData.JumpInput = cmd.JumpPressed; });
            events.SetSubscribe(events.OnWalkToggledEvent, cmd => { reusableData.IsPreferWalkMode = cmd.WalkToggled; });
            events.SetSubscribe(events.OnRunPressedEvent, cmd => { reusableData.IsSprinting = cmd.RunPressed; });
            
            events.SetSubscribe(events.OnAttackPressedEvent, cmd => 
            { 
                if (cmd.AttackPressed) reusableData.SignalAttack(); 
            });
        }

        #endregion

        #region Animation Events Bridge

        public void OnMovementStateAnimationEnterEvent() => _playerRootHFSM.OnAnimationEnterEvent();
        public void OnMovementStateAnimationExitEvent() => _playerRootHFSM.OnAnimationExitEvent();
        public void OnMovementStateAnimationTransitionEvent() => _playerRootHFSM.OnAnimationTransitionEvent();
        public void OnAttackWindupFinishedEvent() => _playerRootHFSM.OnAttackWindupFinishedEvent();
        public void OnAttackSwingFinishedEvent() => _playerRootHFSM.OnAttackSwingFinishedEvent();
        public void OnComboWindowOverEvent() => _playerRootHFSM.OnComboWindowOverEvent();

        #endregion
    }

    // Context 保持不變
    public class PlayerStateContext
    {
        public GameObject Player;
        public Animator Animator { get; }
        public Rigidbody Rigidbody { get; }
        public PlayerSO Data { get; }
        public PlayerStateReusableData ReusableData { get; }
        public PlayerAnimationData AnimationData { get; }
        public Transform MainCameraTransform { get; }

        public PlayerStateContext(GameObject player, Animator animator, Rigidbody rigidbody, PlayerSO data, PlayerStateReusableData reusableData, PlayerAnimationData animationData, Transform mainCameraTransform)
        {
            Player = player;
            Animator = animator;
            Rigidbody = rigidbody;
            Data = data;
            ReusableData = reusableData;
            AnimationData = animationData;
            MainCameraTransform  = mainCameraTransform;
        }
    }
}