using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class EnemyCarAI : MonoBehaviour
{
    //──────────────────────────────────────────────
    // AI 設定 (新加入的部分)
    //──────────────────────────────────────────────
    [Header("🤖 AI 行為設定")]
    public Transform target;          // AI 要追逐的目標 (通常是玩家)
    public float sensorLength = 10f;  // 感測器射線長度
    public float sideSensorPos = 1.5f;// 左右感測器的偏移量
    public float avoidSpeed = 10f;    // 閃避時的轉向強度
    
    [Header("🤖 防卡死系統")]
    public float stuckCheckTime = 2f; // 多久沒移動視為卡住
    public float reverseTime = 1.5f;  // 卡住後倒車多久
    private float stuckTimer;
    private bool isReversing;

    //──────────────────────────────────────────────
    // 原有車輛設定 (保留你的物理參數)
    //──────────────────────────────────────────────
    [Header("車輛狀態")]
    public bool isDrivable = true;

    [Header("驅動與轉向設定")]
    public SimpleCarController.DriveType driveType = SimpleCarController.DriveType.RWD; // AI 建議用 RWD 或 AWD 比較好甩
    public SimpleCarController.SteeringType steeringType = SimpleCarController.SteeringType.FrontOnly;

    [Header("引擎參數")]
    public float engineMaxTorque = 600f;
    public AnimationCurve engineTorqueCurve = new AnimationCurve(
        new Keyframe(800, 0.6f), new Keyframe(3500, 1.0f), 
        new Keyframe(6000, 0.9f), new Keyframe(8000, 0.7f));

    [Header("檔位與傳動")]
    public float idleRPM = 800f;
    public float maxRPM = 6000f;
    public float rpmSmoothSpeed = 5f;
    public float[] gearRatios = { 3.2f, 2.1f, 1.4f, 1.0f, 0.8f };
    public float finalDriveRatio = 3.7f;

    [Header("操控與物理")]
    public float maxSteerAngle = 35f; // AI 可以給稍微大一點的角度
    public float brakeForce = 8000f;
    public float autoBrakeForce = 1500f;
    public float downforceCoefficient = 1.0f;
    public float airDragCoefficient = 0.5f;

    [Header("輪胎 Collider")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    [Header("輪胎模型")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    //──────────────────────────────────────────────
    // 內部變數
    //──────────────────────────────────────────────
    private Rigidbody rb;
    private float motorInput;
    private float steerInput;
    private float brakeInput;

    public float currentSpeed_H; // km/h
    public float currentSpeed_S; // m/s
    public float currentRPM;
    private float targetRPM;
    private int currentGear;

    private void Start()
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
    // AI 思考邏輯 (取代原本的 Update 輸入)
    //──────────────────────────────────────────────
    private void Update()
    {
        if (!isDrivable || target == null)
        {
            motorInput = 0;
            brakeInput = 1;
            return;
        }

        // 1. 感測器偵測 (避障邏輯)
        float avoidanceVal = 0f;
        bool obstacleDetected = Sensors(out avoidanceVal);

        // 2. 追蹤目標邏輯
        Vector3 relativeVector = transform.InverseTransformPoint(target.position);
        float newSteer = (relativeVector.x / relativeVector.magnitude); // 簡單的方向判定 (-1 ~ 1)

        // 3. 整合轉向 (如果偵測到障礙物，避障優先；否則追蹤目標)
        if (obstacleDetected)
        {
            steerInput = avoidanceVal;
        }
        else
        {
            steerInput = newSteer;
        }

        // 4. 油門與煞車控制 (轉彎時自動減速)
        if (isReversing)
        {
            // 倒車模式
            motorInput = -1f;
            brakeInput = 0f;
            steerInput = -steerInput; // 倒車時反向打盤
        }
        else
        {
            // 前進模式
            // 如果轉向角度大 (Mathf.Abs(steerInput) > 0.5f)，油門減小
            float cornerFactor = Mathf.Clamp01(1.0f - Mathf.Abs(steerInput));
            
            // 根據是否需要急轉彎來控制油門
            motorInput = Mathf.Lerp(0.3f, 1f, cornerFactor); 
            
            // 如果轉向太急且速度過快，自動踩煞車
            if (Mathf.Abs(steerInput) > 0.6f && currentSpeed_H > 40f)
            {
                motorInput = 0f;
                brakeInput = 1f;
            }
            else
            {
                brakeInput = 0f;
            }
        }
        
        CheckIfStuck();
    }

    //──────────────────────────────────────────────
    // 物理與驅動 (FixedUpdate)
    //──────────────────────────────────────────────
    private void FixedUpdate()
    {
        HandleSteering();
        ApplyDriveTorque();
        ApplyBrakes();
        AddDownForceAndDrag(); // 整合後的物理力

        // 更新速度與數據
        currentSpeed_S = rb.linearVelocity.magnitude;
        currentSpeed_H = currentSpeed_S * 3.6f;

        UpdateGearAndRPM();
        
        // 更新輪胎視覺
        UpdateWheelPose(frontLeftWheel, frontLeftMesh);
        UpdateWheelPose(frontRightWheel, frontRightMesh);
        UpdateWheelPose(rearLeftWheel, rearLeftMesh);
        UpdateWheelPose(rearRightWheel, rearRightMesh);
    }

    //──────────────────────────────────────────────
    // AI 感測器系統 (Raycast)
    //──────────────────────────────────────────────
    bool Sensors(out float avoidVal)
    {
        avoidVal = 0;
        bool hitSomething = false;
        float avoidMultiplier = 0f;
        
        Vector3 sensorStartPos = transform.position;
        sensorStartPos.y += 0.5f; // 抬高一點避免照到地板

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        // 1. 中央射線
        if (Physics.Raycast(sensorStartPos, forward, out RaycastHit hit, sensorLength))
        {
            if (!hit.collider.CompareTag("Player") && !hit.collider.CompareTag("Terrain")) // 忽略玩家和地板(如果有Tag)
            {
                Debug.DrawLine(sensorStartPos, hit.point, Color.red);
                hitSomething = true;
                // 如果撞到正前方，隨機或根據法線決定轉向
                if (hit.normal.x < 0) avoidMultiplier = -1; else avoidMultiplier = 1;
            }
        }

        // 2. 右側射線
        sensorStartPos += right * sideSensorPos;
        if (Physics.Raycast(sensorStartPos, forward, out hit, sensorLength))
        {
            if (!hit.collider.CompareTag("Player"))
            {
                Debug.DrawLine(sensorStartPos, hit.point, Color.red);
                hitSomething = true;
                avoidMultiplier -= 1f; // 向左閃避
            }
        }

        // 3. 左側射線
        sensorStartPos -= right * (sideSensorPos * 2);
        if (Physics.Raycast(sensorStartPos, forward, out hit, sensorLength))
        {
            if (!hit.collider.CompareTag("Player"))
            {
                Debug.DrawLine(sensorStartPos, hit.point, Color.red);
                hitSomething = true;
                avoidMultiplier += 1f; // 向右閃避
            }
        }

        // 4. 斜向射線 (增強轉角判定)
        if (Physics.Raycast(transform.position + transform.up * 0.5f, Quaternion.AngleAxis(30, transform.up) * forward, out hit, sensorLength * 0.7f))
        {
             if (!hit.collider.CompareTag("Player")) { hitSomething = true; avoidMultiplier -= 0.5f; }
        }
        if (Physics.Raycast(transform.position + transform.up * 0.5f, Quaternion.AngleAxis(-30, transform.up) * forward, out hit, sensorLength * 0.7f))
        {
             if (!hit.collider.CompareTag("Player")) { hitSomething = true; avoidMultiplier += 0.5f; }
        }

        if (hitSomething)
        {
            avoidVal = avoidMultiplier * avoidSpeed;
        }

        return hitSomething;
    }

    // 檢查是否卡住 (如果一直踩油門但速度很慢)
    void CheckIfStuck()
    {
        if (currentSpeed_H < 5f && !isReversing)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > stuckCheckTime)
            {
                StartCoroutine(ReverseRoutine());
            }
        }
        else
        {
            stuckTimer = 0;
        }
    }

    IEnumerator ReverseRoutine()
    {
        isReversing = true;
        stuckTimer = 0;
        yield return new WaitForSeconds(reverseTime);
        isReversing = false;
    }

    //──────────────────────────────────────────────
    // 物理運算 (保留原版邏輯，稍微精簡)
    //──────────────────────────────────────────────
    void AddDownForceAndDrag()
    {
        // 空氣阻力
        float airDrag = airDragCoefficient * currentSpeed_S * currentSpeed_S;
        rb.AddForce(-rb.linearVelocity.normalized * airDrag, ForceMode.Force);

        // 下壓力
        float downforce = downforceCoefficient * currentSpeed_S * currentSpeed_S;
        rb.AddForce(-transform.up * downforce, ForceMode.Force);

        // 自動引擎阻力
        if (motorInput == 0f && brakeInput == 0f && rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            rb.AddForce(-rb.linearVelocity.normalized * autoBrakeForce, ForceMode.Force);
        }
    }

    void HandleSteering()
    {
        float steerAngle = maxSteerAngle * steerInput;
        frontLeftWheel.steerAngle = steerAngle;
        frontRightWheel.steerAngle = steerAngle;
    }

    void ApplyDriveTorque()
    {
        float torqueCurveFactor = engineTorqueCurve.Evaluate(currentRPM);
        float currentEngineTorque = engineMaxTorque * torqueCurveFactor * motorInput;
        float totalTorque = currentEngineTorque * gearRatios[currentGear] * finalDriveRatio;

        // 簡單分配到後輪 (RWD) 或其他模式
        if (driveType == SimpleCarController.DriveType.RWD)
        {
            rearLeftWheel.motorTorque = totalTorque / 2f;
            rearRightWheel.motorTorque = totalTorque / 2f;
        }
        else if (driveType == SimpleCarController.DriveType.FWD)
        {
            frontLeftWheel.motorTorque = totalTorque / 2f;
            frontRightWheel.motorTorque = totalTorque / 2f;
        }
        else // AWD
        {
            frontLeftWheel.motorTorque = totalTorque / 4f;
            frontRightWheel.motorTorque = totalTorque / 4f;
            rearLeftWheel.motorTorque = totalTorque / 4f;
            rearRightWheel.motorTorque = totalTorque / 4f;
        }
    }

    void ApplyBrakes()
    {
        float t = brakeForce * brakeInput;
        frontLeftWheel.brakeTorque = t;
        frontRightWheel.brakeTorque = t;
        rearLeftWheel.brakeTorque = t;
        rearRightWheel.brakeTorque = t;
    }

    void UpdateGearAndRPM()
    {
        // 簡化的 RPM 模擬
        float wheelRPM = (rearLeftWheel.rpm + rearRightWheel.rpm) / 2f;
        targetRPM = idleRPM + Mathf.Abs(wheelRPM) * gearRatios[currentGear] * finalDriveRatio;
        targetRPM = Mathf.Clamp(targetRPM, idleRPM, maxRPM);
        currentRPM = Mathf.Lerp(currentRPM, targetRPM, Time.deltaTime * rpmSmoothSpeed);

        // 自動換檔
        if (currentRPM > maxRPM * 0.9f && currentGear < gearRatios.Length - 1) currentGear++;
        if (currentRPM < maxRPM * 0.4f && currentGear > 0) currentGear--;
    }

    void SetupWheelFriction(WheelCollider wheel)
    {
        // 保持原設定
        WheelFrictionCurve f = wheel.forwardFriction;
        f.stiffness = 2.0f; // AI 可以稍微滑一點沒關係
        wheel.forwardFriction = f;

        WheelFrictionCurve s = wheel.sidewaysFriction;
        s.stiffness = 2.5f;
        wheel.sidewaysFriction = s;
    }

    void UpdateWheelPose(WheelCollider collider, Transform mesh)
    {
        if (mesh == null) return;
        collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.SetPositionAndRotation(pos, rot);
    }

    // 畫出感測器射線，方便在 Scene 視窗除錯
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 start = transform.position + Vector3.up * 0.5f;
        Gizmos.DrawLine(start, start + transform.forward * sensorLength);
        Gizmos.DrawLine(start + transform.right * sideSensorPos, start + transform.right * sideSensorPos + transform.forward * sensorLength);
        Gizmos.DrawLine(start - transform.right * sideSensorPos, start - transform.right * sideSensorPos + transform.forward * sensorLength);
    }
}