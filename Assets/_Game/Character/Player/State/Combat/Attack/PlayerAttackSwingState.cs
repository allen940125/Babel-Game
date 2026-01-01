using UnityEngine;
namespace GameFramework.Actors
{
    public class PlayerAttackSwingState : PlayerCombatState
    {
        private AttackActionData _currentAttackData;
        
        public PlayerAttackSwingState(PlayerStateContext stateContext) : base(stateContext)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            StateContext.ReusableData.AttackSwingFinished = false;

            _currentAttackData = StateContext.ReusableData.GetCurrentAttackData();
            
            // 1. 開啟傷害判定
            //StateContext.WeaponController.EnableHitbox();

            // 2. 鎖定移動與旋轉 (這時候不能再轉彎了！)
            StateContext.ReusableData.ActionMovementMultiplier = 0f;

            // 3. 開啟 Root Motion (讓動畫帶動角色往前踏步)
            //StateContext.Animator.applyRootMotion = true;
            
            // 4. 【關鍵】應用攻擊位移 (Impulse)
            ApplyAttackImpulse();
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
            //StateContext.WeaponController.DisableHitbox();
            //StateContext.Animator.applyRootMotion = false;
        }
        
        private void ApplyAttackImpulse()
        {
            // 防呆
            if (_currentAttackData == null || _currentAttackData.RootMotionForce == Vector3.zero) 
                return;

            // 將配置的力 (Local) 轉為世界座標方向 (World)
            // 這裡假設 RootMotionForce.z 是往前衝的力量
            Vector3 forceDirection = StateContext.Player.transform.TransformDirection(_currentAttackData.RootMotionForce);

            // 使用 Impulse 模式，因為這是一瞬間的爆發力
            StateContext.Rigidbody.AddForce(forceDirection, ForceMode.Impulse);
        }
    }
}