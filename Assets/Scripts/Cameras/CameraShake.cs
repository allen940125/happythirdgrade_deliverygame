using System;
using Unity.Cinemachine;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    CinemachinePanTilt panTilt;
    private bool isShaking = false;
    private float shakeTimer = 0f;
    private float duration = 1.0f; // 過渡時間
    private float shakeAmount = 0f; // 單次晃動增量
    private float lastShakeOffset = 0f; // 上一幀的晃動偏移量

    void Start()
    {
        panTilt = GetComponent<CinemachinePanTilt>();
    }
    
    void Update()
    {
        // if (isShaking && GameManager.Instance.PlayerSo.DrunkennessData.IsDrunkenCameraShaking())
        // {
        //     shakeTimer += Time.deltaTime;
        //     float t = shakeTimer / duration;
        //     float newShakeOffset = Mathf.Lerp(0f, GameManager.Instance.PlayerSo.DrunkennessData.shakeAmount, t);
        //
        //     // 讓相機值增加，而不是覆蓋
        //     panTilt.PanAxis.Value += (newShakeOffset - lastShakeOffset);
        //
        //     lastShakeOffset = newShakeOffset; // 更新上一次的偏移量
        //
        //     if (t >= 1.0f)
        //     {
        //         isShaking = false;
        //     }
        // }
    }
}