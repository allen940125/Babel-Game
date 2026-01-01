using UnityHFSM;

namespace GameFramework.Actors
{
    public class PlayerEnvironmentStateMachine : StateMachine
    {
        public PlayerEnvironmentStateMachine(PlayerStateContext context)
        {
            // 將上下文傳遞給子狀態機
            AddState("PlayerGroundedStateMachine", new PlayerGroundedStateMachine(context));
            AddState("PlayerAirborneStateMachine", new PlayerAirborneStateMachine(context));

            SetStartState("PlayerGroundedStateMachine");

            AddTransition(new Transition<string>(
                "PlayerGroundedStateMachine",
                "PlayerAirborneStateMachine",
                condition: t => context.ReusableData.IsGrounded == false
            ));

            AddTransition(new Transition<string>(
                "PlayerAirborneStateMachine",
                "PlayerGroundedStateMachine",
                condition: t => context.ReusableData.IsGrounded && context.ReusableData.HasFinishedAirborne
            ));
        }
    }
}
