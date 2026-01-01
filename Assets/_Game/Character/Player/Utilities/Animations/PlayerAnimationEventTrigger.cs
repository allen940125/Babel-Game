using GameFramework.Actors;
using UnityEngine;

namespace GenshinImpactMovementSystem
{
    public class PlayerAnimationEventTrigger : MonoBehaviour
    {
        private PlayerController player;
        Animator animator;

        private void Awake()
        {
            player = transform.parent.GetComponent<PlayerController>();
            animator = GetComponent<Animator>();
        }

        public void TriggerOnMovementStateAnimationEnterEvent()
        {
            if (IsInAnimationTransition())
            {
                return;
            }

            player.OnMovementStateAnimationEnterEvent();
        }

        public void TriggerOnMovementStateAnimationExitEvent()
        {
            if (IsInAnimationTransition())
            {
                return;
            }

            player.OnMovementStateAnimationExitEvent();
        }

        public void TriggerOnMovementStateAnimationTransitionEvent()
        {
            if (IsInAnimationTransition())
            {
                return;
            }

            player.OnMovementStateAnimationTransitionEvent();
        }

        private bool IsInAnimationTransition(int layerIndex = 0)
        {
            return player.PlayerStateContext.Animator.IsInTransition(layerIndex);
        }
        
        public void TriggerAttackWindupFinishedEvent() 
        {
            if (IsInAnimationTransition()) return;
            player.OnAttackWindupFinishedEvent();
        }

        public void TriggerAttackSwingFinishedEvent() 
        {
            if (IsInAnimationTransition()) return;
            player.OnAttackSwingFinishedEvent();
        }

        public void TriggerComboWindowOverEvent() 
        {
            if (IsInAnimationTransition()) return;
            player.OnComboWindowOverEvent();
        }

    }
}