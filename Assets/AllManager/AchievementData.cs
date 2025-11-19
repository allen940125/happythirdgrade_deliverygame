// Game/Achievements/AchievementData.cs
using System;
using UnityEngine;

[Serializable]
public class AchievementData
{
    public int achievementID;
    public string title;
    [TextArea] public string description;
    public bool isUnlocked; // 是否已解锁

    public AchievementType type; // 成就类型：例如累计赚取金钱，累计送货次数
    public int targetValue; // 目标值
    public int currentValue; // 当前累计进度 (会被保存)

    public int permanentRewardMoney; // 永久性奖励金钱
    // public List<PermanentUpgradeData> permanentRewards; // 例如解锁新的起始道具、永久属性加成

    public bool IsCompleted => currentValue >= targetValue;

    // 克隆方法，用于加载玩家存档数据时，或者在ScriptableObject中定义默认值时
    public AchievementData Clone()
    {
        return new AchievementData
        {
            achievementID = this.achievementID,
            title = this.title,
            description = this.description,
            isUnlocked = this.isUnlocked,
            type = this.type,
            targetValue = this.targetValue,
            currentValue = this.currentValue, // 成就的进度需要从存档加载
            permanentRewardMoney = this.permanentRewardMoney
        };
    }
}

public enum AchievementType
{
    TotalMoneyEarned,       // 累计赚取金钱
    TotalPackagesDelivered, // 累计送货次数
    TotalDeaths,            // 累计死亡次数
    // ... 更多全局成就类型
}