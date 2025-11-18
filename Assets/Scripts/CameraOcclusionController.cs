using UnityEngine;
using System.Collections.Generic;
// System.Linq 已不再需要

public class CameraOcclusionController : MonoBehaviour
{
    [Header("目標設定")]
    [Tooltip("玩家角色的 Transform")]
    public Transform target; 
    [Tooltip("目標的 LayerMask (例如: Environment, Building)")]
    public LayerMask obstacleLayer;

    // *** 核心優化：使用 HashSet 提高查找效率 ***
    // (HashSet 的 Add/Remove/Contains 效能遠高於 List)
    private HashSet<FadeObject> currentObstacles = new HashSet<FadeObject>();
    private HashSet<FadeObject> previousObstacles = new HashSet<FadeObject>();

    // *** 核心優化：只管理 "正在" Fading 的物件 ***
    private HashSet<FadeObject> activeFaders = new HashSet<FadeObject>();
    
    // 緩存列表，避免在 Update 中 new List 造成 GC (垃圾回收)
    private List<FadeObject> fadersToRemove = new List<FadeObject>();

    // 在 LateUpdate 中 "偵測"
    void LateUpdate()
    {
        if (target == null) return;

        Vector3 start = transform.position;
        Vector3 end = target.position;
        Vector3 direction = end - start;
        float distance = direction.magnitude;

        // 清空當前幀的列表
        currentObstacles.Clear();
        
        // 1. 發射射線 (Raycast)
        RaycastHit[] hits = Physics.SphereCastAll(start, 0.3f, direction.normalized, distance, obstacleLayer);

        // 2. 處理當前偵測到的障礙物
        foreach (var hit in hits)
        {
            FadeObject fadeObject = hit.collider.GetComponentInParent<FadeObject>();
            if (fadeObject != null)
            {
                currentObstacles.Add(fadeObject);
            }
        }

        // 3. 找出 "新增" 的遮擋物
        foreach (var obstacle in currentObstacles)
        {
            // 如果 "當前" 列表有，但 "上一幀" 列表沒有 -> 這是新遮擋物
            if (!previousObstacles.Contains(obstacle))
            {
                obstacle.StartFade();
                activeFaders.Add(obstacle); // 加入到 "待處理" 列表
            }
        }

        // 4. 找出 "消失" 的遮擋物
        foreach (var obstacle in previousObstacles)
        {
            // 如果 "上一幀" 列表有，但 "當前" 列表沒有 -> 遮擋物移開了
            if (obstacle != null && !currentObstacles.Contains(obstacle))
            {
                obstacle.StopFade();
                activeFaders.Add(obstacle); // 加入到 "待處理" 列表
            }
        }
        
        // 5. 交換列表，準備下一幀
        // (我們交換 Set，而不是複製，這樣可以避免記憶體分配)
        var temp = previousObstacles;
        previousObstacles = currentObstacles;
        currentObstacles = temp;
    }

    // 在 Update 中 "執行 Fading"
    void Update()
    {
        // 如果沒有任何物件在 Fading，就直接返回
        if (activeFaders.Count == 0) return;

        fadersToRemove.Clear();
        float dt = Time.deltaTime;

        // *** 核心優化 ***
        // 只遍歷 "正在 Fading" 的物件
        // 從 651 次 Update 降到 K 次 (K = 正在變化的物件數)
        foreach (var fader in activeFaders)
        {
            // 呼叫 FadeObject 的更新函式
            // 如果 DoFadeUpdate 返回 false，表示它已完成 Fading
            if (!fader.DoFadeUpdate(dt))
            {
                // 標記為待移除 (不能在 foreach 中直接移除)
                fadersToRemove.Add(fader);
            }
        }

        // 移除所有已完成 Fading 的物件
        foreach (var fader in fadersToRemove)
        {
            activeFaders.Remove(fader);
        }
    }
}