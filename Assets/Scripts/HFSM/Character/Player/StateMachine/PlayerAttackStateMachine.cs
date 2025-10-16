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
            //AddState("AttackComboWait", new PlayerAttackComboWaitState(context));
            //AddState("AttackEnd", new PlayerAttackEndState(context));

            SetStartState("AttackIdleState");

            // 接下來加上轉換邏輯，例如：
            
            AddTransition(new Transition<string>("AttackIdleState", "AttackWindup", t => context.ReusableData.WantsToAttack));
            AddTransition(new Transition<string>("AttackWindup", "AttackSwing", t => context.ReusableData.AttackWindupFinished));
            AddTransition(new Transition<string>("AttackSwing", "AttackRecovery", t => context.ReusableData.AttackSwingFinished));
            //AddTransition(new Transition<string>("AttackRecovery", "AttackComboWait", t => context.ReusableData.ComboQueued && context.ComboWindowActive));
            //AddTransition(new Transition<string>("AttackComboWait", "AttackWindup", t => context.ReusableData.NextComboConfirmed));
            //AddTransition(new Transition<string>("AttackComboWait", "AttackIdleState", t => context.ComboWaitTimeOver));
            AddTransition(new Transition<string>("AttackRecovery", "AttackIdleState", t => context.ReusableData.AttackComboWindowFinished));

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

