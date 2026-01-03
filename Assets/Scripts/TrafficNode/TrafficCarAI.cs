using UnityEngine;

public class TrafficCarAI : BaseCarController
{
    [Header("📍 導航設定")]
    public TrafficNode currentNode;
    public float waypointThreshold = 5f;

    [Header("👀 感測器設定 (雙射線)")]
    public float sensorLength = 10f;       // 看多遠 (煞車距離)
    public float sensorWidth = 0.8f;       // 左右兩根射線的寬度 (車寬的一半)
    public float reverseDistance = 2.5f;   // 離障礙物剩 2.5米 就倒車

    [Header("⚙️ 簡單速限")]
    public float speedLimit = 50f;         // 最高時速
    public float motorPower = 0.5f;        // 平常踩油門的力道 (0~1)

    private float reverseTimer = 0f;       // 倒車稍微持續一下，不要抽搐

    private void Update()
    {
        if (currentNode == null) return;

        // 1. 導航：判斷是否到達路點
        if (Vector3.Distance(transform.position, currentNode.transform.position) < waypointThreshold)
        {
            PickNextNode();
        }

        // 2. 轉向計算 (永遠追著點跑)
        Vector3 relativeVector = transform.InverseTransformPoint(currentNode.transform.position);
        float steer = (relativeVector.x / relativeVector.magnitude);
        
        float motor = 0f;
        float brake = 0f;

        // 3. 感測器邏輯
        // 取得最近障礙物的距離，如果沒看到東西會回傳 -1
        float obstacleDist = GetObstacleDistance();

        // 如果正在倒車計時中 (避免車子前後一直抖動)
        if (reverseTimer > 0)
        {
            reverseTimer -= Time.deltaTime;
            motor = -1f; // 繼續倒車
            brake = 0f;
            steer = -steer; // 倒車時反向打輪，容易脫困
        }
        else if (obstacleDist != -1f) // 有看到東西！
        {
            if (obstacleDist < reverseDistance)
            {
                // A. 距離太近了 -> 觸發倒車
                reverseTimer = 1.5f; // 倒車持續 1.5 秒
                motor = -1f;
            }
            else
            {
                // B. 在偵測範圍內 -> 停車
                motor = 0f;
                brake = 1f; // 踩死煞車
            }
        }
        else // 前方沒東西
        {
            // C. 正常行駛
            // 如果沒超速，就踩油門；超速就放油門
            if (CurrentSpeedKmH < speedLimit)
            {
                motor = motorPower;
                brake = 0f;
            }
            else
            {
                motor = 0f; // 放油門滑行
                // 如果超速太多 (下坡)，稍微點一點煞車
                if (CurrentSpeedKmH > speedLimit + 5f) brake = 0.2f;
            }
        }

        SetInputs(steer, motor, brake);
    }

    // ──────────────────────────────────────────────
    // 雙射線感測器
    // ──────────────────────────────────────────────
    float GetObstacleDistance()
    {
        Vector3 origin = transform.position + Vector3.up * 1.0f; // 抬高 1 米
        Vector3 fwd = transform.forward;
        Vector3 right = transform.right;

        // 定義兩根射線的起點：左邊一根、右邊一根
        Vector3 leftPos = origin - right * sensorWidth;
        Vector3 rightPos = origin + right * sensorWidth;

        float dist = -1f;

        // 發射左射線
        if (CastRay(leftPos, fwd, out float leftDist))
        {
            dist = leftDist;
        }

        // 發射右射線
        if (CastRay(rightPos, fwd, out float rightDist))
        {
            // 如果左邊沒打到，或者右邊打到的距離更近，就以右邊為準
            if (dist == -1f || rightDist < dist)
            {
                dist = rightDist;
            }
        }

        return dist;
    }

    // 輔助函式：發射單根射線
    bool CastRay(Vector3 pos, Vector3 dir, out float hitDist)
    {
        hitDist = -1f;
        
        // 畫線除錯：綠色=安全
        Color debugColor = Color.green;

        if (Physics.Raycast(pos, dir, out RaycastHit hit, sensorLength))
        {
            // 過濾掉自己
            if (hit.transform.root == this.transform) 
            {
                Debug.DrawRay(pos, dir * sensorLength, debugColor);
                return false;
            }

            // 打到東西了！變紅色
            debugColor = Color.red;
            Debug.DrawLine(pos, hit.point, debugColor);
            
            hitDist = hit.distance;
            return true;
        }

        Debug.DrawRay(pos, dir * sensorLength, debugColor);
        return false;
    }

    void PickNextNode()
    {
        if (currentNode.nextNodes.Count > 0)
        {
            currentNode = currentNode.nextNodes[Random.Range(0, currentNode.nextNodes.Count)];
        }
    }
}