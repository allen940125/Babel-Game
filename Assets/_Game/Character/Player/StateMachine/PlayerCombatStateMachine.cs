using UnityHFSM;

namespace GameFramework.Actors
{
    public class PlayerCombatStateMachine : StateMachine
    {
        public PlayerCombatStateMachine(PlayerStateContext context)
        {
            // 將上下文傳遞給子狀態機
            AddState("PlayerAttackStateMachine", new PlayerAttackStateMachine(context));
            AddState("PlayerHitStateMachine", new PlayerHitStateMachine(context));
            
            AddState("PlayerCombatIdleState", new PlayerCombatIdleState(context));
            
            SetStartState("PlayerCombatIdleState");

            // Attack → Hit：如果玩家正在攻擊但被敵人命中，進入受擊狀態
            AddTransition(new Transition<string>(
                "PlayerAttackStateMachine",
                "PlayerHitStateMachine",
                condition: t => context.ReusableData.IsUnderAttack
            ));

            // Hit → Attack：受擊結束後重新可以行動
            AddTransition(new Transition<string>(
                "PlayerHitStateMachine",
                "PlayerAttackStateMachine",
                condition: t => context.ReusableData.HasRecoveredFromHit && context.ReusableData.WantsToAttack
            ));

            // Idle → Attack（假設有 IdleCombatState）
            AddTransition(new Transition<string>(
                "PlayerCombatIdleState",
                "PlayerAttackStateMachine",
                condition: t => context.ReusableData.WantsToAttack
            ));

            // Idle → Hit（也能從待機進入受擊）
            AddTransition(new Transition<string>(
                "PlayerCombatIdleState",
                "PlayerHitStateMachine",
                condition: t => context.ReusableData.IsUnderAttack
            ));
        }
    }
}