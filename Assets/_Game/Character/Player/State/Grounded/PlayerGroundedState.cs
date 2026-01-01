using UnityEngine;

namespace GameFramework.Actors
{
    public class PlayerGroundedState : PlayerMovementState
    {
        public PlayerGroundedState(PlayerStateContext stateContext) : base(stateContext)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            SetAnimationBool(StateContext.AnimationData.GroundedParameterHash, true);
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
            SetAnimationBool(StateContext.AnimationData.GroundedParameterHash, false);
        }
    }
}
