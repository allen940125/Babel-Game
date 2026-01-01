namespace GameFramework.Actors
{
    public class PlayerRollingState : PlayerLandingState
    {
        public PlayerRollingState(PlayerStateContext stateContext) : base(stateContext)
        {
            
        }
        
        public override void OnEnter()
        {
            base.OnEnter();  
            SetAnimationBool(StateContext.AnimationData.RollParameterHash, true);
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
            SetAnimationBool(StateContext.AnimationData.RollParameterHash, false);
        }
    }
}