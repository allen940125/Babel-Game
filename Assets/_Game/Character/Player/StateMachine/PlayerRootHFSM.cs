using UnityEngine;
using UnityHFSM;

namespace GameFramework.Actors
{
    public class PlayerRootHFSM
    {
        private PlayerEnvironmentStateMachine _environmentStateMachine;

        public string CurrentEnvironmentState => StateMachineExtensions.GetFullStatePath(_environmentStateMachine);
    
        private PlayerStateContext _context;
    
        public PlayerRootHFSM(PlayerStateContext context)
        {
            _context = context;
            _environmentStateMachine = new PlayerEnvironmentStateMachine(_context);
        
            _environmentStateMachine.Init();
        }

        public void Tick()
        {
            _environmentStateMachine.OnLogic();

            _context.ReusableData.CurEnvironmentState = _environmentStateMachine.GetFullStatePath();
            
            Debug.Log(GetCurrentState());
        }
    
        public void Trigger(string trigger)
        {
            _environmentStateMachine.Trigger(trigger);
        }
        
        public void OnAnimationEnterEvent()
        {
            GetCurrentState()?.OnAnimationEnterEvent();
        }

        public void OnAnimationExitEvent()
        {
            GetCurrentState()?.OnAnimationExitEvent();
        }

        public void OnAnimationTransitionEvent()
        {
            GetCurrentState()?.OnAnimationTransitionEvent();
        }
        
        public void OnAttackWindupFinishedEvent() 
        {
            GetCurrentState()?.OnAttackWindupFinishedEvent();
        }

        public void OnAttackSwingFinishedEvent() 
        {
            GetCurrentState()?.OnAttackSwingFinishedEvent();
        }

        public void OnComboWindowOverEvent() 
        {
            GetCurrentState()?.OnComboWindowOverEvent();
        }
        
        public GameState GetCurrentState()
        {
            // 获取当前最底层的状态实例
            StateBase<string> state = _environmentStateMachine.ActiveState;
            while (state is StateMachine nestedMachine)
            {
                state = nestedMachine.ActiveState;
            }
            return state as GameState; // 转换为基类
        }
    }
}
