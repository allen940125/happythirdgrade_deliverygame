// DeliveryTarget.cs (Game/Delivery/DeliveryTarget.cs)
using UnityEngine;
using Gamemanager; 

/// <summary>
/// 负责侦测玩家进入触发区域，并通知 DeliveryManager 订单完成。
/// </summary>
public class DeliveryTarget : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 1. 检查 Tag 是否为 "Player"
        if (other.CompareTag("Player"))
        {
            // 2. 呼叫 DeliveryManager 确认订单
            if (DeliveryManager.Instance != null)
            {
                // 将玩家抵达的位置信息传递给管理器进行核对
                DeliveryManager.Instance.OnPlayerArrivedAtTarget(transform.position); 
            }
            else
            {
                Debug.LogError("[DeliveryTarget] DeliveryManager 实例未找到，无法完成订单。");
            }
            
            // 3. 销毁自身实例 (假设 DeliveryManager 已经处理了数据清理)
            // 立即销毁，避免玩家停留导致多次触发
            Destroy(gameObject); 
        }
    }
}