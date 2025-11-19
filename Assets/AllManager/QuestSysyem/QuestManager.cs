using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Gamemanager;

// 全局成就管理器
public class PlayerAchievementManager : Singleton<PlayerAchievementManager>
{
    [Header("所有成就配置 (ScriptableObject)")]
    public List<AchievementData> allAchievementConfigs;

    private List<AchievementData> _playerAchievements = new List<AchievementData>();
    public IReadOnlyList<AchievementData> PlayerAchievements => _playerAchievements;

    // =============================================================
    // Unity 生命周期
    // =============================================================

    protected override void Awake()
    {
        base.Awake();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
    }

    // =============================================================
    // 存檔資料（由 SaveManager 調用）
    // =============================================================

    /// <summary>
    /// SaveManager 載入後會呼叫這個方法
    /// 將存檔與 ScriptableObject 配置合併
    /// </summary>
    public void LoadAchievements(List<AchievementData> loadedData)
    {
        _playerAchievements.Clear();

        foreach (var config in allAchievementConfigs)
        {
            var loaded = loadedData?.FirstOrDefault(a => a.achievementID == config.achievementID);

            if (loaded != null)
            {
                _playerAchievements.Add(loaded); // 使用存檔資料
            }
            else
            {
                _playerAchievements.Add(config.Clone()); // 新增成就 or 未存檔 → 用預設
            }
        }

        Debug.Log("[PlayerAchievementManager] 成就進度已載入並初始化。");

        CheckAllAchievementsProgress();
    }

    /// <summary>
    /// SaveManager 在存檔時會呼叫取得資料
    /// </summary>
    public List<AchievementData> GetSaveData()
    {
        return _playerAchievements.Select(a => a.Clone()).ToList();
    }

    // =============================================================
    // 事件系統
    // =============================================================

    private void SubscribeToEvents()
    {
        var gameEvent = GameManager.Instance.MainGameEvent;

        gameEvent.SetSubscribe(gameEvent.OnMoneyChangedEvent, OnMoneyChangedEvent);
        gameEvent.SetSubscribe(gameEvent.OnDeliverySuccessfulEvent, OnDeliverySuccessfulEvent);
    }

    private void UnsubscribeFromEvents()
    {
        var gameEvent = GameManager.Instance.MainGameEvent;

        gameEvent.Unsubscribe<MoneyChangedEvent>(OnMoneyChangedEvent);
        gameEvent.Unsubscribe<DeliverySuccessfulEvent>(OnDeliverySuccessfulEvent);
    }

    // =============================================================
    // 事件處理
    // =============================================================

    private void OnMoneyChangedEvent(MoneyChangedEvent cmd)
    {
        int totalMoneyEarned = cmd.CurrentTotalMoney; // 必須是跨局累積數字

        var achievementsToUpdate = _playerAchievements
            .Where(a => !a.isUnlocked && a.type == AchievementType.TotalMoneyEarned)
            .ToList();

        foreach (var achievement in achievementsToUpdate)
        {
            achievement.currentValue = totalMoneyEarned;

            if (achievement.IsCompleted)
                UnlockAchievement(achievement.achievementID);
        }
    }

    private void OnDeliverySuccessfulEvent(DeliverySuccessfulEvent eventData)
    {
        var achievementsToUpdate = _playerAchievements
            .Where(a => !a.isUnlocked && a.type == AchievementType.TotalPackagesDelivered)
            .ToList();

        foreach (var achievement in achievementsToUpdate)
        {
            achievement.currentValue++; // 若你有 PlayerStatsManager，可改為 eventData.totalDeliveries

            if (achievement.IsCompleted)
                UnlockAchievement(achievement.achievementID);
        }
    }

    // =============================================================
    // 成就操作
    // =============================================================

    private void CheckAllAchievementsProgress()
    {
        foreach (var achievement in _playerAchievements.Where(a => !a.isUnlocked))
        {
            if (achievement.IsCompleted)
            {
                UnlockAchievement(achievement.achievementID);
            }
        }
    }

    public bool IsAchievementUnlocked(int achievementID)
    {
        return _playerAchievements
            .FirstOrDefault(a => a.achievementID == achievementID)?
            .isUnlocked ?? false;
    }

    private void UnlockAchievement(int achievementID)
    {
        var achievement = _playerAchievements.Find(a => a.achievementID == achievementID);
        if (achievement == null || achievement.isUnlocked) return;

        achievement.isUnlocked = true;

        Debug.Log($"[PlayerAchievementManager] 成就解鎖！ -> {achievement.title}");

        GameManager.Instance.MainGameEvent.Send(new AchievementUnlockedEvent { AchievementID = achievementID });
        // 要求 SaveManager 存檔
        //SaveManager.Instance.RequestSave();
    }
}
