using GameFramework.Actors;
using Gamemanager;
using UnityEngine;
using UnityHFSM;

namespace GameFramework.Actors
{
    /// <summary>
    /// 所有玩家狀態的基底類別 (Base State)。
    /// <para>負責處理通用的邏輯，例如：物理旋轉、動畫參數設定、接收動畫事件。</para>
    /// </summary>
    public class GameState : State
    {
        protected PlayerStateContext StateContext;

        public GameState(PlayerStateContext stateContext)
        {
            StateContext = stateContext;
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnLogic()
        {
            base.OnLogic();
        }

        /// <summary>
        /// 物理更新循環 (FixedUpdate)。
        /// <para>預設會呼叫 <see cref="HandleRotation"/> 來處理角色轉向。</para>
        /// </summary>
        public virtual void PhysicsUpdate()
        {
            HandleRotation();
        }
        
        public override void OnExit()
        {
            base.OnExit();
        }

        /// <summary>
        /// 處理角色的旋轉邏輯。
        /// <para>根據 CurrentRotationMode 決定是看滑鼠還是看移動方向。</para>
        /// </summary>
        protected void HandleRotation()
        {
            // 讀取目前的模式
            RotationMode mode = StateContext.ReusableData.CurrentRotationMode;
            float targetAngle = StateContext.ReusableData.CurrentTargetRotation.y;
            bool shouldUpdateAngle = false;

            // --- 分流邏輯 ---
            if (mode == RotationMode.OrientToCursor)
            {
                // === 模式 A：看滑鼠 (ARPG 戰鬥用) ===
        
                // 取得滑鼠與角色的向量差
                Vector3 directionToMouse = StateContext.ReusableData.MouseWorldPosition - StateContext.Player.transform.position;
                directionToMouse.y = 0f; // 忽略高度差 (重要！不然會歪掉)

                // 只有當滑鼠離角色有一點距離時才轉，避免原地抽搐
                if (directionToMouse.sqrMagnitude > 0.01f)
                {
                    // 這裡不需要加相機角度，因為 MouseWorldPosition 已經是絕對座標
                    targetAngle = GetDirectionAngle(directionToMouse);
                    shouldUpdateAngle = true;
                }
            }
            else
            {
                // === 模式 B：看移動輸入 (走路/TPS 用) ===
        
                Vector3 inputDirection = GetMovementInputDirection();

                if (inputDirection != Vector3.zero)
                {
                    float angle = GetDirectionAngle(inputDirection);
                    targetAngle = AddCameraRotationToAngle(angle);
                    shouldUpdateAngle = true;
                }
            }

            // 更新目標角度 (如果需要變更)
            if (shouldUpdateAngle && targetAngle != StateContext.ReusableData.CurrentTargetRotation.y)
            {
                UpdateTargetRotationData(targetAngle);
            }

            // 執行平滑旋轉 (傳入倍率！)
            RotateTowardsTargetRotation(StateContext.ReusableData.RotationSpeedMultiplier);
        }
        
        #region Helper 方法 (旋轉與方向計算)
        
        /// <summary>
        /// 獲取玩家輸入的 3D 方向向量。
        /// <para>將 Input 的 Vector2(x, y) 轉換為 Vector3(x, 0, y)。</para>
        /// </summary>
        /// <returns>世界座標系下的輸入方向 (尚未考慮相機)。</returns>
        protected Vector3 GetMovementInputDirection()
        {
            return new Vector3(StateContext.ReusableData.MovementInput.x, 0f, StateContext.ReusableData.MovementInput.y);
        }

        /// <summary>
        /// 計算向量的方向角度 (Y軸旋轉)。
        /// </summary>
        /// <param name="direction">要計算的方向向量。</param>
        /// <returns>0 到 360 度的角度值。</returns>
        protected float GetDirectionAngle(Vector3 direction)
        {
            float directionAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            if (directionAngle < 0f) directionAngle += 360f;
            return directionAngle;
        }

        /// <summary>
        /// 將輸入角度加上主相機的當前旋轉角度。
        /// <para>確保「向前」是相對於相機的視角，而非絕對的世界座標 Z 軸。</para>
        /// </summary>
        /// <param name="angle">原始輸入角度。</param>
        /// <returns>修正後的目標角度。</returns>
        protected float AddCameraRotationToAngle(float angle)
        {
            angle += StateContext.MainCameraTransform.eulerAngles.y;
            if (angle > 360f) angle -= 360f;
            return angle;
        }

        /// <summary>
        /// 更新共用資料中的目標旋轉角度，並重置平滑時間計時器。
        /// </summary>
        /// <param name="targetAngle">新的目標 Y 軸角度。</param>
        private void UpdateTargetRotationData(float targetAngle)
        {
            StateContext.ReusableData.CurrentTargetRotation.y = targetAngle;
            StateContext.ReusableData.DampedTargetRotationPassedTime.y = 0f;
        }

        /// <summary>
        /// 執行平滑旋轉。
        /// </summary>
        /// <param name="speedMultiplier">速度倍率。1=正常, 0.5=半速, 0=鎖死。</param>
        protected void RotateTowardsTargetRotation(float speedMultiplier = 1.0f)
        {
            float currentYAngle = StateContext.Rigidbody.rotation.eulerAngles.y;
    
            if (currentYAngle == StateContext.ReusableData.CurrentTargetRotation.y) return;

            // --- 關鍵修改：動態調整平滑時間 ---
    
            float smoothTime = StateContext.ReusableData.TimeToReachTargetRotation.y;
    
            // 如果倍率很小 (例如 0.1)，代表轉很慢 -> smoothTime 變大
            // 如果倍率是 1.0，維持原樣
            if (speedMultiplier > 0.001f)
            {
                smoothTime /= speedMultiplier;
            }
            else
            {
                // 如果倍率是 0 (鎖死)，給一個極大的時間讓它轉不動
                smoothTime = float.MaxValue;
            }

            float smoothedYAngle = Mathf.SmoothDampAngle(currentYAngle, StateContext.ReusableData.CurrentTargetRotation.y, 
                ref StateContext.ReusableData.DampedTargetRotationCurrentVelocity.y, 
                smoothTime);

            StateContext.Rigidbody.MoveRotation(Quaternion.Euler(0f, smoothedYAngle, 0f));
        }

        /// <summary>
        /// 根據給定的 Y 軸角度，獲取對應的世界座標前進向量。
        /// <para>通常供子類別 (如 MovementState) 計算施力方向使用。</para>
        /// </summary>
        /// <param name="targetRotationAngle">目標 Y 軸角度。</param>
        /// <returns>指向該角度的單位向量 (Vector3.forward 旋轉後的結果)。</returns>
        protected Vector3 GetTargetRotationDirection(float targetRotationAngle)
        {
            return Quaternion.Euler(0f, targetRotationAngle, 0f) * Vector3.forward;
        }
        
        #endregion
        
        #region 動畫控制 Helper (封裝 Animator 操作)

        /// <summary>
        /// 設定 Animator 的 Bool 參數。
        /// </summary>
        protected void SetAnimationBool(int hash, bool value)
        {
            //Debug.Log($"[動畫 Bool] 設定 {hash} 為 {value}");
            StateContext.Animator.SetBool(hash, value);
        }

        /// <summary>
        /// 設定 Animator 的 Float 參數。
        /// </summary>
        protected void SetAnimationFloat(int hash, float value)
        {
            //Debug.Log($"[動畫 Float] 設定 {hash} 為 {value}");
            StateContext.Animator.SetFloat(hash, value);
        }

        /// <summary>
        /// 設定 Animator 的 Int 參數。
        /// </summary>
        protected void SetAnimationInt(int hash, int value)
        {
            //Debug.Log($"[動畫 Int] 設定 {hash} 為 {value}");
            StateContext.Animator.SetInteger(hash, value);
        }

        /// <summary>
        /// 觸發 Animator 的 Trigger 參數。
        /// </summary>
        protected void SetAnimationTrigger(int hash)
        {
            //Debug.Log($"[動畫 Trigger] 設定 {hash}");
            StateContext.Animator.SetTrigger(hash);
        }

        /// <summary>
        /// 重置 Animator 的 Trigger 參數 (防止重複觸發)。
        /// </summary>
        protected void ResetAnimationTrigger(int hash)
        {
            //Debug.Log($"[動畫 Trigger] 重設 {hash}");
            StateContext.Animator.ResetTrigger(hash);
        }
        
        #endregion

        #region 動畫事件回調 (Animation Events)

        /// <summary>
        /// 動畫事件：當特定動畫狀態進入時觸發。
        /// </summary>
        public virtual void OnAnimationEnterEvent()
        {
            
        }

        /// <summary>
        /// 動畫事件：當特定動畫狀態退出時觸發。
        /// </summary>
        public virtual void OnAnimationExitEvent()
        {
            
        }

        /// <summary>
        /// 動畫事件：當動畫轉換發生時觸發。
        /// </summary>
        public virtual void OnAnimationTransitionEvent()
        {
            
        }
        
        /// <summary>
        /// 動畫事件：攻擊前搖 (Windup) 結束，準備進入揮擊 (Swing)。
        /// <para>設定 AttackWindupFinished = true。</para>
        /// </summary>
        public virtual void OnAttackWindupFinishedEvent()
        {
            StateContext.ReusableData.AttackWindupFinished = true;
        }

        /// <summary>
        /// 動畫事件：揮擊 (Swing) 動作結束，準備進入後搖 (Recovery)。
        /// <para>設定 AttackSwingFinished = true。</para>
        /// </summary>
        public virtual void OnAttackSwingFinishedEvent() 
        {
            StateContext.ReusableData.AttackSwingFinished = true;
        }

        /// <summary>
        /// 動畫事件：連擊輸入窗口結束。
        /// <para>如果玩家在此之前沒有輸入攻擊，則無法連擊。設定 AttackComboWindowFinished = true。</para>
        /// </summary>
        public virtual void OnComboWindowOverEvent()
        {
            StateContext.ReusableData.AttackComboWindowFinished = true;
        }
        
        #endregion
    }
}