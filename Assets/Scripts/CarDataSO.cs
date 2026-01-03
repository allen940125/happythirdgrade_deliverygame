using UnityEngine;

[CreateAssetMenu(fileName = "NewCarData", menuName = "Car/Car Data")]
public class CarDataSO : ScriptableObject
{
    [Header("基本資料")]
    public string carName;

    // CarDataSO.cs
    [Header("生存能力")]
    public float baseMaxHealth = 100f; // Level 1 的血量
    // 成長曲線 (例如：等級 1=1.0, 等級 10=3.0 代表血量變 3 倍)
    public AnimationCurve healthGrowth = AnimationCurve.Linear(1, 1f, 10, 3f);
    
    [Header("基礎數值 (Level 1 的狀態)")]
    public float baseTorque = 600f;      // 基礎扭力
    public float baseMaxSpeed = 200f;    // 基礎極速(示意用)
    public float baseBrake = 8000f;      // 基礎煞車

    [Header("成長曲線 (X軸=等級, Y軸=倍率)")]
    // 這裡設定：等級 1 時 Y=1.0，等級 10 時 Y=2.0 (代表變強兩倍)
    public AnimationCurve torqueGrowth = AnimationCurve.Linear(1, 1f, 10, 2f);
    
    // 如果你不希望煞車隨等級變強，這條線畫成平的就好
    public AnimationCurve brakeGrowth = AnimationCurve.Constant(1, 10, 1f);

    [Header("特殊配置 (解鎖條件)")]
    // 比如等級 5 才能變四驅
    public int awdUnlockLevel = 5; 
    public int fourWsUnlockLevel = 10;
}