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

            SetStartState("Idle");

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

