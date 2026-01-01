using Gamemanager;
using UnityEngine;

namespace GameFramework.Actors
{
    public class PlayerLandingState : PlayerGroundedState
    {
        public PlayerLandingState(PlayerStateContext stateContext) : base(stateContext)
        {
            
        }
        
        public override void OnEnter()
        {
            //base.OnEnter();  
            StateContext.ReusableData.TargetMovementSpeedModifier = 0f;
            ResetVelocity();
            SetAnimationBool(StateContext.AnimationData.LandingParameterHash, true);
        }

        public override void OnLogic()
        {
            base.OnLogic();
        }
        
        public override void PhysicsUpdate()
        {
            base.PhysicsUpdate();
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void OnAnimationExitEvent()
        {
            Debug.Log("落地動畫撥放完畢");
            StateContext.ReusableData.HasFinishedAirborne = true;
            SetAnimationBool(StateContext.AnimationData.LandingParameterHash, false);
        }
    }
}