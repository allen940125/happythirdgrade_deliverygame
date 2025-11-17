using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CameraOcclusionController : MonoBehaviour
{
    [Header("目標設定")]
    [Tooltip("玩家角色的 Transform")]
    public Transform target; 
    [Tooltip("目標的 LayerMask (例如: Environment, Building)")]
    public LayerMask obstacleLayer;

    private List<FadeObject> currentObstacles = new List<FadeObject>();
    private List<FadeObject> previousObstacles = new List<FadeObject>();

    void LateUpdate()
    {
        // 確保有目標且相機與目標之間有距離
        if (target == null) return;

        Vector3 start = transform.position;
        Vector3 end = target.position;
        Vector3 direction = end - start;
        float distance = direction.magnitude;

        currentObstacles.Clear();
        
        // 1. 發射射線 (Raycast)
        // 使用 SphereCastAll 可以偵測到略微偏離中心的遮擋物
        RaycastHit[] hits = Physics.SphereCastAll(start, 0.3f, direction.normalized, distance, obstacleLayer);

        // 2. 處理當前偵測到的障礙物
        foreach (var hit in hits)
        {
            FadeObject fadeObject = hit.collider.GetComponentInParent<FadeObject>();
            if (fadeObject != null && !currentObstacles.Contains(fadeObject))
            {
                // 找到新的遮擋物，開始淡化
                fadeObject.StartFade();
                currentObstacles.Add(fadeObject);
            }
        }

        // 3. 處理已消失的障礙物 (需要恢復不透明)
        // 遍歷上一幀的列表，看哪些物件已經不在 currentObstacles 裡
        foreach (var obstacle in previousObstacles)
        {
            if (obstacle != null && !currentObstacles.Contains(obstacle))
            {
                // 障礙物移開了，停止淡化 (恢復不透明)
                obstacle.StopFade();
            }
        }
        
        // 4. 更新列表以供下一幀比較
        previousObstacles = currentObstacles.ToList();
    }
}