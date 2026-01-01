namespace GameFramework.Actors
{
    public class PlayerAttackState : PlayerCombatState
    {
        public PlayerAttackState(PlayerStateContext stateContext) : base(stateContext)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            
            SetAnimationBool(StateContext.AnimationData.WantsToAttackParameterName, true);

            StateContext.ReusableData.TargetMovementSpeedModifier = 0.0f;
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
            
            SetAnimationBool(StateContext.AnimationData.WantsToAttackParameterName, false);
            
            StateContext.ReusableData.TargetMovementSpeedModifier = 1.0f;
        }
    }
}