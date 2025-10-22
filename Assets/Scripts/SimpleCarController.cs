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
    public float maxMotorTorque = 5000f;  // 輪子最大扭力（越大加速越快）
    public float maxSteerAngle = 30f;
    public float brakeForce = 8000f;
    public float autoBrakeForce = 10f;

    [Header("檔位與引擎設定")]
    public float idleRPM = 800f;
    public float maxRPM = 6000f;
    public float rpmSmoothSpeed = 5f;
    public float[] gearRatios = { 3.2f, 2.1f, 1.4f, 1.0f, 0.8f }; // 五速自排
    public int currentGear = 0;
    public float currentRPM;
    public float targetRPM;

    [Header("四輪轉向設定")]
    [Tooltip("低速時後輪與前輪反向的最大角度比例")]
    public float lowSpeedSteerFactor = -0.5f;
    [Tooltip("高速時後輪與前輪同向的最大角度比例")]
    public float highSpeedSteerFactor = 0.3f;
    [Tooltip("速度超過多少視為高速（m/s）")]
    public float fourWS_SpeedThreshold = 15f;

    [Header("調試用")]
    public float currentSpeed = 0f;
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
        // ───────────────────────────────
        // 前輪轉向
        // ───────────────────────────────
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
        // 驅動與煞車
        // ───────────────────────────────
        float movingDirection = Vector3.Dot(transform.forward, rb.linearVelocity);
        float brakeTorque = brakeForce * brakeInput;
        torque = maxMotorTorque * motorInput;

        // 若方向與輸入相反 → 自動煞車
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

        // 扭力分配
        ApplyDriveTorque();

        // 沒油門與煞車時 → 自動阻力
        if (motorInput == 0f && brakeInput == 0f && rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            rb.AddForce(-rb.linearVelocity.normalized * autoBrakeForce, ForceMode.Acceleration);
        }

        // 更新車速
        currentSpeed = rb.linearVelocity.magnitude * 3.6f;

        // 更新檔位與轉速
        UpdateGearAndRPM();

        // 更新輪胎模型
        UpdateWheelPose(frontLeftWheel, frontLeftMesh);
        UpdateWheelPose(frontRightWheel, frontRightMesh);
        UpdateWheelPose(rearLeftWheel, rearLeftMesh);
        UpdateWheelPose(rearRightWheel, rearRightMesh);
    }

    void ApplyDriveTorque()
    {
        float gearEffect = gearRatios[currentGear];
        float effectiveTorque = maxMotorTorque * motorInput * gearEffect;

        switch (driveType)
        {
            case DriveType.FWD:
                frontLeftWheel.motorTorque = effectiveTorque;
                frontRightWheel.motorTorque = effectiveTorque;
                rearLeftWheel.motorTorque = 0f;
                rearRightWheel.motorTorque = 0f;
                break;

            case DriveType.RWD:
                rearLeftWheel.motorTorque = effectiveTorque;
                rearRightWheel.motorTorque = effectiveTorque;
                frontLeftWheel.motorTorque = 0f;
                frontRightWheel.motorTorque = 0f;
                break;

            case DriveType.AWD:
                float splitTorque = effectiveTorque * 0.5f;
                frontLeftWheel.motorTorque = splitTorque;
                frontRightWheel.motorTorque = splitTorque;
                rearLeftWheel.motorTorque = splitTorque;
                rearRightWheel.motorTorque = splitTorque;
                break;
        }
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

    // ───────────────────────────────
    // 自動檔位與轉速系統
    // ───────────────────────────────
    void UpdateGearAndRPM()
    {
        // 根據車速與檔位，估算引擎轉速（簡化模擬）
        float wheelRPM = (currentSpeed * 60f) / (2f * Mathf.PI * 0.34f); // 假設輪胎半徑 0.34m
        targetRPM = idleRPM + wheelRPM * gearRatios[currentGear];
        targetRPM = Mathf.Clamp(targetRPM, idleRPM, maxRPM);
        currentRPM = Mathf.Lerp(currentRPM, targetRPM, Time.deltaTime * rpmSmoothSpeed);

        // 自動升檔與降檔
        if (currentRPM > 5500f && currentGear < gearRatios.Length - 1)
            currentGear++;
        else if (currentRPM < 2000f && currentGear > 0)
            currentGear--;
    }
}
