// Game/Quests/QuestData.cs
using System;
using UnityEngine;

[Serializable]
public class QuestData
{
    public int questID;
    public string title;
    [TextArea] public string description;
    public QuestStatus status;

    public QuestType type; // 任务类型：例如赚取金钱，送货次数
    public int targetValue; // 目标值 (例如：500金币，5次送货)
    public int currentValue; // 当前进度 (例如：已赚300金币，已送3次货)

    public int rewardMoney; // 奖励金钱
    // public List<ItemData> rewardItems; // 如果有物品奖励，也可以在这里添加

    public bool IsCompleted => currentValue >= targetValue;

    public QuestData Clone() // 用于深拷贝，避免修改到原始配置
    {
        return new QuestData
        {
            questID = this.questID,
            title = this.title,
            description = this.description,
            status = QuestStatus.NotStarted, // 新局开始时重置状态
            type = this.type,
            targetValue = this.targetValue,
            currentValue = 0, // 新局开始时重置进度
            rewardMoney = this.rewardMoney
        };
    }
}

public enum QuestStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed
}

public enum QuestType
{
    EarnMoney,          // 赚取指定金额
    DeliverPackages,    // 送货指定次数
    SurviveTime,        // 生存指定时间
    // ... 可以根据你的游戏添加更多类型
}