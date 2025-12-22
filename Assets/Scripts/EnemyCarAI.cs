using UnityEngine;

public class EnemyCarController : SimpleCarController
{
    [Header("AI 設定")]
    public Transform target;
    public float sensorLength = 10f;

    private void Update()
    {
        if (target == null) return;

        // 1. 跑感測器邏輯
        float aiSteer = 0f;
        float aiMotor = 1f; // 預設油門踩死
        float aiBrake = 0f;

        bool obstacleDetected = RunSensors(out float avoidVal);

        if (obstacleDetected)
        {
            aiSteer = avoidVal;
        }
        else
        {
            // 追蹤邏輯
            Vector3 relativeVector = transform.InverseTransformPoint(target.position);
            aiSteer = (relativeVector.x / relativeVector.magnitude);
        }

        // 2. 防卡死與轉彎減速邏輯 (略)
        // ...

        // 3. 傳給父類別
        SetInputs(aiSteer, aiMotor, aiBrake);
    }

    bool RunSensors(out float avoidVal)
    {
        // 把之前的 Raycast 邏輯搬過來
        avoidVal = 0f;
        return false;
    }
}