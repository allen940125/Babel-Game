namespace GameFramework.Actors
{
    public class PlayerCombatIdleState : PlayerCombatState
    {
        public PlayerCombatIdleState(PlayerStateContext stateContext) : base(stateContext)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            
            SetAnimationBool(StateContext.AnimationData.CombatParameterName, false);
            StateContext.Animator.SetLayerWeight(1, 0f);
            
            // 設定為空閒狀態
            StateContext.ReusableData.IsAttackingAction = false;

            // 清除已用過的輸入旗標
            StateContext.ReusableData.WantsToAttack = false;
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
            SetAnimationBool(StateContext.AnimationData.CombatParameterName, true);
            StateContext.Animator.SetLayerWeight(1, 1f);
            // 可選：離開戰鬥 idle 的處理，例如記錄 idle 持續時間等
        }
    }
}