using System;
using UnityEngine;
using UnityHFSM;

namespace GameFramework.Actors
{
    public class PlayerHitStateMachine : StateMachine
    {
        public PlayerHitStateMachine(PlayerStateContext context)
        {
            // 這裡只需要定義受擊的各種姿勢，例如：
            // 輕受擊 (LightHit)
            // 重受擊 (HeavyHit)
            // 擊飛 (Knockback)
        
            //AddState("GeneralHit", new PlayerGeneralHitState(context));
        
            SetStartState("GeneralHit");
        
            // 這裡不需要寫 "回到 Idle" 的邏輯
            // 因為 "回到 Idle" 是父狀態機 (Grounded) 的工作
            // 這裡只要確保 HasRecoveredFromHit 變成 true 即可
        }

        public override void OnLogic()
        {
            base.OnLogic();
            //Debug.Log("當前狀態" + ActiveState);
        }

        // ===== Helper Methods =====
    }
}

