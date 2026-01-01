using UnityEngine;

namespace GameFramework.Actors
{
    public class PlayerJumpingState : PlayerAirborneState
    {
        public PlayerJumpingState(PlayerStateContext stateContext) : base(stateContext)
        {

        }

        public override void OnEnter()
        {
            base.OnEnter();    
            StateContext.ReusableData.IsJumping = true;
            SetAnimationBool(StateContext.AnimationData.JumpParameterHash, true);
            
            StateContext.ReusableData.TargetMovementSpeedModifier = StateContext.Data.AirborneData.FallData.SpeedModifier;
        }

        public override void OnLogic()
        {
            base.OnLogic();

            // 如果玩家已放開跳躍鍵，就立刻移除“續力”
            if (!StateContext.ReusableData.JumpInput)
            {
                ResetVerticalVelocity();
            }
        }
        
        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
        }

        public override void OnExit()
        {
            base.OnExit();
            SetAnimationBool(StateContext.AnimationData.JumpParameterHash, false);
            StateContext.ReusableData.IsJumping = false;
        }
    }
}