// DeliveryAnchor.cs (Core/Delivery/DeliveryAnchor.cs)
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 挂载在房子的安全子物件上，用于静态地收集所有可用的送货世界坐标。
/// 这是一个轻量级的注册器，不需要单例。
/// </summary>
public class DeliveryAnchor : MonoBehaviour
{
    // 静态列表：存储所有可作为目标的安全世界坐标
    public static List<Vector3> AllSafeDeliveryPositions = new List<Vector3>();

    private void Awake()
    {
        // 【静态注册】: 在游戏启动时，将自己的安全位置添加到列表中
        AllSafeDeliveryPositions.Add(transform.position);
    }
    
    private void OnDestroy()
    {
        // 确保在运行时被销毁时，从列表中移除
        AllSafeDeliveryPositions.Remove(transform.position);
    }
    
    // 你可以在这里添加 Editor 脚本来可视化这个点位
}