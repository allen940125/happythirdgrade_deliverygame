// DeliveryManager.cs (Game/Delivery/DeliveryManager.cs)

using System;
using UnityEngine;
using Gamemanager;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

public class DeliveryManager : SessionSingleton<DeliveryManager>
{
    [Header("配置")]
    [SerializeField] private GameObject DeliveryTargetPrefab; // 拖拽 DeliveryTarget Prefab 到这里
    [SerializeField] private float MinReward = 100f;
    [SerializeField] private float MaxReward = 300f;

    [SerializeField] private DeliveryOrder _currentOrder; 
    [SerializeField] private GameObject _currentTargetInstance; // 追踪当前生成的物件实例

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        // 游戏开始后自动开启第一张订单
        StartNewOrder(); 
    }

    // =================================================================
    // 核心流程
    // =================================================================

    public void StartNewOrder()
    {
        List<Vector3> safePositions = DeliveryAnchor.AllSafeDeliveryPositions;

        if (DeliveryTargetPrefab == null)
        {
            Debug.LogError("DeliveryManager 缺少目标 Prefab。");
            return;
        }

        if (safePositions == null || safePositions.Count == 0)
        {
            Debug.LogError("没有可用的安全锚点，请检查场景中是否有 DeliveryAnchor 脚本。");
            return;
        }
        
        // 1. 随机选取一个位置
        int index = Random.Range(0, safePositions.Count);
        Vector3 randomPos = safePositions[index]; 
        int reward = CalculateReward(randomPos); 

        // 2. 创建订单数据
        _currentOrder = new DeliveryOrder(randomPos, reward);
        
        // 3. 实例化 Prefab
        _currentTargetInstance = Instantiate(
            DeliveryTargetPrefab, 
            randomPos, 
            Quaternion.identity 
        );
        
        Debug.Log($"[DeliveryManager] 新订单生成，目标点：{randomPos}，奖励：{reward}G");
        
        // 4. 【關鍵修改】：通知 WaypointManager 追蹤新的目標
        if (WaypointManager.Instance != null)
        {
            // 將實例化的目標物件的 Transform 傳遞給 WaypointManager
            WaypointManager.Instance.SetTarget(_currentTargetInstance.transform); 
        }
        
        // 4. 通知 Waypoint/UI 系统显示新的目标指示
        // WaypointRenderer.Instance.TrackTarget(_currentTargetInstance.transform); 
    }

    /// <summary>
    /// 由 DeliveryTarget 触发器调用，通知玩家已抵达目标位置。
    /// </summary>
    public void OnPlayerArrivedAtTarget(Vector3 arrivedPosition)
    {
        if (_currentOrder == null) return;
        
        // 由于 DeliveryTarget 的触发器已经确保了玩家在范围内，我们只需判断是否是当前目标
        // 如果需要更严格的判断，可以对比位置是否完全相同
        // if (arrivedPosition == _currentOrder.targetPosition) 
        
        FinishOrder();
    }
    
    private void FinishOrder()
    {
        if (_currentOrder == null) return;
        
        int reward = _currentOrder.rewardAmount;

        // 1. 【发钱】(调用 PlayerWallet)
        // ⚠️ 假设 PlayerWallet 是单例
        // PlayerWallet.Instance.AddMoney(reward); 

        GameScoreManager.Instance.AddMoney(800);
        
        // 2. 【发送事件】(通知任务管理器和成就管理器)
        // ⚠️ 假设 MoneyChangedEvent 已经在 AddMoney 中触发
        GameManager.Instance.MainGameEvent.Send(new DeliverySuccessfulEvent
        {
            MoneyGained = reward,
            // currentTotalMoney 需要从 PlayerWallet 处获取
        });

        // 3. 清理並啟動下一個訂單
        _currentOrder = null;
        // ⚠️ _currentTargetInstance 應該在 DeliveryTarget 腳本中自行銷毀
        // 但在銷毀前，我們先通知 WaypointManager 移除指標。
        if (WaypointManager.Instance != null)
        {
            // 傳遞 null，代表移除當前目標
            WaypointManager.Instance.SetTarget(null); 
        }
        _currentTargetInstance = null; 
        
        StartNewOrder();
    }
    
    // ----------------------------------------------------------------
    // 辅助方法
    // ----------------------------------------------------------------
    
    private int CalculateReward(Vector3 pos)
    {
        // 简单的随机奖励
        return (int)Random.Range(MinReward, MaxReward); 
    }
}

// 订单数据结构 (DeliveryOrder.cs，可独立成文件)
public class DeliveryOrder
{
    public Vector3 targetPosition { get; private set; }
    public int rewardAmount { get; private set; }

    public DeliveryOrder(Vector3 pos, int reward)
    {
        targetPosition = pos;
        rewardAmount = reward;
    }
}