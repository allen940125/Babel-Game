using UnityHFSM;

namespace GameFramework.Actors
{
    public class PlayerGroundedStateMachine : StateMachine
    {
        private readonly PlayerStateContext _context;

        public PlayerGroundedStateMachine(PlayerStateContext context)
        {
            _context = context;

            AddState("Idle", new PlayerIdleState(context));
            AddState("Walk", new PlayerWalkState(context));
            AddState("Run", new PlayerRunState(context));
            AddState("Sprint", new PlayerSprintState(context));
            AddState("JumpStart", new PlayerJumpStartState(context));

            // 2. ★ 關鍵改變：把戰鬥機塞進來 ★
            // 這裡直接復用你原本寫好的 PlayerAttackStateMachine
            AddState("Combat", new PlayerAttackStateMachine(context));

            // 3. ★ 關鍵改變：把受擊機塞進來 ★
            AddState("Hit", new PlayerHitStateMachine(context));
            
            SetStartState("Idle");

            // ==========================================
            //      高權限轉換 (High Priority Interrupts)
            // ==========================================
            
            // ★ 受擊權限最高：無論在 走、跑、攻擊，只要被打，強制進 Hit
            // 這裡使用了 "Any Transition" 的概念，或者你可以針對每個狀態寫
            // 為了演示清楚，這裡寫針對 Idle/Move/Combat 的轉換
            
            // 從移動切換到受擊
            AddTransition(new Transition<string>("Idle", "Hit", t => context.ReusableData.IsUnderAttack));
            AddTransition(new Transition<string>("Walk", "Hit", t => context.ReusableData.IsUnderAttack));
            AddTransition(new Transition<string>("Run", "Hit", t => context.ReusableData.IsUnderAttack));
            AddTransition(new Transition<string>("Combat", "Hit", t => context.ReusableData.IsUnderAttack));

            // ==========================================
            //      戰鬥轉換 (Combat Transitions)
            // ==========================================
            
            // 從 Idle/Move 切換到 戰鬥
            AddTransition(new Transition<string>("Idle", "Combat", t => context.ReusableData.WantsToAttack));
            AddTransition(new Transition<string>("Walk", "Combat", t => context.ReusableData.WantsToAttack));
            AddTransition(new Transition<string>("Run", "Combat", t => context.ReusableData.WantsToAttack));

            // ==========================================
            //      回歸邏輯 (Recovery)
            // ==========================================

            // 戰鬥結束 -> 回 Idle (由 Combat 內部的邏輯決定何時結束，這裡只是接盤)
            // 注意：你原本的 Combat 狀態機裡面有 Exit Transition 嗎？
            // 如果沒有，你需要讓 Combat 狀態機在打完後觸發某個條件回到 Idle
            AddTransition(new Transition<string>("Combat", "Idle", 
                t => context.ReusableData.AttackComboWindowFinished)); // 假設這是結束訊號

            // 受擊結束 -> 回 Idle
            AddTransition(new Transition<string>("Hit", "Idle", 
                t => context.ReusableData.HasRecoveredFromHit));
            
            // ========== 移動中進入狀態 ==========
            AddTransition(new Transition<string>("Idle", "Sprint", t => IsSprinting()));
            AddTransition(new Transition<string>("Idle", "Run",    t => IsMoving() && IsRunning()));
            AddTransition(new Transition<string>("Idle", "Walk",   t => IsMoving() && IsWalking()));

            // ========== 停止時回到 Idle ==========
            AddTransition(new Transition<string>("Walk",   "Idle", t => !IsMoving()));
            AddTransition(new Transition<string>("Run",    "Idle", t => !IsMoving()));
            AddTransition(new Transition<string>("Sprint", "Idle", t => !IsMoving() && !IsSprinting()));
            AddTransition(new Transition<string>("JumpStart", "Idle",   t => !IsJumping()));
            
            // ========== 走路、跑步、衝刺間轉換 ==========
            AddTransition(new Transition<string>("Walk",   "Sprint", t => IsSprinting()));
            AddTransition(new Transition<string>("Walk",   "Run",    t => IsRunning()));

            AddTransition(new Transition<string>("Run",    "Sprint", t => IsSprinting()));
            AddTransition(new Transition<string>("Run",    "Walk",   t => IsWalking()));

            AddTransition(new Transition<string>("Sprint", "Run",    t => IsRunning()));
            AddTransition(new Transition<string>("Sprint", "Walk",   t => IsWalking()));
            
            // ========== 轉換到跳躍 ==========
            AddTransition(new Transition<string>("Idle", "JumpStart",   t => IsJumping()));
            AddTransition(new Transition<string>("Walk", "JumpStart",   t => IsJumping()));
            AddTransition(new Transition<string>("Run", "JumpStart",   t => IsJumping()));
            AddTransition(new Transition<string>("Sprint", "JumpStart",   t => IsJumping()));
            
        }

        public override void OnLogic()
        {
            base.OnLogic();
            //Debug.Log("當前狀態" + ActiveState);
        }

        // ===== Helper Methods =====
        private bool IsMoving() => _context.ReusableData.MovementInput.magnitude > 0.1f;
        private bool IsWalking() => _context.ReusableData.IsPreferWalkMode && !_context.ReusableData.IsSprinting;
        private bool IsRunning() => !_context.ReusableData.IsPreferWalkMode && !_context.ReusableData.IsSprinting;
        private bool IsSprinting() => _context.ReusableData.IsSprinting;
        private bool IsJumping() => _context.ReusableData.JumpInput && _context.ReusableData.CanJump;
    }
}

