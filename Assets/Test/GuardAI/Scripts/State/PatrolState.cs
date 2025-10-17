// using System.Collections;
// using UnityEngine;
//
// public class PatrolState : GuardAI.GuardStateBase
// {
//     public PatrolState(GuardAI guard, GuardAI.AnimatorWrapper anim) : base(guard, anim) { }
//
//     public override void OnEnter()
//     {
//         _anim.SetSpeed(_guard._patrolSpeed);
//         guard.StartCoroutine(PatrolRoutine());
//     }
//
//     private IEnumerator PatrolRoutine()
//     {
//         while (true)
//         {
//             var target = _guard._patrolPoints[_guard._currentPatrolIndex];
//             yield return MoveTo(target, _guard._patrolSpeed);
//             
//             yield return new WaitForSeconds(3);
//             UpdatePatrolIndex();
//         }
//     }
//
//     private void UpdatePatrolIndex()
//     {
//         // 更新巡逻索引逻辑...
//     }
// }