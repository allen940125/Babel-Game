namespace GameFramework.Actors
{
    public class PlayerJumpStartState : PlayerAirborneState
    {
        public PlayerJumpStartState(PlayerStateContext stateContext) : base(stateContext)
        {

        }

        public override void OnEnter()
        {
            base.OnEnter();
            Jump();
            SetAnimationBool(StateContext.AnimationData.JumpParameterHash, true);
            StateContext.ReusableData.IsJumping = true;
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