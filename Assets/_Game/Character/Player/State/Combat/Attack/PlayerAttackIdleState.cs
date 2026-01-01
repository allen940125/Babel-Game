namespace GameFramework.Actors
{
    public class PlayerAttackIdleState : PlayerCombatState
    {
        public PlayerAttackIdleState(PlayerStateContext stateContext) : base(stateContext)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            // // 1. 重置所有攻擊相關旗標
            // StateContext.ReusableData.AttackWindupFinished = false;
            // StateContext.ReusableData.AttackSwingFinished = false;
            // StateContext.ReusableData.AttackComboWindowFinished = false;
            //
            // // 2. 重置連擊段數 (Combo Index)
            // StateContext.ReusableData.ComboIndex = 0;
            //
            // // 使用 CrossFade 讓動作銜接更滑順 (0.1f 是過渡時間)
            // StateContext.Animator.CrossFade("Null", 0.1f);
            //
            // // 3. 確保 Hitbox 是關閉的
            // //StateContext.WeaponController.DisableHitbox();
            //
            // StateContext.ReusableData.ActionMovementMultiplier = 1f;
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