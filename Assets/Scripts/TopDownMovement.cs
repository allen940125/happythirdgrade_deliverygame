using UnityEngine;

public class SimpleCarController : MonoBehaviour
{
    public enum DriveType { FWD, RWD, AWD }   // 🔁 驅動類型
    [Header("驅動方式")]
    public DriveType driveType = DriveType.FWD;

    [Header("輪胎 Collider")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    [Header("輪胎模型 (可選)")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("車輛設定")]
    public float maxMotorTorque = 1500f;
    public float maxSteerAngle = 30f;
    public float brakeForce = 2000f;

    private float motorInput;
    private float steerInput;
    private float brakeInput;

    void Update()
    {
        motorInput = Input.GetAxis("Vertical");   // W / S
        steerInput = Input.GetAxis("Horizontal"); // A / D
        brakeInput = Input.GetKey(KeyCode.Space) ? 1f : 0f;

        // 🔁 驅動模式切換（可選）
        if (Input.GetKeyDown(KeyCode.Alpha1)) driveType = DriveType.FWD;
        if (Input.GetKeyDown(KeyCode.Alpha2)) driveType = DriveType.RWD;
        if (Input.GetKeyDown(KeyCode.Alpha3)) driveType = DriveType.AWD;
    }

    void FixedUpdate()
    {
        // 前輪轉向
        float steerAngle = maxSteerAngle * steerInput;
        frontLeftWheel.steerAngle = steerAngle;
        frontRightWheel.steerAngle = steerAngle;

        // 馬達扭力分配
        float torque = maxMotorTorque * motorInput;

        switch (driveType)
        {
            case DriveType.FWD:
                frontLeftWheel.motorTorque = torque;
                frontRightWheel.motorTorque = torque;
                rearLeftWheel.motorTorque = 0f;
                rearRightWheel.motorTorque = 0f;
                break;

            case DriveType.RWD:
                rearLeftWheel.motorTorque = torque;
                rearRightWheel.motorTorque = torque;
                frontLeftWheel.motorTorque = 0f;
                frontRightWheel.motorTorque = 0f;
                break;

            case DriveType.AWD:
                // 扭力均分四輪（或可再微調）
                float splitTorque = torque * 0.5f;
                frontLeftWheel.motorTorque = splitTorque;
                frontRightWheel.motorTorque = splitTorque;
                rearLeftWheel.motorTorque = splitTorque;
                rearRightWheel.motorTorque = splitTorque;
                break;
        }

        // 煞車
        float brakeTorque = brakeForce * brakeInput;
        ApplyBrake(brakeTorque);

        // 更新輪胎模型
        UpdateWheelPose(frontLeftWheel, frontLeftMesh);
        UpdateWheelPose(frontRightWheel, frontRightMesh);
        UpdateWheelPose(rearLeftWheel, rearLeftMesh);
        UpdateWheelPose(rearRightWheel, rearRightMesh);
    }

    void ApplyBrake(float brakeTorque)
    {
        frontLeftWheel.brakeTorque = brakeTorque;
        frontRightWheel.brakeTorque = brakeTorque;
        rearLeftWheel.brakeTorque = brakeTorque;
        rearRightWheel.brakeTorque = brakeTorque;
    }

    void UpdateWheelPose(WheelCollider collider, Transform mesh)
    {
        if (mesh == null) return;
        collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }
}
