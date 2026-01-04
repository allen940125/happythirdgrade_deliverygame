using UnityEngine;

public class CarStatsManager : MonoBehaviour
{
    private InventoryRuntimeData CurrentInventory => SaveManager.Instance.CurrentSaveData.InventoryData;
    
    [Header("數據來源")]
    public CarDataSO carData; // 把上面做的 SO 拉進來

    [Header("當前狀態 (存檔會存這個)")]
    public int currentLevel = 1;

    private BaseCarController controller;
    private BaseCarHealth carHealth;

    void Awake()
    {
        controller = GetComponent<BaseCarController>();
        carHealth = GetComponent<BaseCarHealth>();

        if (gameObject.name == "Player")
        {
            GameManager.Instance.CarStatsManager = this;
        }
    }

    void Start()
    {
        currentLevel = GameManager.Instance.currentLevel;
        UpdateCarStats();
    }

    // 每次升級或換裝備時呼叫這個
    public void UpdateCarStats()
    {
        if (carData == null || controller == null) return;

        // 1. 計算數值 = 基礎值 * 成長倍率(查曲線)
        float growthMultiplier = carData.torqueGrowth.Evaluate(currentLevel);
        float finalTorque = carData.baseTorque * growthMultiplier;

        float brakeMultiplier = carData.brakeGrowth.Evaluate(currentLevel);
        float finalBrake = carData.baseBrake * brakeMultiplier;

        // 2. 判斷特殊功能解鎖
        // 假設 SO 裡設定 Level 5 解鎖 AWD，如果沒達到就用預設的 FWD
        DriveType driveMode = (currentLevel >= carData.awdUnlockLevel) ? DriveType.AWD : DriveType.FWD;
        
        SteeringType steerMode = (currentLevel >= carData.fourWsUnlockLevel) ? SteeringType.FourWheel : SteeringType.FrontOnly;

        // 3. 把算好的最終結果塞給車子
        // (注意：你的 BaseCarController 要有 InitializeStats 方法)
        controller.InitializeStats(
            finalTorque,
            controller.maxSteerAngle, // 假設轉向角度不升級
            driveMode,
            steerMode,
            controller.airDragCoefficient,
            controller.downforceCoefficient
        );
        
        // 2. >> 計算血量數值 (新增的) <<
        float hpGrowth = carData.healthGrowth.Evaluate(currentLevel);
        float finalHP = carData.baseMaxHealth * hpGrowth;

        if (carHealth != null)
        {
            // 把算好的血量塞進去
            carHealth.InitializeHealth(finalHP);
        }

        Debug.Log($"車輛數據更新: Lv.{currentLevel} | 扭力: {finalTorque}");
    }

    // 測試用：升級按鈕
    [ContextMenu("Level Up")]
    public void LevelUp()
    {
        GameManager.Instance.currentLevel++;
        currentLevel = GameManager.Instance.currentLevel;
        UpdateCarStats();
    }
}

[System.Serializable]
public class CarRuntimeData
{
    // 用來對應 SO 的名稱或 ID，這樣讀檔時才知道是哪台車
    public string CarID; 
    
    // 這是存檔的核心：玩家練到幾等了
    public int CurrentLevel = 1;

    // (選填) 如果以後有經驗值條，可以加在這裡
    // public float CurrentExp; 
}