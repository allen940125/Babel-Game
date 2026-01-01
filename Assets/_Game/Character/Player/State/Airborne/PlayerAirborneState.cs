namespace GameFramework.Actors
{
    public class PlayerAirborneState : PlayerMovementState
    {
        public PlayerAirborneState(PlayerStateContext stateContext) : base(stateContext)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            SetAnimationBool(StateContext.AnimationData.GroundedParameterHash, false);
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
            SetAnimationBool(StateContext.AnimationData.GroundedParameterHash, true);
            //OnContactWithGround();
        }
        
        protected virtual void OnContactWithGround()
        {
            SetAnimationBool(StateContext.AnimationData.LandingParameterHash, true);
        }
    }
}
