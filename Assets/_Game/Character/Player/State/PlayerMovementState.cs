using GameFramework.Actors;
using Gamemanager;
using UnityEngine;
using UnityHFSM;

namespace GameFramework.Actors
{
    public class PlayerMovementState : GameState
    {
        public PlayerMovementState(PlayerStateContext stateContext) : base(stateContext)
        {
            
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnLogic()
        {
            base.OnLogic();

            if (StateContext.ReusableData.MovementInput != Vector2.zero)
            {
                // 緩慢補間 SpeedModifier (加速)
                StateContext.ReusableData.MovementSpeedModifier = Mathf.MoveTowards(
                    StateContext.ReusableData.MovementSpeedModifier,
                    StateContext.ReusableData.TargetMovementSpeedModifier,
                    StateContext.Data.GroundedData.MovementSpeedModifierTransitionRate * Time.deltaTime
                );
            }
            else
            {
                // 緩慢補間 SpeedModifier (減速/停止)
                StateContext.ReusableData.MovementSpeedModifier = Mathf.MoveTowards(
                    StateContext.ReusableData.MovementSpeedModifier,
                    0f,
                    StateContext.Data.GroundedData.MovementSpeedModifierTransitionRate * Time.deltaTime
                );
            }

            StateContext.ReusableData.CurSpeed = GetMovementSpeed();
            SetAnimationFloat(StateContext.AnimationData.SpeedParameterHash, GetMovementSpeed());
            
            StateContext.ReusableData.AirboreSpeed = GetPlayerVerticalVelocity().y;
        }

        public override void PhysicsUpdate()
        {
            // 1. 呼叫父類別：GameState.HandleRotation() 會在這裡被執行，負責讓角色轉向
            base.PhysicsUpdate();
            
            // 2. 執行自己的邏輯：負責施加移動推力 (AddForce)
            Move();
        }

        public override void OnExit()
        {
            base.OnExit();
        }
        
        protected void Move()
        {
            if (StateContext.ReusableData.MovementInput == Vector2.zero || StateContext.ReusableData.FinalMoveSpeedModifier == 0f || !StateContext.ReusableData.CanMove)
            {
                return;
            }
            
            // --- 修改後的邏輯 ---
            
            // 1. 獲取輸入方向 (呼叫父類別方法)
            Vector3 movementInput = GetMovementInputDirection();
            
            // 2. 計算目標角度 (只計算數學，不執行旋轉)
            // 原本的 Rotate() 被拆解成下面這兩步：
            float targetAngle = GetDirectionAngle(movementInput);
            targetAngle = AddCameraRotationToAngle(targetAngle); // 加上相機角度修正
            
            // 3. 獲取施力方向 (呼叫父類別方法)
            // 根據剛剛算出來的角度，取得世界座標的前進向量
            Vector3 targetForceDirection = GetTargetRotationDirection(targetAngle);
            
            // 4. 計算速度
            float movementSpeed = GetMovementSpeed();
            Vector3 currentPlayerHorizontalVelocity = GetPlayerHorizontalVelocity();
            
            // 5. 施加力道 (推動角色)
            StateContext.Rigidbody.AddForce(targetForceDirection * movementSpeed - currentPlayerHorizontalVelocity, ForceMode.VelocityChange);
        }
        
        /// <summary>
        /// 獲取移動速度
        /// </summary>
        /// <param name="shouldConsiderSlopes">應該考慮斜坡</param>
        /// <returns></returns>
        protected float GetMovementSpeed(bool shouldConsiderSlopes = true)
        {
            float movementSpeed = StateContext.Data.GroundedData.BaseSpeed * StateContext.ReusableData.FinalMoveSpeedModifier;
            
            if (shouldConsiderSlopes)
            {
                //movementSpeed *= StateContext.ReusableData.MovementOnSlopesSpeedModifier;
            }

            return movementSpeed;
        }

        protected void Jump()
        {
            Debug.Log("觸發跳躍");
            StateContext.Rigidbody.AddForce(StateContext.Data.AirborneData.JumpData.JumpStartForce, ForceMode.VelocityChange);
        }

        #region 數值設置 (Movement 專用)
        
        /// <summary>
        /// 獲取水平速度
        /// </summary>
        /// <returns></returns>
        protected Vector3 GetPlayerHorizontalVelocity()
        {
            Vector3 playerHorizontalVelocity = StateContext.Rigidbody.linearVelocity;

            playerHorizontalVelocity.y = 0f;

            return playerHorizontalVelocity;
        }
        
        /// <summary>
        /// 獲取垂直速度
        /// </summary>
        /// <returns></returns>
        protected Vector3 GetPlayerVerticalVelocity()
        {
            return new Vector3(0f, StateContext.Rigidbody.linearVelocity.y, 0f);
        }
        
        /// <summary>
        /// 重置速度
        /// </summary>
        protected void ResetVelocity()
        {
            StateContext.Rigidbody.linearVelocity = Vector3.zero;
        }
        
        
        /// <summary>
        /// 重置垂直速度
        /// </summary>
        protected void ResetVerticalVelocity()
        {
            Vector3 playerHorizontalVelocity = GetPlayerHorizontalVelocity();

            StateContext.Rigidbody.linearVelocity = playerHorizontalVelocity;
        }
        
        #endregion
    }
}