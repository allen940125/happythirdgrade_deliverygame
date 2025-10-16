using System;
using UnityEngine;

namespace GameFramework.Actors
{
    [Serializable]
    public class PlayerStateReusableData
    {
        public string CurEnvironmentState;
        public string CurCombatState;

        [Header("玩家輸入")]
        public Vector2 MovementInput;
        public bool JumpInput;
        
        public float MovementSpeedModifier;
        public float TargetMovementSpeedModifier; // 目標值
        
        public float AirboreSpeed;
        
        [Header("State Group")]
        public bool IsGrounded = false;
        public bool Stopping = false;
        public bool Landing = false;
        public bool Airborne = false;
        
        public bool CanMove = true;
        public bool CanJump = true;
        
        [Header("Grounded")]
        public float CurSpeed = 0f;

        public bool IsPreferWalkMode = false;
        public bool IsSprinting = false;
        public bool IsDashing = false;
        public bool IsMediumStopping = false;
        public bool IsHardStopping = false;
        public bool IsRolling = false;
        public bool IsHardLanding = false;

        [Header("Airborne")]
        public bool HasFinishedAirborne;
        
        public bool IsJumping = false;
        public bool IsFalling = false;
        
        [Header("Combat")]
        public bool IsInCombat = false;
        public bool WantsToAttack = false;
        public bool IsUnderAttack = false;
        public bool CanBeInterrupted = false;
        public bool HasRecoveredFromHit = false;
        public bool IsAttackingAction = false;

        public bool AttackWindupFinished;
        public bool AttackSwingFinished;
        public bool AttackComboWindowFinished;
        
        [Header("Rotate")]
        public PlayerRotationData RotationData { get; set; }
        [SerializeField] private Vector3 currentTargetRotation;
        [SerializeField] private Vector3 timeToReachTargetRotation;
        [SerializeField] private Vector3 dampedTargetRotationCurrentVelocity;
        [SerializeField] private Vector3 dampedTargetRotationPassedTime;
        
        /// <summary>
        /// 用於存儲目標旋轉的 Vector3 值（用來計算角色的目標旋轉方向）。
        /// </summary>
        public ref Vector3 CurrentTargetRotation
        {
            get
            {
                return ref currentTargetRotation;
            }
        }

        /// <summary>
        /// 計算到達目標旋轉所需的時間（以 Vector3 存儲，用來分別管理各軸向的時間）。
        /// </summary>
        public ref Vector3 TimeToReachTargetRotation
        {
            get
            {
                return ref timeToReachTargetRotation;
            }
        }

        /// <summary>
        /// 當前目標旋轉的速度，用於平滑過渡，確保旋轉變化不會過於突兀。
        /// </summary>
        public ref Vector3 DampedTargetRotationCurrentVelocity
        {
            get
            {
                return ref dampedTargetRotationCurrentVelocity;
            }
        }

        /// <summary>
        /// 記錄旋轉過程已經經過的時間，用於計算旋轉過渡進度。
        /// </summary>
        public ref Vector3 DampedTargetRotationPassedTime
        {
            get
            {
                return ref dampedTargetRotationPassedTime;
            }
        }
    }
}

