using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SlotSpinManager : MonoBehaviour
{
    [Header("參考")]
    public SlotSetting slotSetting; // SlotSetting腳本

    [Header("旋轉設定")]
    [Tooltip("旋轉持續時間(秒)")]
    public float spinDuration = 2f;

    [Tooltip("每個轉盤停止的間隔時間(秒)")]
    public float stopDelay = 0.3f;

    [Header("速度曲線")]
    [Tooltip("控制加速和減速效果")]
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("狀態")]
    private bool isSpinning = false;
    private List<SlotReelController> reelControllers = new List<SlotReelController>();

    // === 初始化(在SlotSetting建立完成後呼叫) ===
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

        // 從 SlotSetting 的 ReelContainer 下尋找
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

    // === 開始旋轉(測試用) ===
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

        StartCoroutine(SpinCoroutine());
    }

    // === 旋轉協程 ===
    IEnumerator SpinCoroutine()
    {
        isSpinning = true;
        Debug.Log("開始旋轉!");

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
            // TODO: 這裡之後要接入 SymbolSelector 決定停在哪個符號
            // 暫時停在中間格
            int centerIndex = slotSetting.visibleRows / 2;

            reelControllers[i].StopSpin(centerIndex);
            Debug.Log($"轉盤 {i} 停止在格子 {centerIndex}");

            // 等待間隔再停下一個
            if (i < reelControllers.Count - 1)
            {
                yield return new WaitForSeconds(stopDelay);
            }
        }

        // 4. 等待所有轉盤完全停止
        yield return new WaitForSeconds(0.5f);

        isSpinning = false;
        Debug.Log("旋轉結束!");

        // TODO: 之後在這裡觸發兌獎檢查
    }

    // === 檢查是否正在旋轉 ===
    public bool IsSpinning()
    {
        return isSpinning;
    }
}
