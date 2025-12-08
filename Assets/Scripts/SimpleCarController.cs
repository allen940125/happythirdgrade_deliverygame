using System;
using Gamemanager;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class SimpleCarController : MonoBehaviour
{
    //──────────────────────────────────────────────
    // ENUMS
    //──────────────────────────────────────────────
    public enum DriveType { FWD, RWD, AWD }
    public enum SteeringType { FrontOnly, FourWheel }
    
    //──────────────────────────────────────────────
    // 車輛設定
    //──────────────────────────────────────────────
    [Header("車輛狀態")]
    public bool isDrivable = true; // << 新增這個開關
    
    [Header("玩家輸入")]
    public Vector2 MovementInput;
    
    [Header("驅動與轉向設定")]
    public DriveType driveType = DriveType.FWD;
    public SteeringType steeringType = SteeringType.FrontOnly;

    [Header("引擎參數")]
    public float engineMaxTorque = 600f;
    public AnimationCurve engineTorqueCurve = new AnimationCurve(
        new Keyframe(800, 0.6f),
        new Keyframe(3500, 1.0f),
        new Keyframe(6000, 0.9f),
        new Keyframe(8000, 0.7f)
    );

    [Header("檔位與傳動設定")]
    public float idleRPM = 800f;
    public float maxRPM = 6000f;
    public float rpmSmoothSpeed = 5f;
    public float[] gearRatios = { 3.2f, 2.1f, 1.4f, 1.0f, 0.8f };
    public float finalDriveRatio = 3.7f;

    [Header("車輛操控設定")]
    public float maxSteerAngle = 30f;
    public float brakeForce = 8000f;

    [Tooltip("放開油門時的阻力 (牛頓)")]
    public float autoBrakeForce = 1500f; // << 數值改為 1500 (代表 1500N 的力)

    [Tooltip("下壓力係數 (F = C * v^2)")]
    public float downforceCoefficient = 1.0f; // << 數值改為 1.0

    [Tooltip("空氣阻力係數 (F = C * v^2)")]
    public float airDragCoefficient = 0.5f; // << 新增這個變數

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

    [Header("四輪轉向設定")]
    [Tooltip("低速時後輪與前輪反向的最大角度比例")]
    public float lowSpeedSteerFactor = -0.5f;
    [Tooltip("高速時後輪與前輪同向的最大角度比例")]
    public float highSpeedSteerFactor = 0.3f;
    [Tooltip("速度超過多少視為高速（m/s）")]
    public float fourWS_SpeedThreshold = 15f;

    [Header("轉向靈敏度隨速度變化")]
    public AnimationCurve steerBySpeed = new AnimationCurve(
        new Keyframe(0, 1.0f),
        new Keyframe(50, 0.8f),
        new Keyframe(100, 0.6f),
        new Keyframe(200, 0.4f),
        new Keyframe(250, 0.3f)
    );

    [Header("動力衰減控制")]
    public bool useSpeedTorqueFalloff = true;
    public float maxEffectiveSpeed = 160;
    
    // 定義撞擊等級
    public enum CrashLevel
    {
        None,   // 沒事
        Light,  // 輕微擦傷 (刮漆聲)
        Medium, // 中度撞擊 (板金凹陷聲、鏡頭小晃)
        Heavy   // 嚴重車禍 (玻璃碎裂聲、車子失控、鏡頭劇烈震動)
    }

    [Header("💥 撞擊感測設定")]
    // 讓你可以用曲線控制「撞擊力道」對應「傷害/震動強度」的比例
    // 建議設定：X軸(0~100)代表撞擊力，Y軸(0~1)代表輸出強度
    public AnimationCurve damageCurve = AnimationCurve.Linear(0, 0, 100, 1);

    // 設定分級門檻 (可以在 Inspector 調整)
    public float lightCrashThreshold = 10f;
    public float mediumCrashThreshold = 30f;
    public float heavyCrashThreshold = 60f;

    // 定義一個事件，讓外部系統 (音效管理器、UI) 可以訂閱
    //public event Action<CrashLevel, float> OnCollisionHit;

    [Header("除錯資訊")]
    public float currentSpeed_H;   // km/h
    public float currentSpeed_S;   // m/s
    public float torque;           // 當前扭力
    public float currentRPM;
    public float targetRPM;
    public int currentGear;

    //──────────────────────────────────────────────
    // 私有成員
    //──────────────────────────────────────────────
    private Rigidbody rb;
    [SerializeField] private float motorInput;
    [SerializeField] private float steerInput;
    private float brakeInput;

    //──────────────────────────────────────────────
    // 初始化
    //──────────────────────────────────────────────
    private void Awake()
    {
        GameManager.Instance.SetPlayer(gameObject);
    }

    private void OnEnable()
    {
        // 事件訂閱（使用更清楚的命名）
        GameManager.Instance.MainGameEvent.SetSubscribe(
            GameManager.Instance.MainGameEvent.OnMovementKeyPressedEvent,
            cmd => {
                Debug.Log("Movement Event Triggered: " + cmd.MoveInput);
                MovementInput = cmd.MoveInput;
            }
        );
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, -0.45f, 0f);
        rb.linearDamping = 0.05f;
        rb.angularDamping = 0.15f;

        SetupWheelFriction(frontLeftWheel);
        SetupWheelFriction(frontRightWheel);
        SetupWheelFriction(rearLeftWheel);
        SetupWheelFriction(rearRightWheel);
    }

    //──────────────────────────────────────────────
    // 更新輸入
    //──────────────────────────────────────────────
    void Update()
    {
        //motorInput = MovementInput.y;
        //steerInput = MovementInput.x;
        //brakeInput = Input.GetKey(KeyCode.Space) ? 1f : 0f;

        // motorInput = Input.GetAxis("Vertical");
        // steerInput = Input.GetAxis("Horizontal");
        // brakeInput = Input.GetKey(KeyCode.Space) ? 1f : 0f;

        // // 驅動/轉向模式切換
        // if (Input.GetKeyDown(KeyCode.Alpha1)) driveType = DriveType.FWD;
        // if (Input.GetKeyDown(KeyCode.Alpha2)) driveType = DriveType.RWD;
        // if (Input.GetKeyDown(KeyCode.Alpha3)) driveType = DriveType.AWD;
        // if (Input.GetKeyDown(KeyCode.Alpha4))
        //     steeringType = (steeringType == SteeringType.FrontOnly) ? SteeringType.FourWheel : SteeringType.FrontOnly;
        
        // 檢查：如果不可駕駛，強制歸零輸入，並直接跳出
        if (!isDrivable)
        {
            MovementInput = Vector2.zero;
            motorInput = 0f;
            steerInput = 0f;
            brakeInput = 1f; // (可選) 死掉時是否要自動踩死煞車？
            return; 
        }
        
        steerInput = MovementInput.x;

        // 檢查是否有明確的後退/煞車/停止訊號
        if (MovementInput.y < 0f) // 訊號為負 (後退/雙按 UI 鍵)
        {
            motorInput = -1f;       // 全力倒車
            brakeInput = 0f;
        }
        // 這是最關鍵的一步：如果沒有後退訊號，就保持前進狀態 (Auto-Drive)
        else 
        {
            // 只要沒有明確的倒車訊號，一律保持油門
            motorInput = 1f;       // 全力油門 (Auto-Drive 核心)
            brakeInput = 0f;
        }
    }

    //──────────────────────────────────────────────
    // 物理更新
    //──────────────────────────────────────────────
    void FixedUpdate()
    {
        HandleSteering();
        HandleBraking();
        ApplyDriveTorque();

        // 更新速度
        currentSpeed_S = rb.linearVelocity.magnitude;
        currentSpeed_H = currentSpeed_S * 3.6f;

        // =================================================================
        // >> 物理力修正 (重要) <<
        // =================================================================

        // 1. 空氣阻力 (Air Drag) - 必須移到 if 之外，並且使用 ForceMode.Force
        //    公式 F = C * v^2，C 是阻力係數
        //    你需要一個新的 public 變數來控制它
        float airDrag = airDragCoefficient * currentSpeed_S * currentSpeed_S;
        rb.AddForce(-rb.linearVelocity.normalized * airDrag, ForceMode.Force);

        // 2. 下壓力 (Downforce) - 改為 ForceMode.Force，並移除 0.01f
        float downforce = downforceCoefficient * currentSpeed_S * currentSpeed_S;
        rb.AddForce(-transform.up * downforce, ForceMode.Force);

        // 3. 自動煞車 / 引擎阻力 - 改為 ForceMode.Force
        if (motorInput == 0f && brakeInput == 0f && rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            rb.AddForce(-rb.linearVelocity.normalized * autoBrakeForce, ForceMode.Force);
        }
        // =================================================================

        if (motorInput == 0f && brakeInput == 0f && currentSpeed_S < 0.5f)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);

            if (rb.linearVelocity.magnitude < 0.05f)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        
        // 檔位與轉速
        UpdateGearAndRPM();

        // 更新輪胎外觀
        UpdateWheelPose(frontLeftWheel, frontLeftMesh);
        UpdateWheelPose(frontRightWheel, frontRightMesh);
        UpdateWheelPose(rearLeftWheel, rearLeftMesh);
        UpdateWheelPose(rearRightWheel, rearRightMesh);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 1. 計算有效撞擊力道 (垂直於牆面的速度分量)
        Vector3 hitNormal = collision.contacts[0].normal;
        Vector3 relativeVelocity = collision.relativeVelocity;
        float impactForce = Vector3.Dot(hitNormal, relativeVelocity);

        // 如果是負值或太小，直接忽略
        if (impactForce < lightCrashThreshold) return;

        // 2. 判斷撞擊級別 (由重到輕判斷)
        CrashLevel severity = CrashLevel.None;

        if (impactForce >= heavyCrashThreshold)
        {
            severity = CrashLevel.Heavy;
        }
        else if (impactForce >= mediumCrashThreshold)
        {
            severity = CrashLevel.Medium;
        }
        else
        {
            severity = CrashLevel.Light;
        }

        // 3. 利用曲線計算一個 0~1 的強度值 (可以用來扣血或震動鏡頭)
        // 假設 impactForce 是 50，Curve 會幫你算出對應的 0.x 值
        float damageFactor = damageCurve.Evaluate(impactForce);

        Debug.Log($"<color=orange>撞擊發生!</color> 力道: {impactForce:F1} | 級別: {severity} | 傷害係數: {damageFactor:F2}");

        // 4. 發送事件給其他系統 (UI、音效、相機震動)
        // 這樣寫可以保持 CarController 很乾淨，不用把播放音效的程式碼寫在這裡
        
        GameManager.Instance.MainGameEvent.Send(new PlayerHurtPressedEvent() { SCrashLevel = severity, HurtValue = damageFactor });
        //OnCollisionHit?.Invoke(severity, damageFactor);
        
        // (可選) 簡單的反作用力，讓車子彈開更有感
        if (severity >= CrashLevel.Medium)
        {
            rb.AddForce(hitNormal * impactForce * 50f, ForceMode.Impulse);
        }
    }
    
    //──────────────────────────────────────────────
    // 各功能區域
    //──────────────────────────────────────────────

    // 新增一個公開方法讓外部呼叫
    public void SetDrivable(bool state)
    {
        isDrivable = state;
    }
    
    #region Steering & Handling
    void HandleSteering()
    {
        float steerFactorBySpeed = steerBySpeed.Evaluate(currentSpeed_H);
        float steerAngle = maxSteerAngle * steerInput * steerFactorBySpeed;

        // 前輪轉向
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
    }
    #endregion

    #region Braking
    void HandleBraking()
    {
        float movingDirection = Vector3.Dot(transform.forward, rb.linearVelocity);
        float brakeTorque = brakeForce * brakeInput;

        // 若輸入與行進方向相反 → 自動煞車
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
    }

    void ApplyBrake(float brakeTorque)
    {
        frontLeftWheel.brakeTorque = brakeTorque;
        frontRightWheel.brakeTorque = brakeTorque;
        rearLeftWheel.brakeTorque = brakeTorque;
        rearRightWheel.brakeTorque = brakeTorque;
    }
    #endregion

    #region Engine & Torque
    void ApplyDriveTorque()
    {
        float torqueCurveFactor = engineTorqueCurve.Evaluate(currentRPM);
        float currentEngineTorque = engineMaxTorque * torqueCurveFactor * motorInput;

        ApplyTorqueWithTraction(currentEngineTorque);
    }

    void ApplyTorqueWithTraction(float engineTorque)
    {
        float slipThreshold = 1.2f;
        float reduceFactor = 1f;
        WheelHit hit;

        // 簡單牽引力控制（TCS）
        if (frontLeftWheel.GetGroundHit(out hit) && Mathf.Abs(hit.forwardSlip) > slipThreshold) reduceFactor = 0.5f;
        if (frontRightWheel.GetGroundHit(out hit) && Mathf.Abs(hit.forwardSlip) > slipThreshold) reduceFactor = 0.5f;
        if (rearLeftWheel.GetGroundHit(out hit) && Mathf.Abs(hit.forwardSlip) > slipThreshold) reduceFactor = 0.5f;
        if (rearRightWheel.GetGroundHit(out hit) && Mathf.Abs(hit.forwardSlip) > slipThreshold) reduceFactor = 0.5f;

        float totalTorque = engineTorque * gearRatios[currentGear] * finalDriveRatio * reduceFactor;

        switch (driveType)
        {
            case DriveType.FWD:
                frontLeftWheel.motorTorque = totalTorque / 2f;
                frontRightWheel.motorTorque = totalTorque / 2f;
                break;
            case DriveType.RWD:
                rearLeftWheel.motorTorque = totalTorque / 2f;
                rearRightWheel.motorTorque = totalTorque / 2f;
                break;
            case DriveType.AWD:
                frontLeftWheel.motorTorque = totalTorque / 4f;
                frontRightWheel.motorTorque = totalTorque / 4f;
                rearLeftWheel.motorTorque = totalTorque / 4f;
                rearRightWheel.motorTorque = totalTorque / 4f;
                break;
        }
    }
    #endregion

    #region Gear & RPM
    void UpdateGearAndRPM()
    {
        // 1. 根據驅動模式，計算驅動輪的平均 RPM
        float avgWheelRPM = 0;
        int driveWheels = 0;

        if (driveType == DriveType.FWD || driveType == DriveType.AWD)
        {
            avgWheelRPM += frontLeftWheel.rpm + frontRightWheel.rpm;
            driveWheels += 2;
        }
        if (driveType == DriveType.RWD || driveType == DriveType.AWD)
        {
            avgWheelRPM += rearLeftWheel.rpm + rearRightWheel.rpm;
            driveWheels += 2;
        }

        if (driveWheels > 0)
        {
            avgWheelRPM /= driveWheels;
        }

        // 2. 由輪速反推轉速
        targetRPM = idleRPM + Mathf.Abs(avgWheelRPM) * gearRatios[currentGear] * finalDriveRatio;
    
        // 3. 限制 RPM 範圍
        targetRPM = Mathf.Clamp(targetRPM, idleRPM, maxRPM);
        currentRPM = Mathf.Lerp(currentRPM, targetRPM, Time.deltaTime * rpmSmoothSpeed);

        // 4. 自排邏輯
        // (您的邏輯是 5500 升檔，最大 6000，這是正常的)
        if (motorInput > 0.5f && currentRPM > (maxRPM * 0.9f) && currentGear < gearRatios.Length - 1) // 90% 轉速升檔
        {
            currentGear++;
        }
        else if (currentRPM < (maxRPM * 0.4f) && currentGear > 0) // 40% 轉速降檔
        {
            // 稍微簡化降檔邏輯
            if (motorInput < 0.1f || currentRPM < (maxRPM * 0.3f))
            {
                currentGear--;
            }
        }
    }
    #endregion

    #region Wheel Setup & Visuals
    void SetupWheelFriction(WheelCollider wheel)
    {
        bool isFrontWheel = (wheel == frontLeftWheel || wheel == frontRightWheel);

        WheelFrictionCurve f = wheel.forwardFriction;
        f.extremumSlip = 0.3f;
        f.extremumValue = 1f;
        f.asymptoteSlip = 0.8f;
        f.asymptoteValue = 0.75f;
        f.stiffness = isFrontWheel ? 2.5f : 3.0f;
        wheel.forwardFriction = f;

        WheelFrictionCurve s = wheel.sidewaysFriction;
        s.extremumSlip = 0.25f;
        s.extremumValue = 1f;
        s.asymptoteSlip = 0.6f;
        s.asymptoteValue = 0.8f;
        s.stiffness = isFrontWheel ? 3.0f : 3.5f;
        wheel.sidewaysFriction = s;
    }

    void UpdateWheelPose(WheelCollider collider, Transform mesh)
    {
        if (mesh == null) return;
        collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.SetPositionAndRotation(pos, rot);
    }
    #endregion
}

