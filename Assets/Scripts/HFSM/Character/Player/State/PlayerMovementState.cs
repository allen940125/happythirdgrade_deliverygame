using Gamemanager;
using UnityEngine;
using UnityEngine.PlayerLoop;
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
                // 緩慢補間 SpeedModifier
                StateContext.ReusableData.MovementSpeedModifier = Mathf.MoveTowards(
                    StateContext.ReusableData.MovementSpeedModifier,
                    StateContext.ReusableData.TargetMovementSpeedModifier,
                    StateContext.Data.GroundedData.MovementSpeedModifierTransitionRate * Time.deltaTime
                );
            }
            else
            {
                // 緩慢補間 SpeedModifier
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
            base.PhysicsUpdate();
            Move();
        }

        public override void OnExit()
        {
            base.OnExit();
        }
        
        protected void Move()
        {
            if (StateContext.ReusableData.MovementInput == Vector2.zero || StateContext.ReusableData.MovementSpeedModifier == 0f || !StateContext.ReusableData.CanMove)
            {
                return;
            }
            
            Vector3 movementDirection = GetMovementInputDirection();
            
            float targetRotationYAngle = Rotate(movementDirection);
            
            Vector3 targetRotationDirection = GetTargetRotationDirection(targetRotationYAngle);
            
            float movementSpeed = GetMovementSpeed();
            
            Vector3 currentPlayerHorizontalVelocity = GetPlayerHorizontalVelocity();
            
            //Debug.Log("目標方向角" + targetRotationDirection + "增加速度" + movementSpeed + "減少的當前速度" + currentPlayerHorizontalVelocity);
            
            StateContext.Rigidbody.AddForce(targetRotationDirection * movementSpeed - currentPlayerHorizontalVelocity, ForceMode.VelocityChange);
        }
        
        /// <summary>
        /// 獲取移動速度
        /// </summary>
        /// <param name="shouldConsiderSlopes">應該考慮斜波</param>
        /// <returns></returns>
        protected float GetMovementSpeed(bool shouldConsiderSlopes = true)
        {
            float movementSpeed = StateContext.Data.GroundedData.BaseSpeed * StateContext.ReusableData.MovementSpeedModifier;
            
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

        /// <summary>
        /// 獲取玩家輸入的方位(如(1,0,1) 只有x跟z軸)
        /// </summary>
        /// <returns></returns>
        protected Vector3 GetMovementInputDirection()
        {
            return new Vector3(StateContext.ReusableData.MovementInput.x, 0f, StateContext.ReusableData.MovementInput.y);
        }
        
        #region 旋轉邏輯

        private float Rotate(Vector3 direction)
        {
            float directionAngle = UpdateTargetRotation(direction);

            RotateTowardsTargetRotation();

            return directionAngle;
        }
        
        /// <summary>
        /// 旋轉目標更新
        /// </summary>
        /// <param name="direction">方向</param>
        /// <param name="shouldConsiderCameraRotation">是否考慮相機旋轉</param>
        /// <returns>更新後的目標旋轉角度</returns>
        protected float UpdateTargetRotation(Vector3 direction, bool shouldConsiderCameraRotation = true)
        {
            float directionAngle = GetDirectionAngle(direction);

            if (shouldConsiderCameraRotation)
            {
                directionAngle = AddCameraRotationToAngle(directionAngle);
            }

            if (directionAngle != StateContext.ReusableData.CurrentTargetRotation.y)
            {
                UpdateTargetRotationData(directionAngle);
            }

            return directionAngle;
        }

        /// <summary>
        /// 獲取方向角 (用於玩家輸入按鍵的x,z軸來獲取方位角)
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        private float GetDirectionAngle(Vector3 direction)
        {
            float directionAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

            if (directionAngle < 0f)
            {
                directionAngle += 360f;
            }

            return directionAngle;
        }

        /// <summary>
        /// 玩家旋轉角度Y加上相機旋轉角度Y
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        private float AddCameraRotationToAngle(float angle)
        {
            angle += StateContext.MainCameraTransform.eulerAngles.y;

            if (angle > 360f)
            {
                angle -= 360f;
            }

            return angle;
        }

        /// <summary>
        /// 旋轉數據目標更新
        /// </summary>
        /// <param name="targetAngle"></param>
        private void UpdateTargetRotationData(float targetAngle)
        {
            StateContext.ReusableData.CurrentTargetRotation.y = targetAngle;

            StateContext.ReusableData.DampedTargetRotationPassedTime.y = 0f;
        }

        /// <summary>
        /// 玩家的旋轉角度朝目標旋轉角度過渡
        /// </summary>
        protected void RotateTowardsTargetRotation()
        {
            float currentYAngle = StateContext.Rigidbody.rotation.eulerAngles.y;

            if (currentYAngle == StateContext.ReusableData.CurrentTargetRotation.y)
            {
                return;
            }

            float smoothedYAngle = Mathf.SmoothDampAngle(currentYAngle, StateContext.ReusableData.CurrentTargetRotation.y, 
                ref StateContext.ReusableData.DampedTargetRotationCurrentVelocity.y, 
                StateContext.ReusableData.TimeToReachTargetRotation.y);
            
            //Debug.Log("當前旋轉角度" + smoothedYAngle);
            
            //StateContext.ReusableData.DampedTargetRotationPassedTime.y += Time.deltaTime;

            Quaternion targetRotation = Quaternion.Euler(0f, smoothedYAngle, 0f);

            StateContext.Rigidbody.MoveRotation(targetRotation);
        }

        /// <summary>
        /// 獲取目標方向向量 根據給定的角度計算對應的方向向量
        /// </summary>
        /// <param name="targetRotationAngle"></param>
        /// <returns></returns>
        protected Vector3 GetTargetRotationDirection(float targetRotationAngle)
        {
            //Debug.Log(targetRotationAngle);
            //Debug.Log(Quaternion.Euler(0f, targetRotationAngle, 0f) * Vector3.forward);
            return Quaternion.Euler(0f, targetRotationAngle, 0f) * Vector3.forward;
        }

        #endregion

        #region 數值設置
        
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
        /// 重製速度
        /// </summary>
        protected void ResetVelocity()
        {
            StateContext.Rigidbody.linearVelocity = Vector3.zero;
        }
        
        
        /// <summary>
        /// 重製垂直速度
        /// </summary>
        protected void ResetVerticalVelocity()
        {
            Vector3 playerHorizontalVelocity = GetPlayerHorizontalVelocity();

            StateContext.Rigidbody.linearVelocity = playerHorizontalVelocity;
        }
        
        #endregion
    }
}
