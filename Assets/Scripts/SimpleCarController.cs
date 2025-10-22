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
    public float autoBrakeForce = 5f;

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

    [Header("空力 / 下壓")]
    public float downforceCoefficient = 5f; // 調整用
    
    [Header("高速轉向變鈍、低速轉向靈敏")]
    public AnimationCurve steerBySpeed = new AnimationCurve(
        new Keyframe(0,   1.0f),
        new Keyframe(50,  0.8f),
        new Keyframe(100, 0.6f),
        new Keyframe(200, 0.4f),
        new Keyframe(250, 0.3f)
    );
    
    [Header("動力衰減控制")]
    public bool useSpeedTorqueFalloff = true;
    public float maxEffectiveSpeed = 160;
    
    [Header("調試用")]
    public float currentSpeed_H = 0f;
    public float currentSpeed_S = 0f;
    public float torque;

    private float motorInput;
    private float steerInput;
    private float brakeInput;
    private Rigidbody rb;

    void SetupWheelFriction(WheelCollider wheel)
    {
        WheelFrictionCurve f = wheel.forwardFriction;
        f.extremumSlip = 0.2f;
        f.extremumValue = 1f;
        f.asymptoteSlip = 0.6f;
        f.asymptoteValue = 0.7f;
        f.stiffness = 1.1f;
        wheel.forwardFriction = f;

        WheelFrictionCurve s = wheel.sidewaysFriction;
        s.extremumSlip = 0.15f;        // 小一些表示更快到達極限
        s.extremumValue = 1f;
        s.asymptoteSlip = 0.5f;
        s.asymptoteValue = 0.8f;
        s.stiffness = 1.6f;           // 提高側向剛性（重要）
        wheel.sidewaysFriction = s;
    }


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = Mathf.Max(rb.mass, 120f); // 可視車子大小調整
        rb.centerOfMass = new Vector3(0f, -0.45f, 0f); // 下調中心，改善抓地
        rb.linearDamping = 0.1f;
        rb.angularDamping = 0.15f;

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
        float steerFactorBySpeed = steerBySpeed.Evaluate(currentSpeed_H); // 0~1 比例
        float steerAngle = maxSteerAngle * steerInput * steerFactorBySpeed;
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
        currentSpeed_H = rb.linearVelocity.magnitude * 3.6f;
        currentSpeed_S = rb.linearVelocity.magnitude;
        
        // 更新檔位與轉速
        UpdateGearAndRPM();

        // 更新輪胎模型
        UpdateWheelPose(frontLeftWheel, frontLeftMesh);
        UpdateWheelPose(frontRightWheel, frontRightMesh);
        UpdateWheelPose(rearLeftWheel, rearLeftMesh);
        UpdateWheelPose(rearRightWheel, rearRightMesh);
        
        // 加入下壓力（平方或線性都可）
        float downforce = downforceCoefficient * currentSpeed_S * currentSpeed_S * 0.01f; // 可微調係數
        rb.AddForce(-transform.up * downforce, ForceMode.Acceleration);
        
        // 之後其餘程式繼續...
    }

    void ApplyDriveTorque()
    {
        float speedKmh = rb.linearVelocity.magnitude * 3.6f;
        float speedFactor = useSpeedTorqueFalloff ? Mathf.Clamp01(1f - (speedKmh / maxEffectiveSpeed)) : 1f;
        float gearEffect = gearRatios[currentGear];
        float effectiveTorque = maxMotorTorque * motorInput * gearEffect * (0.4f + 0.6f * speedFactor); // 保留基礎扭力

        // 簡易牽引力控制：如果驅動輪 slip 太大，削減扭力
        ApplyTorqueWithTraction(frontLeftWheel, frontRightWheel, rearLeftWheel, rearRightWheel, effectiveTorque);
    }

    void ApplyTorqueWithTraction(WheelCollider fl, WheelCollider fr, WheelCollider rl, WheelCollider rr, float desiredTorque)
    {
        // 讀每顆輪的滑差（WheelHit）
        float slipThreshold = 1.8f;
        float reduceFactor = 1f;

        WheelHit hit;
        if (frontLeftWheel.GetGroundHit(out hit) && Mathf.Abs(hit.forwardSlip) > slipThreshold) reduceFactor *= 0.8f;
        if (frontRightWheel.GetGroundHit(out hit) && Mathf.Abs(hit.forwardSlip) > slipThreshold) reduceFactor *= 0.8f;
        if (rearLeftWheel.GetGroundHit(out hit) && Mathf.Abs(hit.forwardSlip) > slipThreshold) reduceFactor *= 0.8f;
        if (rearRightWheel.GetGroundHit(out hit) && Mathf.Abs(hit.forwardSlip) > slipThreshold) reduceFactor *= 0.8f;

        float effTorque = desiredTorque * reduceFactor;

        // 分配給驅動輪（保留你原本的 driveType 邏輯）
        switch (driveType)
        {
            case DriveType.FWD:
                fl.motorTorque = effTorque;
                fr.motorTorque = effTorque;
                rl.motorTorque = 0f;
                rr.motorTorque = 0f;
                break;
            case DriveType.RWD:
                rl.motorTorque = effTorque;
                rr.motorTorque = effTorque;
                fl.motorTorque = 0f;
                fr.motorTorque = 0f;
                break;
            default: // AWD
                float split = effTorque * 0.5f;
                fl.motorTorque = split;
                fr.motorTorque = split;
                rl.motorTorque = split;
                rr.motorTorque = split;
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
        float wheelRPM = (currentSpeed_H * 60f) / (2f * Mathf.PI * 0.34f); // 假設輪胎半徑 0.34m
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
