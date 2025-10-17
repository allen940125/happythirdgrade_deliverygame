// using UnityEngine;
//
// public class ChaseState : GuardAI.GuardStateBase
// {
//     public ChaseState(GuardAI guard, GuardAI.AnimatorWrapper anim) : base(guard, anim) { }
//
//     public override void OnEnter()
//     {
//         _anim.SetSpeed(_guard._chaseSpeed);
//     }
//
//     public override void OnUpdate()
//     {
//         Controller.Move((_guard._player.position - guard.transform.position).normalized * 
//                         (_guard._chaseSpeed * Time.deltaTime));
//     }
// }