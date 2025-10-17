// using UnityEngine;
//
// // 动画控制包装类
// public class AnimatorWrapper
// {
//     private readonly Animator _anim;
//         
//     // 使用Hash提升性能
//     private static readonly int SpeedHash = Animator.StringToHash("Speed");
//     private static readonly int MotionSpeedHash = Animator.StringToHash("MotionSpeed");
//     private static readonly int AttackHash = Animator.StringToHash("Attack");
//     private static readonly int SearchHash = Animator.StringToHash("Search");
//
//     public AnimatorWrapper(Animator animator) => _anim = animator;
//
//     public void SetSpeed(float speed)
//     {
//         _anim.SetFloat(SpeedHash, speed);
//         _anim.SetFloat(MotionSpeedHash, speed);
//     }
//
//     public void TriggerAttack() => _anim.SetTrigger(AttackHash);
//     public void SetSearching(bool value) => _anim.SetBool(SearchHash, value);
// }
