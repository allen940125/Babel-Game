namespace GameFramework.Actors
{
    public class PlayerIdleState : PlayerGroundedState
    {
        public PlayerIdleState(PlayerStateContext stateContext) : base(stateContext)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            StateContext.ReusableData.TargetMovementSpeedModifier = StateContext.Data.GroundedData.IdleData.SpeedModifier;
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
    }
}

