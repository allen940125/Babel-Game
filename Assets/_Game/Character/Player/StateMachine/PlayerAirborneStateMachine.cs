using UnityEngine;
using UnityHFSM;

namespace GameFramework.Actors
{
    public class PlayerAirborneStateMachine : StateMachine
    {
        private readonly PlayerStateContext _context;

        private float _maxFallSpeed = 0f;
        
        public PlayerAirborneStateMachine(PlayerStateContext context)
        {
            _context = context;

            AddState("Jump", new PlayerJumpingState(_context));
            AddState("Fall", new PlayerFallingState(_context));
            AddState("Land", new PlayerLightLandingState(_context));
            AddState("HandLand", new PlayerHardLandingState(_context));
            AddState("RollLand", new PlayerRollingState(_context));
            
            SetStartState("Jump");

            // ===== 跳到落下 =====
            AddTransition(new Transition<string>("Jump", "Fall", t => IsFalling()));

            // ===== 落下到著地 =====
            AddTransition(new Transition<string>("Fall", "RollLand", t => IsGrounded() && IsFastFall() && HasMoveInput()));
            
            AddTransition(new Transition<string>("Fall", "HandLand", t => IsGrounded() && IsFastFall() && !HasMoveInput()));
            
            AddTransition(new Transition<string>("Fall", "Land", t => IsGrounded() && !IsFastFall()));
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _context.ReusableData.HasFinishedAirborne = false;
            _maxFallSpeed = 0f;
        }

        public override void OnLogic()
        {
            base.OnLogic();
            //Debug.Log("是否是快速掉落" + IsFastFall()); 
            
            float fallSpeed = _context.Rigidbody.linearVelocity.y;
            if (fallSpeed < _maxFallSpeed)
            {
                _maxFallSpeed = fallSpeed;
            }
        }

        // ==============================
        //      空中邏輯條件區塊
        // ==============================

        private bool IsJumping() => _context.Rigidbody.linearVelocity.y > 0.1f;

        private bool IsFalling() => _context.Rigidbody.linearVelocity.y < -0.1f;
        
        private bool IsGrounded() => _context.ReusableData.IsGrounded;
        
        private bool IsFastFall() => _maxFallSpeed < -7.0f;
        
        private bool HasMoveInput() => _context.ReusableData.MovementInput.magnitude > 0.1f;

    }
}