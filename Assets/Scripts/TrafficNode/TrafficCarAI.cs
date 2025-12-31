using UnityEngine;

public class TrafficCarAI : BaseCarController
{
    [Header("📍 導航設定")]
    public TrafficNode currentNode;
    public float waypointThreshold = 5f;

    [Header("👀 散射感測器 (五向偵測)")]
    public float sensorLength = 10f;       // 看多遠
    public float sensorHeight = 0.6f;      // 射線高度 (0.6 比較接近保險桿)
    public float sensorAngle = 30f;        // 散射角度 (像鬍鬚一樣張開)
    public float sensorWidth = 0.8f;       // 左右平移寬度
    public float reverseDistance = 2.5f;   // 倒車觸發距離

    [Header("⚙️ 速度行為")]
    public float cruisingSpeed = 50f;
    public float panicSpeed = 120f;
    public float motorPower = 0.5f;

    [Header("😡 不耐煩設定")]
    public float maxPatience = 5f;
    private float patienceTimer = 0f;
    private bool isImpatient = false;

    // ─── 內部狀態 ───
    private float currentSpeedLimit;
    private bool isPanic = false;
    private float panicTimer = 0f;
    private float panicDuration = 5f;
    private float reverseTimer = 0f;

    protected override void Start()
    {
        base.Start();
        currentSpeedLimit = cruisingSpeed;
    }

    private void Update()
    {
        UpdatePanicState();
        if (currentNode == null) return;

        // 1. 導航
        if (Vector3.Distance(transform.position, currentNode.transform.position) < waypointThreshold)
        {
            PickNextNode();
        }

        // 2. 轉向
        Vector3 relativeVector = transform.InverseTransformPoint(currentNode.transform.position);
        float steer = (relativeVector.x / relativeVector.magnitude);
        
        float motor = 0f;
        float brake = 0f;

        // 3. >> 改良版散射感測器 <<
        float obstacleDist = GetScatteredObstacleDistance();

        // ─── 倒車狀態 ───
        if (reverseTimer > 0)
        {
            reverseTimer -= Time.deltaTime;
            motor = -1f;
            brake = 0f;
            steer = -steer;
            patienceTimer = 0f; 
            isImpatient = false; 
        }
        // ─── 前方有障礙物 ───
        else if (obstacleDist != -1f)
        {
            if (obstacleDist < reverseDistance)
            {
                reverseTimer = 1.5f; 
            }
            else
            {
                if (isImpatient)
                {
                    motor = 0.5f; 
                    brake = 0f;
                }
                else
                {
                    motor = 0f;
                    brake = 1f;
                    patienceTimer += Time.deltaTime;
                    if (patienceTimer > maxPatience)
                    {
                        isImpatient = true;
                    }
                }
            }
        }
        // ─── 前方沒東西 ───
        else
        {
            patienceTimer = 0f;
            isImpatient = false;

            if (CurrentSpeedKmH < currentSpeedLimit)
            {
                motor = isPanic ? 1f : motorPower;
                brake = 0f;
            }
            else
            {
                motor = 0f;
                if (CurrentSpeedKmH > currentSpeedLimit + 5f) brake = 0.2f;
            }
        }

        SetInputs(steer, motor, brake);
    }

    // ──────────────────────────────────────────────
    // 💥 驚嚇模式 (被撞觸發)
    // ──────────────────────────────────────────────
    protected override void OnCarCrash(CrashLevel level, float damageFactor, float impactForce, Vector3 hitNormal)
    {
        base.OnCarCrash(level, damageFactor, impactForce, hitNormal);
        if (level >= CrashLevel.Light) TriggerPanic();
    }
    void TriggerPanic() { isPanic = true; panicTimer = panicDuration; currentSpeedLimit = panicSpeed; isImpatient = true; }
    void UpdatePanicState() { if (isPanic) { panicTimer -= Time.deltaTime; if (panicTimer <= 0) { isPanic = false; currentSpeedLimit = cruisingSpeed; } } }
    void PickNextNode() { if (currentNode.nextNodes.Count > 0) currentNode = currentNode.nextNodes[Random.Range(0, currentNode.nextNodes.Count)]; }

    // ──────────────────────────────────────────────
    // 🌟 核心修改：散射感測器 (Scattered Raycasts)
    // ──────────────────────────────────────────────
    float GetScatteredObstacleDistance()
    {
        // 1. 設定原點 (高度降低)
        Vector3 origin = transform.position + Vector3.up * sensorHeight;
        Vector3 fwd = transform.forward;
        Vector3 right = transform.right;

        float minDistance = -1f;

        // 2. 定義 5 根射線的方向與起點
        // A. 正中央
        CheckRay(origin, fwd, ref minDistance);
        
        // B. 右邊平行 (平移)
        CheckRay(origin + right * sensorWidth, fwd, ref minDistance);
        
        // C. 左邊平行 (平移)
        CheckRay(origin - right * sensorWidth, fwd, ref minDistance);

        // D. 右斜射 (旋轉)
        Vector3 rightAngledDir = Quaternion.Euler(0, sensorAngle, 0) * fwd;
        CheckRay(origin, rightAngledDir, ref minDistance);

        // E. 左斜射 (旋轉)
        Vector3 leftAngledDir = Quaternion.Euler(0, -sensorAngle, 0) * fwd;
        CheckRay(origin, leftAngledDir, ref minDistance);

        return minDistance;
    }

    // 輔助函式：發射並更新最近距離
    void CheckRay(Vector3 pos, Vector3 dir, ref float currentMinDist)
    {
        // 射線長度 (斜向的稍微短一點，因為不需要看那麼遠)
        // 這裡是簡單邏輯，統一長度
        float len = sensorLength;

        Color debugColor = Color.green;

        if (Physics.Raycast(pos, dir, out RaycastHit hit, len))
        {
            // 忽略自己和路點
            if (hit.transform.root == this.transform || hit.collider.isTrigger) 
            {
                Debug.DrawRay(pos, dir * len, debugColor);
                return;
            }

            // 撞到了！
            debugColor = Color.red;
            Debug.DrawLine(pos, hit.point, debugColor);

            // 如果這是目前偵測到最近的障礙物，就更新距離
            if (currentMinDist == -1f || hit.distance < currentMinDist)
            {
                currentMinDist = hit.distance;
            }
        }
        else
        {
            Debug.DrawRay(pos, dir * len, debugColor);
        }
    }
}