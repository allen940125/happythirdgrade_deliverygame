using UnityEngine;
using System;

// 為了讓其他腳本不用打 BaseCarController.DriveType，我們把 Enum 放在外面
public enum DriveType { FWD, RWD, AWD }
public enum SteeringType { FrontOnly, FourWheel }
public enum CrashLevel { None, Light, Medium, Heavy }

[RequireComponent(typeof(Rigidbody))]
public class SimpleCarController : MonoBehaviour
{
    //──────────────────────────────────────────────
    // 1. 車輛共用參數 (Inspector 設定)
    //──────────────────────────────────────────────
    [Header("車輛狀態")]
    public bool isDrivable = true;

    [Header("驅動與轉向設定")]
    public DriveType driveType = DriveType.FWD;
    public SteeringType steeringType = SteeringType.FrontOnly;

    [Header("引擎參數")]
    public float engineMaxTorque = 600f;
    public AnimationCurve engineTorqueCurve = new AnimationCurve(
        new Keyframe(800, 0.6f), new Keyframe(3500, 1.0f),
        new Keyframe(6000, 0.9f), new Keyframe(8000, 0.7f)
    );

    [Header("檔位與傳動")]
    public float idleRPM = 800f;
    public float maxRPM = 6000f;
    public float rpmSmoothSpeed = 5f;
    public float[] gearRatios = { 3.2f, 2.1f, 1.4f, 1.0f, 0.8f };
    public float finalDriveRatio = 3.7f;

    [Header("操控與物理")]
    public float maxSteerAngle = 30f;
    public float brakeForce = 8000f;
    [Tooltip("放開油門時的阻力 (牛頓)")]
    public float autoBrakeForce = 1500f;
    [Tooltip("下壓力係數 (F = C * v^2)")]
    public float downforceCoefficient = 1.0f;
    [Tooltip("空氣阻力係數 (F = C * v^2)")]
    public float airDragCoefficient = 0.5f;

    [Header("輪胎 Collider")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    [Header("輪胎模型 (Visual)")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("四輪轉向設定")]
    public float lowSpeedSteerFactor = -0.5f;
    public float highSpeedSteerFactor = 0.3f;
    public float fourWS_SpeedThreshold = 15f;
    public AnimationCurve steerBySpeed = new AnimationCurve(
        new Keyframe(0, 1.0f), new Keyframe(50, 0.8f), 
        new Keyframe(100, 0.6f), new Keyframe(250, 0.3f));

    [Header("💥 撞擊感測設定")]
    public AnimationCurve damageCurve = AnimationCurve.Linear(0, 0, 100, 1);
    public float lightCrashThreshold = 10f;
    public float mediumCrashThreshold = 30f;
    public float heavyCrashThreshold = 60f;

    //──────────────────────────────────────────────
    // 2. 內部狀態 (供子類別讀取)
    //──────────────────────────────────────────────
    protected Rigidbody rb;
    
    // 這些是真正的控制訊號，由子類別透過 SetInputs() 來修改
    protected float steerInput; // -1 ~ 1
    protected float motorInput; // -1 ~ 1
    protected float brakeInput; // 0 ~ 1

    // 公開數據供 UI 顯示
    public float CurrentSpeedKmH { get; private set; }
    public float CurrentSpeedMS { get; private set; }
    public float CurrentRPM { get; private set; }
    public int CurrentGear { get; private set; }

    private float targetRPM;

    //──────────────────────────────────────────────
    // 3. 初始化與核心循環
    //──────────────────────────────────────────────
    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, -0.45f, 0f); // 重心降低防止翻車
        rb.linearDamping = 0.05f;
        rb.angularDamping = 0.15f;

        SetupWheelFriction(frontLeftWheel);
        SetupWheelFriction(frontRightWheel);
        SetupWheelFriction(rearLeftWheel);
        SetupWheelFriction(rearRightWheel);
    }

    protected virtual void FixedUpdate()
    {
        // 1. 如果不可駕駛，強制歸零輸入 (但物理慣性還要跑)
        if (!isDrivable)
        {
            steerInput = 0;
            motorInput = 0;
            brakeInput = 1; // 死亡鎖死煞車
        }

        // 2. 執行物理邏輯
        HandleSteering();
        ApplyDriveTorque();
        ApplyBraking();
        ApplyAerodynamics(); // 空氣阻力與下壓力

        // 3. 更新數據
        CurrentSpeedMS = rb.linearVelocity.magnitude;
        CurrentSpeedKmH = CurrentSpeedMS * 3.6f;
        
        // 4. 低速自動停止修正
        if (motorInput == 0f && brakeInput == 0f && CurrentSpeedMS < 0.5f)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
        }

        UpdateGearAndRPM();
        UpdateAllWheelPoses();
    }

    //──────────────────────────────────────────────
    // 4. 對外接口 (API)
    //──────────────────────────────────────────────
    
    /// <summary>
    /// 子類別 (Player/Enemy) 呼叫此方法來開車
    /// </summary>
    public void SetInputs(float steer, float motor, float brake)
    {
        steerInput = Mathf.Clamp(steer, -1f, 1f);
        motorInput = Mathf.Clamp(motor, -1f, 1f);
        brakeInput = Mathf.Clamp01(brake);
    }

    /// <summary>
    /// 設定車輛是否可操控
    /// </summary>
    public void SetDrivable(bool state)
    {
        isDrivable = state;
    }

    //──────────────────────────────────────────────
    // 5. 物理實作細節
    //──────────────────────────────────────────────

    void HandleSteering()
    {
        float steerFactorBySpeed = steerBySpeed.Evaluate(CurrentSpeedKmH);
        float steerAngle = maxSteerAngle * steerInput * steerFactorBySpeed;

        frontLeftWheel.steerAngle = steerAngle;
        frontRightWheel.steerAngle = steerAngle;

        // 四輪轉向邏輯
        if (steeringType == SteeringType.FourWheel)
        {
            float factor = (CurrentSpeedMS < fourWS_SpeedThreshold) ? lowSpeedSteerFactor : highSpeedSteerFactor;
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

    void ApplyDriveTorque()
    {
        float torqueCurveFactor = engineTorqueCurve.Evaluate(CurrentRPM);
        float currentEngineTorque = engineMaxTorque * torqueCurveFactor * motorInput;
        
        // TCS 牽引力控制 (簡化版)
        float reduceFactor = 1f;
        if (IsSlipping(frontLeftWheel) || IsSlipping(frontRightWheel) || 
            IsSlipping(rearLeftWheel) || IsSlipping(rearRightWheel))
        {
            reduceFactor = 0.5f;
        }

        float totalTorque = currentEngineTorque * gearRatios[CurrentGear] * finalDriveRatio * reduceFactor;

        // 分配扭力
        ApplyTorqueToWheels(totalTorque);
    }

    void ApplyTorqueToWheels(float totalTorque)
    {
        // 先歸零
        frontLeftWheel.motorTorque = 0; frontRightWheel.motorTorque = 0;
        rearLeftWheel.motorTorque = 0; rearRightWheel.motorTorque = 0;

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

    void ApplyBraking()
    {
        // 判斷是否需要「自動煞車/引擎煞車」
        // 條件：玩家沒有踩油門 也沒有踩煞車 且 車子還在動
        bool isEngineBraking = (motorInput == 0f && brakeInput == 0f && CurrentSpeedMS > 0.1f);
        
        // 判斷是否「反向煞車」（車子向前滑但玩家按後退）
        float movingDir = Vector3.Dot(transform.forward, rb.linearVelocity);
        bool isReverseBraking = (movingDir > 0.5f && motorInput < 0) || (movingDir < -0.5f && motorInput > 0);

        float finalBrakeTorque = 0f;

        if (isReverseBraking)
        {
            finalBrakeTorque = brakeForce; // 全力煞車準備換向
            // 切斷動力以防衝突
            frontLeftWheel.motorTorque = 0; frontRightWheel.motorTorque = 0;
            rearLeftWheel.motorTorque = 0; rearRightWheel.motorTorque = 0;
        }
        else if (isEngineBraking)
        {
            // 這裡改用 AddForce 模擬引擎阻力，而不是用 WheelCollider.brakeTorque，手感較好
            rb.AddForce(-rb.linearVelocity.normalized * autoBrakeForce, ForceMode.Force);
            finalBrakeTorque = 0f; // 輪胎本身不鎖死
        }
        else
        {
            finalBrakeTorque = brakeForce * brakeInput;
        }

        // 套用煞車值
        frontLeftWheel.brakeTorque = finalBrakeTorque;
        frontRightWheel.brakeTorque = finalBrakeTorque;
        rearLeftWheel.brakeTorque = finalBrakeTorque;
        rearRightWheel.brakeTorque = finalBrakeTorque;
    }

    void ApplyAerodynamics()
    {
        // 空氣阻力
        float airDrag = airDragCoefficient * CurrentSpeedMS * CurrentSpeedMS;
        rb.AddForce(-rb.linearVelocity.normalized * airDrag, ForceMode.Force);

        // 下壓力
        float downforce = downforceCoefficient * CurrentSpeedMS * CurrentSpeedMS;
        rb.AddForce(-transform.up * downforce, ForceMode.Force);
    }

    void UpdateGearAndRPM()
    {
        // 計算輪子平均轉速
        float avgWheelRPM = (rearLeftWheel.rpm + rearRightWheel.rpm) / 2f; // 以後輪為基準
        if(driveType == DriveType.FWD) avgWheelRPM = (frontLeftWheel.rpm + frontRightWheel.rpm) / 2f;

        targetRPM = idleRPM + Mathf.Abs(avgWheelRPM) * gearRatios[CurrentGear] * finalDriveRatio;
        targetRPM = Mathf.Clamp(targetRPM, idleRPM, maxRPM);
        CurrentRPM = Mathf.Lerp(CurrentRPM, targetRPM, Time.deltaTime * rpmSmoothSpeed);

        // 自動換檔邏輯
        if (motorInput > 0.5f && CurrentRPM > (maxRPM * 0.9f) && CurrentGear < gearRatios.Length - 1)
        {
            CurrentGear++;
        }
        else if (CurrentGear > 0)
        {
            if (motorInput < 0.1f || CurrentRPM < (maxRPM * 0.4f))
            {
                CurrentGear--;
            }
        }
    }

    //──────────────────────────────────────────────
    // 6. 撞擊處理 (Virtual)
    //──────────────────────────────────────────────
    protected virtual void OnCollisionEnter(Collision collision)
    {
        // 1. 計算撞擊力
        Vector3 hitNormal = collision.contacts[0].normal;
        float impactForce = Vector3.Dot(hitNormal, collision.relativeVelocity);
        
        if (impactForce < lightCrashThreshold) return;

        // 2. 判斷等級
        CrashLevel severity = CrashLevel.Light;
        if (impactForce >= heavyCrashThreshold) severity = CrashLevel.Heavy;
        else if (impactForce >= mediumCrashThreshold) severity = CrashLevel.Medium;

        // 3. 計算傷害係數 (0~1)
        float damageFactor = damageCurve.Evaluate(impactForce);

        // 4. 呼叫虛擬方法 (讓子類別決定要不要扣血、播放音效或送事件)
        OnCarCrash(severity, damageFactor, impactForce, hitNormal);

        // 5. 物理反彈 (共用)
        if (severity >= CrashLevel.Medium)
        {
            rb.AddForce(hitNormal * impactForce * 20f, ForceMode.Impulse); // 稍微彈開
        }
    }

    /// <summary>
    /// 子類別可以 Override 這個方法來處理具體的遊戲邏輯 (如扣血、UI通知)
    /// </summary>
    protected virtual void OnCarCrash(CrashLevel level, float damageFactor, float impactForce, Vector3 hitNormal)
    {
        Debug.Log($"[BaseCar] 發生撞擊! Level: {level}, Factor: {damageFactor:F2}");
        // 預設這裡什麼都不做，交給 PlayerCarController 或 EnemyCarController 去實作
    }

    //──────────────────────────────────────────────
    // 7. 輔助函式
    //──────────────────────────────────────────────
    bool IsSlipping(WheelCollider wheel)
    {
        WheelHit hit;
        if(wheel.GetGroundHit(out hit))
        {
            return Mathf.Abs(hit.forwardSlip) > 1.0f; // 簡單判定
        }
        return false;
    }

    void SetupWheelFriction(WheelCollider wheel)
    {
        WheelFrictionCurve f = wheel.forwardFriction;
        f.stiffness = 2.0f;
        wheel.forwardFriction = f;

        WheelFrictionCurve s = wheel.sidewaysFriction;
        s.stiffness = 2.5f;
        wheel.sidewaysFriction = s;
    }

    void UpdateAllWheelPoses()
    {
        UpdateWheelPose(frontLeftWheel, frontLeftMesh);
        UpdateWheelPose(frontRightWheel, frontRightMesh);
        UpdateWheelPose(rearLeftWheel, rearLeftMesh);
        UpdateWheelPose(rearRightWheel, rearRightMesh);
    }

    void UpdateWheelPose(WheelCollider collider, Transform mesh)
    {
        if (mesh == null) return;
        collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.SetPositionAndRotation(pos, rot);
    }
}