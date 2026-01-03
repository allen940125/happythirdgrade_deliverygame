// using UnityEngine;
// using Cinemachine;
//
// public class AutoAlignCamera : MonoBehaviour
// {
//     [Header("設定")]
//     public Transform playerTransform; // 玩家的 Transform
//     //public CinemachineFreeLook freeLookCamera; // 你的 FreeLook 攝影機
//     public float alignSpeed = 5f; // 回正的速度
//     public bool onlyAlignWhenMoving = true; // 是否只有在移動時才回正
//
//     private void Update()
//     {
//         //if (playerTransform == null || freeLookCamera == null) return;
//
//         // // 偵測玩家是否有移動 (假設用 WASD 或 Joystick)
//         // float inputX = Input.GetAxis("Horizontal");
//         // float inputY = Input.GetAxis("Vertical");
//         // bool isMoving = Mathf.Abs(inputX) > 0.01f || Mathf.Abs(inputY) > 0.01f;
//
//         // 如果設定為「只有移動時才回正」，且玩家沒移動，就不執行
//         //if (onlyAlignWhenMoving && !isMoving) return;
//
//         // 計算玩家目前的 Y 軸角度
//         float targetAngle = playerTransform.eulerAngles.y;
//
//         // 取得攝影機目前的角度 (Cinemachine FreeLook 的 X 軸控制水平旋轉)
//         float currentAngle = freeLookCamera.m_XAxis.Value;
//
//         // 使用 Mathf.LerpAngle 平滑過渡 (處理 0度 到 360度 的跳變)
//         float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * alignSpeed);
//
//         // 將計算後的角度賦值回去
//         freeLookCamera.m_XAxis.Value = newAngle;
//     }
// }