namespace GameFramework.Actors
{
    public class PlayerRunState : PlayerMovingState
    {
        public PlayerRunState(PlayerStateContext stateContext) : base(stateContext)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            StateContext.ReusableData.TargetMovementSpeedModifier = StateContext.Data.GroundedData.RunData.SpeedModifier;
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
