using System;
using System.Collections.Generic;
using System.Linq;
using Gamemanager;
using UnityEngine;

public class GameQuestManager : SessionSingleton<GameQuestManager>
{
    [Header("本局任务配置")]
    public QuestTestData RunQuestConfig;
    private List<QuestData> _currentRunQuests = new List<QuestData>();
    public IReadOnlyList<QuestData> CurrentRunQuests => _currentRunQuests;
    
    
    protected override void Awake()
    {
        base.Awake();
        SubscribeToEvents();
        InitializeRun(RunQuestConfig.testQuests);
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    // =====================================================================
    // 初始化任务
    // =====================================================================
    public void InitializeRun(List<QuestData> questsForThisRun)
    {
        _currentRunQuests.Clear();

        foreach (var config in questsForThisRun)
        {
            var q = config.Clone();
            q.status = QuestStatus.InProgress;
            _currentRunQuests.Add(q);

            Debug.Log($"[GameQuestManager] 任務初始化: {q.title}, ID:{q.questID}");
        }

        CheckAllQuestProgress();
    }

    // =====================================================================
    // 事件订阅 / 取消
    // =====================================================================
    private void SubscribeToEvents()
    {
        var GameEvent = GameManager.Instance.MainGameEvent;

        GameEvent.SetSubscribe(GameEvent.OnMoneyChangedEvent, OnMoneyChangedEvent);
        GameEvent.SetSubscribe(GameEvent.OnDeliverySuccessfulEvent, OnDeliverySuccessfulEvent);
    }

    private void UnsubscribeFromEvents()
    {
        var GameEvent = GameManager.Instance.MainGameEvent;

        GameEvent.Unsubscribe<MoneyChangedEvent>(OnMoneyChangedEvent);
        GameEvent.Unsubscribe<DeliverySuccessfulEvent>(OnDeliverySuccessfulEvent);
    }

    // =====================================================================
    // Event Handlers
    // =====================================================================
    private void OnMoneyChangedEvent(MoneyChangedEvent cmd)
    {
        int CurrentTotalMoney = cmd.CurrentTotalMoney;

        var quests = _currentRunQuests
            .Where(q => q.status == QuestStatus.InProgress && q.type == QuestType.EarnMoney)
            .ToList();

        foreach (var quest in quests)
        {
            quest.currentValue = CurrentTotalMoney;
            if (quest.IsCompleted)
                CompleteQuest(quest.questID);
        }
    }

    private void OnDeliverySuccessfulEvent(DeliverySuccessfulEvent cmd)
    {
        var quests = _currentRunQuests
            .Where(q => q.status == QuestStatus.InProgress && q.type == QuestType.DeliverPackages)
            .ToList();

        foreach (var quest in quests)
        {
            quest.currentValue++;
            if (quest.IsCompleted)
                CompleteQuest(quest.questID);
        }
    }

    // =====================================================================
    // 任務狀態操作
    // =====================================================================
    private void CheckAllQuestProgress()
    {
        foreach (var q in _currentRunQuests.Where(q => q.status == QuestStatus.InProgress))
        {
            if (q.IsCompleted)
                CompleteQuest(q.questID);
        }
    }

    public void CompleteQuest(int questID)
    {
        var quest = _currentRunQuests.Find(q => q.questID == questID);
        if (quest == null || quest.status != QuestStatus.InProgress)
            return;

        quest.status = QuestStatus.Completed;

        Debug.Log($"[GameQuestManager] 任務完成: {quest.title}");
        
        GameManager.Instance.MainGameEvent.Send(new GameQuestCompletedEvent
        {
            QuestID = questID
        });
    }
}
