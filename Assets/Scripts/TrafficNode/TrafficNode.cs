using UnityEngine;
using System.Collections.Generic;

public class TrafficNode : MonoBehaviour
{
    [Header("下一個可能的目標點")]
    // 如果是直路，List 裡只有一個點
    // 如果是十字路口，List 裡會有三個點 (直走、左轉、右轉)
    public List<TrafficNode> nextNodes;

    [Header("速限 (可選)")]
    public float speedLimit = 50f; // 這段路的建議車速

    // 畫線方便編輯
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.5f);
        
        if (nextNodes != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var node in nextNodes)
            {
                if(node != null)
                    Gizmos.DrawLine(transform.position, node.transform.position);
            }
        }
    }
}