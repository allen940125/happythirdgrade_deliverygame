using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SlotSpinManager : MonoBehaviour
{
    [Header("參考")]
    public SlotSetting slotSetting;

    [Header("旋轉設定")]
    [Tooltip("旋轉持續時間(秒) - 第一個Reel開始到停止的時間")]
    public float spinDuration = 2f;

    [Tooltip("每個轉盤停止的間隔時間(秒)")]
    public float stopDelay = 0.3f;

    [Header("速度曲線")]
    [Tooltip("控制旋轉速度變化 (橫軸:進度0~1, 縱軸:速度倍率)")]
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0.5f, 1, 2f);

    // 內部變數
    private bool isSpinning = false;
    private List<SlotReelController> reelControllers = new List<SlotReelController>();

    // === 初始化 ===
    public void InitializeReels()
    {
        FindAllReelControllers();

        // 初始化每個轉盤控制器
        foreach (var controller in reelControllers)
        {
            controller.Initialize();
        }

        Debug.Log($"SlotSpinManager 初始化完成,找到 {reelControllers.Count} 個轉盤");
    }

    // === 尋找所有轉盤控制器 ===
    void FindAllReelControllers()
    {
        reelControllers.Clear();

        if (slotSetting != null && slotSetting.reelContainer != null)
        {
            foreach (Transform reelTransform in slotSetting.reelContainer)
            {
                SlotReelController controller = reelTransform.GetComponent<SlotReelController>();
                if (controller != null)
                {
                    reelControllers.Add(controller);
                }
            }
        }
    }

    // === 開始旋轉 ===
    [ContextMenu("開始旋轉")]
    public void StartSpin()
    {
        if (isSpinning)
        {
            Debug.LogWarning("已經在旋轉中!");
            return;
        }

        // 如果還沒初始化,先初始化
        if (reelControllers.Count == 0)
        {
            InitializeReels();
        }

        if (reelControllers.Count == 0)
        {
            Debug.LogError("沒有找到任何轉盤控制器!");
            return;
        }

        StartCoroutine(SpinSequence());
    }

    // === 旋轉序列 ===
    IEnumerator SpinSequence()
    {
        isSpinning = true;
        Debug.Log("=== 開始旋轉 ===");

        // 1. 所有轉盤開始旋轉
        foreach (var controller in reelControllers)
        {
            controller.StartSpin(speedCurve, spinDuration);
        }

        // 2. 等待旋轉時間
        yield return new WaitForSeconds(spinDuration);

        // 3. 依序停止每個轉盤 (左→中→右)
        for (int i = 0; i < reelControllers.Count; i++)
        {
            reelControllers[i].StopSpin();
            Debug.Log($"轉盤 {i} 停止");

            // 等待間隔再停下一個
            if (i < reelControllers.Count - 1)
            {
                yield return new WaitForSeconds(stopDelay);
            }
        }

        // 4. 等待確保所有動畫結束
        yield return new WaitForSeconds(0.5f);

        isSpinning = false;
        Debug.Log("=== 旋轉結束 ===");

        // TODO: 之後在這裡觸發兌獎檢查 (步驟5)
    }

    // === 檢查是否正在旋轉 ===
    public bool IsSpinning()
    {
        return isSpinning;
    }
}