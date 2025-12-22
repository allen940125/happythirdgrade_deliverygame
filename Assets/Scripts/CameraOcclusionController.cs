using UnityEngine;
using System.Collections.Generic;

public class CameraOcclusionController : MonoBehaviour
{
    [Header("目標設定")]
    public Transform target; 
    public LayerMask obstacleLayer;

    private HashSet<FadeObject> activeFaders = new HashSet<FadeObject>();
    private List<FadeObject> fadersToRemove = new List<FadeObject>();
    
    // 用來記錄上一幀擋住的物件，用 Set 方便比對
    private HashSet<FadeObject> previousObstacles = new HashSet<FadeObject>();
    
    // 預先配置 RaycastHit 陣列，避免每幀 new
    private RaycastHit[] hitBuffer = new RaycastHit[10]; 

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 start = transform.position;
        Vector3 direction = target.position - start;
        float distance = direction.magnitude;

        // 1. 使用 NonAlloc 版本，結果會填入 hitBuffer，回傳打到的數量
        int hitCount = Physics.SphereCastNonAlloc(start, 0.2f, direction.normalized, hitBuffer, distance, obstacleLayer);

        // 這次偵測到的物件集合
        HashSet<FadeObject> currentObstacles = new HashSet<FadeObject>();

        // 2. 整理當前遮擋物
        for (int i = 0; i < hitCount; i++)
        {
            // 這裡建議在 FadeObject 上掛一個簡單的 Component tag 或者直接 GetComponent
            // 如果層級很深，GetComponentInParent 還是有點小貴，最好是碰撞體就在 FadeObject 上
            FadeObject fo = hitBuffer[i].collider.GetComponent<FadeObject>();
            if (fo == null) fo = hitBuffer[i].collider.GetComponentInParent<FadeObject>();
            
            if (fo != null)
            {
                currentObstacles.Add(fo);
            }
        }

        // 3. 邏輯比對：誰是新來的？ (Current 有，Previous 沒有)
        foreach (var obj in currentObstacles)
        {
            if (!previousObstacles.Contains(obj))
            {
                obj.StartFade();
                activeFaders.Add(obj);
            }
        }

        // 4. 邏輯比對：誰離開了？ (Previous 有，Current 沒有)
        foreach (var obj in previousObstacles)
        {
            if (!currentObstacles.Contains(obj))
            {
                obj.StopFade();
                activeFaders.Add(obj); // 也要加回來讓它跑完 "淡出" 動畫
            }
        }

        // 5. 更新 Previous
        previousObstacles = currentObstacles;
    }

    void Update()
    {
        if (activeFaders.Count == 0) return;

        fadersToRemove.Clear();
        float dt = Time.deltaTime;

        foreach (var fader in activeFaders)
        {
            // 如果 DoFadeUpdate 回傳 false，代表動畫跑完了
            if (!fader.DoFadeUpdate(dt))
            {
                fadersToRemove.Add(fader);
            }
        }

        foreach (var item in fadersToRemove)
        {
            activeFaders.Remove(item);
        }
    }
}