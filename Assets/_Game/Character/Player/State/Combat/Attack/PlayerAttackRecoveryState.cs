namespace GameFramework.Actors
{
    public class PlayerAttackRecoveryState : PlayerCombatState
    {
        public PlayerAttackRecoveryState(PlayerStateContext stateContext) : base(stateContext)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            StateContext.ReusableData.ComboIndex += 1;
        }

        public override void OnLogic()
        {
            base.OnLogic();

            // 可選：每幀監測輸入（其實主要由 Transition 判斷，但這邊可預留回饋）
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