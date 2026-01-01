using UnityEngine;

namespace GameFramework.Actors
{
    public class PlayerAttackWindupState : PlayerCombatState
    {
        public PlayerAttackWindupState(PlayerStateContext stateContext) : base(stateContext)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();

            // 1. 消耗輸入 (重要！這樣才不會無限循環)
            StateContext.ReusableData.WantsToAttack = false;
        
            // 重置動畫事件旗標
            StateContext.ReusableData.AttackWindupFinished = false;
            StateContext.ReusableData.AttackSwingFinished = false;
            StateContext.ReusableData.AttackComboWindowFinished = false;

            // 1. 從 Config 拿資料 (取代原本的字串拼接)
            int currentIndex = StateContext.ReusableData.ComboIndex;
            var attackData = StateContext.ReusableData.CurrentWeapon.GetStep(currentIndex);
            
            if (attackData != null)
            {
                // 播放動畫
                StateContext.Animator.CrossFade(attackData.AnimationName, attackData.CrossFadeDuration);
        
                // 2. 施加微小的突進力 (這就是 RootMotionForce 的作用)
                // 確保這是在 Windup 剛開始，或者你也可以在 Swing 開始時施加
                // if (attackData.ForwardForce > 0)
                // {
                //     StateContext.ActorController.AddForce(StateContext.Transform.forward * attackData.ForwardForce);
                // }
        
                // 3. 設定當前的傷害倍率給 WeaponController (讓之後打到人時計算用)
                //StateContext.WeaponController.SetCurrentDamageMultiplier(attackData.DamageMultiplier);

                StateContext.ReusableData.CurrentRotationMode = RotationMode.OrientToCursor;
                StateContext.ReusableData.ActionMovementMultiplier = attackData.MovementMultiplier;
                StateContext.ReusableData.RotationSpeedMultiplier = attackData.RotationSpeedMultiplier;
            }
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
            SetAnimationBool(StateContext.AnimationData.WantsToAttackParameterName, false);
            StateContext.ReusableData.CurrentRotationMode = RotationMode.OrientToMovement;
            StateContext.ReusableData.RotationSpeedMultiplier = 1.0f;
        }
        
        public override void OnAnimationExitEvent()
        {
            // StateContext.ReusableData.AttackComboWindowFinished = false;
            // StateContext.ReusableData.AttackWindupFinished = false;
            // StateContext.ReusableData.AttackSwingFinished = false;
        }
    }
}