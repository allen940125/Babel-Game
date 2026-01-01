namespace GameFramework.Actors
{
    public class PlayerHardLandingState : PlayerLandingState
    {
        public PlayerHardLandingState(PlayerStateContext stateContext) : base(stateContext)
        {
            
        }
        
        public override void OnEnter()
        {
            base.OnEnter();  
            SetAnimationBool(StateContext.AnimationData.HardLandParameterHash, true);
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
            SetAnimationBool(StateContext.AnimationData.HardLandParameterHash, false);
        }
    }
}