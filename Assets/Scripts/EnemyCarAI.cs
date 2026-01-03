using UnityEngine;
// 不需要 UnityEngine.AI 了

public class SmartEnemyCarController : BaseCarController
{
    [Header("🎯 追蹤設定")]
    public Transform target;          // 追蹤目標

    [Header("👀 感測器設定")]
    public float sensorLength = 10f;
    public float sensorAngle = 30f;
    public float avoidSensitivity = 1.0f; // 避障靈敏度

    [Header("🏎️ 駕駛行為")]
    public float cautiousMaxSpeed = 80f; // 過彎時的速限
    [Range(0, 1)] public float corneringSlowdown = 0.6f; // 轉彎時放開油門的程度

    [Header("🆘 防卡死系統")]
    public float stuckCheckVelocity = 2f; // 低於此速度視為卡住
    public float stuckTimeThreshold = 2f; // 持續幾秒視為卡住
    public float reverseDuration = 2f;    // 倒車持續時間

    // 內部變數
    private float stuckTimer;
    private bool isReversing;
    private float reverseTimer;

    protected override void Start()
    {
        base.Start();
        // 移除了 NavMeshAgent 的設定
    }

    private void Update()
    {
        if (target == null) return;

        // 1. 判斷是否卡住 (需要倒車)
        CheckStuckStatus();

        float finalSteer = 0f;
        float finalMotor = 0f;
        float finalBrake = 0f;

        if (isReversing)
        {
            // --- 倒車模式 (卡住時觸發) ---
            finalMotor = -1f; // 全力倒車
            
            // 倒車時反向打方向盤，比較容易脫困
            // 判斷目標在左還是在右，決定倒車轉向
            Vector3 relativeVector = transform.InverseTransformPoint(target.position);
            finalSteer = (relativeVector.x > 0) ? -1f : 1f; 
            
            reverseTimer -= Time.deltaTime;
            if (reverseTimer <= 0)
            {
                isReversing = false;
                stuckTimer = 0f;
            }
        }
        else
        {
            // --- 前進追擊模式 ---
            
            // A. 計算目標方向 (Steering)
            // 直接計算目標相對於車頭的角度
            Vector3 vectorToTarget = transform.InverseTransformPoint(target.position);
            float steerToTarget = (vectorToTarget.x / vectorToTarget.magnitude);

            // B. 計算避障修正 (Avoidance)
            float avoidVal = RunSensors();
            
            // 邏輯：如果有障礙物，優先聽感測器的；如果前面沒路障，就聽追蹤目標的
            if (Mathf.Abs(avoidVal) > 0.1f)
            {
                // 發現障礙物，覆蓋轉向
                finalSteer = avoidVal;
            }
            else
            {
                // 前方空曠，直追目標
                finalSteer = steerToTarget;
            }

            // C. 智慧油門控制 (過彎減速)
            float cornerFactor = Mathf.Clamp01(1.0f - Mathf.Abs(finalSteer));
            
            // 判斷是否需要急煞車 (轉向大 + 速度快)
            bool isSharpTurn = Mathf.Abs(finalSteer) > 0.6f;
            
            if (isSharpTurn && CurrentSpeedKmH > cautiousMaxSpeed)
            {
                finalMotor = 0.1f; // 含一點點油門維持動力
                finalBrake = 0.5f; // 點煞車
            }
            else
            {
                // 直線加速，轉彎減速
                finalMotor = Mathf.Lerp(corneringSlowdown, 1f, cornerFactor);
                finalBrake = 0f;
            }
        }

        // 3. 傳給父類別 (BaseCarController) 執行物理移動
        SetInputs(finalSteer, finalMotor, finalBrake);
    }

    // ──────────────────────────────────────────────
    // 多角度感測器 (Raycast Array) - 保持不變
    // ──────────────────────────────────────────────
    float RunSensors()
    {
        float avoid = 0f;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 fwd = transform.forward;
        Vector3 right = transform.right;

        // 射線：中、右、左、右斜、左斜
        bool hitCenter = CastRay(origin, fwd, sensorLength);
        bool hitRight  = CastRay(origin, fwd + right * 0.5f, sensorLength * 0.8f);
        bool hitLeft   = CastRay(origin, fwd - right * 0.5f, sensorLength * 0.8f);
        bool hitRightAngled = CastRay(origin, Quaternion.Euler(0, sensorAngle, 0) * fwd, sensorLength * 0.6f);
        bool hitLeftAngled  = CastRay(origin, Quaternion.Euler(0, -sensorAngle, 0) * fwd, sensorLength * 0.6f);

        // 避障權重
        if (hitRight || hitRightAngled) avoid -= 0.6f; // 右邊有東西，往左閃
        if (hitLeft || hitLeftAngled)   avoid += 0.6f; // 左邊有東西，往右閃
        
        // 正前方有東西
        if (hitCenter)
        {
            if (hitRight && !hitLeft) avoid = -1f;      // 左空往左
            else if (hitLeft && !hitRight) avoid = 1f;  // 右空往右
            else avoid = (UnityEngine.Random.value > 0.5f) ? 1f : -1f; // 都堵住隨機閃
        }

        return avoid * avoidSensitivity;
    }

    bool CastRay(Vector3 pos, Vector3 dir, float len)
    {
        if (Physics.Raycast(pos, dir, out RaycastHit hit, len))
        {
            if (!hit.collider.CompareTag("Player") && !hit.collider.CompareTag("Environment")) 
            {
                Debug.DrawLine(pos, hit.point, Color.red);
                return true;
            }
        }
        // Debug.DrawLine(pos, pos + dir * len, Color.green);
        return false;
    }

    // ──────────────────────────────────────────────
    // 防卡死邏輯
    // ──────────────────────────────────────────────
    void CheckStuckStatus()
    {
        if (!isReversing && CurrentSpeedKmH < stuckCheckVelocity)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > stuckTimeThreshold)
            {
                isReversing = true;
                reverseTimer = reverseDuration;
                // Debug.Log("卡住了，倒車中...");
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }
}