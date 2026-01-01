namespace GameFramework.Actors
{
    public class PlayerWalkState : PlayerMovingState
    {
        public PlayerWalkState(PlayerStateContext stateContext) : base(stateContext)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            StateContext.ReusableData.TargetMovementSpeedModifier = StateContext.Data.GroundedData.WalkData.SpeedModifier;
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