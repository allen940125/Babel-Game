using UnityEngine;

namespace GameFramework.Actors
{
    public class PlayerFallingState : PlayerAirborneState
    {
        public PlayerFallingState(PlayerStateContext stateContext) : base(stateContext)
        {

        }

        public override void OnEnter()
        {
            base.OnEnter();
            SetAnimationBool(StateContext.AnimationData.FallParameterHash, true);
            
            StateContext.ReusableData.TargetMovementSpeedModifier = StateContext.Data.AirborneData.FallData.SpeedModifier;
            
            //ResetVerticalVelocity();
        }

        public override void OnLogic()
        {
            base.OnLogic();
        }
        
        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
            LimitVerticalVelocity();
        }

        public override void OnExit()
        {
            base.OnExit();
            SetAnimationBool(StateContext.AnimationData.FallParameterHash, false);
        }
        
        /// <summary>
        /// 限制垂直速度
        /// </summary>
        private void LimitVerticalVelocity()
        {
            Vector3 playerVerticalVelocity = GetPlayerVerticalVelocity();
        
            if (playerVerticalVelocity.y >= -StateContext.Data.AirborneData.FallData.FallSpeedLimit)
            {
                return;
            }
        
            Vector3 limitedVelocityForce = new Vector3(0f, -StateContext.Data.AirborneData.FallData.FallSpeedLimit - playerVerticalVelocity.y, 0f);
        
            StateContext.Rigidbody.AddForce(limitedVelocityForce, ForceMode.VelocityChange);
        }
        
        // protected override void OnContactWithGround()
        // {
        //     float fallDistance = StateContext.ReusableData.AirboreSpeed;
        //
        //     if (fallDistance < StateContext.Data.AirborneData.FallData.MinimumDistanceToBeConsideredHardFall)
        //     {
        //         SetAnimationBool(StateContext.AnimationData.LandingParameterHash, true);
        //
        //         return;
        //     }
        //
        //     if (StateContext.ReusableData.IsPreferWalkMode && !StateContext.ReusableData.IsSprinting || StateContext.ReusableData.MovementInput == Vector2.zero)
        //     {
        //         SetAnimationBool(StateContext.AnimationData.HardLandParameterHash, true);
        //         return;
        //     }
        //
        //     SetAnimationBool(StateContext.AnimationData.RollParameterHash, true);
        // }
    }
}