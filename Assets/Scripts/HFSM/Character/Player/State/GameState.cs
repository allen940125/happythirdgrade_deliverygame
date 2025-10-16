using GameFramework.Actors;
using Gamemanager;
using UnityEngine;
using UnityHFSM;

namespace GameFramework.Actors
{
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

        public virtual void PhysicsUpdate()
        {
            
        }
        
        public override void OnExit()
        {
            base.OnExit();
        }

        protected void SetAnimationBool(int hash, bool value)
        {
            //Debug.LogError("Set Animation Bool: " + hash);
            //Debug.Log($"[動畫 Bool] 設定 {hash} 為 {value}");
            StateContext.Animator.SetBool(hash, value);
        }

        protected void SetAnimationFloat(int hash, float value)
        {
            //Debug.Log($"[動畫 Float] 設定 {hash} 為 {value}");
            StateContext.Animator.SetFloat(hash, value);
        }

        protected void SetAnimationInt(int hash, int value)
        {
            //Debug.Log($"[動畫 Int] 設定 {hash} 為 {value}");
            StateContext.Animator.SetInteger(hash, value);
        }

        protected void SetAnimationTrigger(int hash)
        {
            //Debug.Log($"[動畫 Trigger] 設定 {hash}");
            StateContext.Animator.SetTrigger(hash);
        }

        protected void ResetAnimationTrigger(int hash)
        {
            //Debug.Log($"[動畫 Trigger] 重設 {hash}");
            StateContext.Animator.ResetTrigger(hash);
        }
        
        public virtual void OnAnimationEnterEvent()
        {
            
        }

        public virtual void OnAnimationExitEvent()
        {
            
        }

        public virtual void OnAnimationTransitionEvent()
        {
            
        }
        
        public virtual void OnAttackWindupFinishedEvent()
        {
            StateContext.ReusableData.AttackWindupFinished = true;
        }

        public virtual void OnAttackSwingFinishedEvent() 
        {
            StateContext.ReusableData.AttackSwingFinished = true;
        }

        public virtual void OnComboWindowOverEvent()
        {
            StateContext.ReusableData.AttackComboWindowFinished = true;
        }
    }
}
