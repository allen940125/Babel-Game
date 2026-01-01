namespace GameFramework.Actors
{
    public class PlayerMovingState : PlayerGroundedState
    {
        public PlayerMovingState(PlayerStateContext stateContext) : base(stateContext)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
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