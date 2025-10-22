

using UnityEngine;

[System.Serializable]
public class WheelData
{
    public WheelCollider collider;
    public Transform mesh;
    public bool canSteer;   // 是否能轉向
    public bool canDrive;   // 是否提供驅動力
    public bool canBrake;   // 是否能煞車
}


public class FlexibleCarController : MonoBehaviour
{
    [Header("輪胎資料")]
    public WheelData[] wheels;

    [Header("車輛設定")]
    public float maxMotorTorque = 1500f;
    public float maxSteerAngle = 30f;
    public float brakeForce = 2000f;

    private float motorInput;
    private float steerInput;
    private float brakeInput;

    void Update()
    {
        motorInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");
        brakeInput = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }

    void FixedUpdate()
    {
        foreach (var wheel in wheels)
        {
            if (wheel.collider == null) continue;

            // 轉向
            if (wheel.canSteer)
                wheel.collider.steerAngle = maxSteerAngle * steerInput;

            // 驅動
            if (wheel.canDrive)
                wheel.collider.motorTorque = maxMotorTorque * motorInput;
            else
                wheel.collider.motorTorque = 0f;

            // 煞車
            if (wheel.canBrake)
                wheel.collider.brakeTorque = brakeForce * brakeInput;
            else
                wheel.collider.brakeTorque = 0f;

            // 更新模型
            if (wheel.mesh)
            {
                wheel.collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
                wheel.mesh.position = pos;
                wheel.mesh.rotation = rot;
            }
        }
    }
}