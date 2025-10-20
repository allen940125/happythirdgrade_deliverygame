using UnityEngine;

public class SimpleCarController : MonoBehaviour
{
    public enum DriveType { FWD, RWD, AWD } // 驅動類型
    public enum SteeringType { FrontOnly, FourWheel } // 轉向類型

    [Header("驅動方式")]
    public DriveType driveType = DriveType.FWD;

    [Header("轉向模式")]
    public SteeringType steeringType = SteeringType.FrontOnly;

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
    public float maxMotorTorque = 5000f;  // 調整後，對1000kg車輛合理
    public float maxSteerAngle = 30f;
    public float brakeForce = 8000f;      // 調整後煞車手感合理
    public float autoBrakeForce = 10f;   // 沒油門時的自動阻力

    [Header("四輪轉向設定")]
    [Tooltip("低速時後輪與前輪反向的最大角度比例")]
    public float lowSpeedSteerFactor = -0.5f;
    [Tooltip("高速時後輪與前輪同向的最大角度比例")]
    public float highSpeedSteerFactor = 0.3f;
    [Tooltip("速度超過多少視為高速（m/s）")]
    public float fourWS_SpeedThreshold = 15f;

    [Header("調試用")]
    [Tooltip("當前車輛速度 (km/h)")]
    public float currentSpeed = 0f;

    [Tooltip("馬達扭力分配")]
    public float torque;

    private float motorInput;
    private float steerInput;
    private float brakeInput;
    private Rigidbody rb;

    void SetupWheelFriction(WheelCollider wheel)
    {
        WheelFrictionCurve forwardFriction = wheel.forwardFriction;
        forwardFriction.extremumSlip = 0.4f;
        forwardFriction.extremumValue = 1f;
        forwardFriction.asymptoteSlip = 0.8f;
        forwardFriction.asymptoteValue = 0.5f;
        forwardFriction.stiffness = 1f;
        wheel.forwardFriction = forwardFriction;

        WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;
        sidewaysFriction.extremumSlip = 0.2f;
        sidewaysFriction.extremumValue = 1f;
        sidewaysFriction.asymptoteSlip = 0.5f;
        sidewaysFriction.asymptoteValue = 0.75f;
        sidewaysFriction.stiffness = 1f;
        wheel.sidewaysFriction = sidewaysFriction;
    }
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Rigidbody drag 調整
        rb.linearDamping = 0.05f;
        rb.angularDamping = 0.05f;

        SetupWheelFriction(frontLeftWheel);
        SetupWheelFriction(frontRightWheel);
        SetupWheelFriction(rearLeftWheel);
        SetupWheelFriction(rearRightWheel);
    }

    void Update()
    {
        motorInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");
        brakeInput = Input.GetKey(KeyCode.Space) ? 1f : 0f;

        // 模式切換
        if (Input.GetKeyDown(KeyCode.Alpha1)) driveType = DriveType.FWD;
        if (Input.GetKeyDown(KeyCode.Alpha2)) driveType = DriveType.RWD;
        if (Input.GetKeyDown(KeyCode.Alpha3)) driveType = DriveType.AWD;
        if (Input.GetKeyDown(KeyCode.Alpha4))
            steeringType = (steeringType == SteeringType.FrontOnly) ? SteeringType.FourWheel : SteeringType.FrontOnly;
    }

    void FixedUpdate()
    {
        // 前輪轉向
        float steerAngle = maxSteerAngle * steerInput;
        frontLeftWheel.steerAngle = steerAngle;
        frontRightWheel.steerAngle = steerAngle;

        // 四輪轉向
        if (steeringType == SteeringType.FourWheel)
        {
            float speed = rb.linearVelocity.magnitude;
            float factor = (speed < fourWS_SpeedThreshold) ? lowSpeedSteerFactor : highSpeedSteerFactor;
            float rearSteerAngle = steerAngle * factor;

            rearLeftWheel.steerAngle = rearSteerAngle;
            rearRightWheel.steerAngle = rearSteerAngle;
        }
        else
        {
            rearLeftWheel.steerAngle = 0f;
            rearRightWheel.steerAngle = 0f;
        }

        // ───────────────────────────────
        // 驅動力與方向邏輯
        // ───────────────────────────────
        float movingDirection = Vector3.Dot(transform.forward, rb.linearVelocity);
        float brakeTorque = brakeForce * brakeInput;
        torque = maxMotorTorque * motorInput;

        // 當方向與輸入相反 → 改為煞車
        if ((movingDirection > 0.5f && motorInput < 0) ||
            (movingDirection < -0.5f && motorInput > 0))
        {
            ApplyBrake(brakeForce);
            torque = 0f;
        }
        else
        {
            ApplyBrake(brakeTorque);
        }

        // ───────────────────────────────
        // 馬達扭力分配
        // ───────────────────────────────
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
                float splitTorque = torque * 0.5f;
                frontLeftWheel.motorTorque = splitTorque;
                frontRightWheel.motorTorque = splitTorque;
                rearLeftWheel.motorTorque = splitTorque;
                rearRightWheel.motorTorque = splitTorque;
                break;
        }

        // ───────────────────────────────
        // 自動阻力
        // ───────────────────────────────
        if (motorInput == 0f && brakeInput == 0f && rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            rb.AddForce(-rb.linearVelocity.normalized * autoBrakeForce, ForceMode.Acceleration);
        }

        // 更新輪胎模型
        UpdateWheelPose(frontLeftWheel, frontLeftMesh);
        UpdateWheelPose(frontRightWheel, frontRightMesh);
        UpdateWheelPose(rearLeftWheel, rearLeftMesh);
        UpdateWheelPose(rearRightWheel, rearRightMesh);

        // 更新速度 (km/h)
        currentSpeed = rb.linearVelocity.magnitude * 3.6f;
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
