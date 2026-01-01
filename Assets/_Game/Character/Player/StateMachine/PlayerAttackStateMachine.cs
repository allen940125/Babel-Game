using UnityHFSM;

namespace GameFramework.Actors
{
    public class PlayerAttackStateMachine : StateMachine
    {
        private readonly PlayerStateContext _context;

        public PlayerAttackStateMachine(PlayerStateContext context)
        {
            _context = context;

            AddState("AttackIdleState", new PlayerAttackIdleState(context));
            AddState("AttackWindup", new PlayerAttackWindupState(context));
            AddState("AttackSwing", new PlayerAttackSwingState(context));
            AddState("AttackRecovery", new PlayerAttackRecoveryState(context));

            SetStartState("AttackIdleState");

            // --- Transition 邏輯修正 ---

            // 1. Idle -> Windup (開始攻擊)
            AddTransition(new Transition<string>("AttackIdleState", "AttackWindup", 
                t => context.ReusableData.WantsToAttack));

            // 2. Windup -> Swing (前搖結束 -> 揮刀)
            AddTransition(new Transition<string>("AttackWindup", "AttackSwing", 
                t => context.ReusableData.AttackWindupFinished));

            // 3. Swing -> Recovery (揮刀結束 -> 後搖/等待連擊窗口)
            AddTransition(new Transition<string>("AttackSwing", "AttackRecovery", 
                t => context.ReusableData.AttackSwingFinished));

            // 4. [關鍵修正] Recovery -> Windup (連擊！如果在後搖期間有輸入，且沒超過最大段數)
            // 注意：這裡假設你有一個 MaxComboCount 變數
            AddTransition(new Transition<string>("AttackRecovery", "AttackWindup", 
                t => context.ReusableData.WantsToAttack && context.ReusableData.ComboIndex < 3)); 

            // 5. Recovery -> Idle (連擊窗口結束，玩家沒按攻擊 -> 回待機)
            // 只有在 "沒有要攻擊" 或者 "動畫播完" 時才回去
            AddTransition(new Transition<string>("AttackRecovery", "AttackIdleState", t => context.ReusableData.AttackComboWindowFinished));
        }

        // ★ 關鍵修復 1：進入戰鬥模式時，強制清洗所有數據 ★
        public override void OnEnter()
        {
            base.OnEnter(); // 一定要呼叫 base，不然子狀態不會啟動

            ResetCombatFlags();
            
            // 重置連段索引
            _context.ReusableData.ComboIndex = 0;
            
            // 確保一進來是可以轉向的 (防止上次異常退出鎖死)
            _context.ReusableData.RotationSpeedMultiplier = 1f;
            _context.ReusableData.ActionMovementMultiplier = 1f;
        }

        // ★ 關鍵修復 2：離開戰鬥模式時，確保數據歸零 ★
        public override void OnExit()
        {
            base.OnExit();
            ResetCombatFlags();
            _context.Animator.CrossFade("Null", 0.1f);
            // 確保一進來是可以轉向的 (防止上次異常退出鎖死)
            _context.ReusableData.RotationSpeedMultiplier = 1f;
            _context.ReusableData.ActionMovementMultiplier = 1f;
        }

        private void ResetCombatFlags()
        {
            _context.ReusableData.AttackWindupFinished = false;
            _context.ReusableData.AttackSwingFinished = false;
            _context.ReusableData.AttackComboWindowFinished = false;
            // 這裡不需要 WantsToAttack = false，因為那是輸入訊號，留給 Input 系統決定
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
        private bool IsJumping() => _context.ReusableData.JumpInput;
    }
}

