using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor; // 引入編輯器命名空間
#endif

public class SlotSpinManager : SessionSingleton<SlotSpinManager>
{
    [Header("拉霸機")]
    [SerializeField] GameObject slotMachineController;
    
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

    private void Update()
    {
        // 如果有按 R 鍵也可以觸發
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            PlaySlotMachine();
        }
    }

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

    public void PlaySlotMachine()
    {
        slotMachineController.SetActive(true);
        StartSpin();
    }
    
    // === 開始旋轉 ===
    // [ContextMenu("開始旋轉")] // 有了下面的按鈕，這個可以留著當備用，也可以拿掉
    private void StartSpin()
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

        // 設定慢動作 (0.1 倍速)
        GameSpeedManager.Instance.SetGameSpeed(0.1f);
        
        // 1. 所有轉盤開始旋轉
        foreach (var controller in reelControllers)
        {
            controller.StartSpin(speedCurve, spinDuration);
        }

        // 2. 等待旋轉時間 (★注意：這裡改成了 Realtime)
        // 這樣就算遊戲變慢，這兩秒還是現實生活中的兩秒
        yield return new WaitForSecondsRealtime(spinDuration);

        // 3. 依序停止每個轉盤 (左→中→右)
        for (int i = 0; i < reelControllers.Count; i++)
        {
            reelControllers[i].StopSpin();
            Debug.Log($"轉盤 {i} 停止");

            // 等待間隔再停下一個 (★注意：這裡也改成了 Realtime)
            if (i < reelControllers.Count - 1)
            {
                yield return new WaitForSecondsRealtime(stopDelay);
            }
        }

        // 4. 等待確保所有動畫結束 (★注意：這裡也改成了 Realtime)
        yield return new WaitForSecondsRealtime(0.5f);

        isSpinning = false;
        
        slotMachineController.SetActive(false);
        
        // 恢復正常速度
        GameSpeedManager.Instance.SetGameSpeed(1f);
        Debug.Log("=== 旋轉結束 ===");

        // TODO: 之後在這裡觸發兌獎檢查 (步驟5)
        int money = Random.Range(100, 999);
        GameScoreManager.Instance.AddMoney(money);
    }

    // === 檢查是否正在旋轉 ===
    public bool IsSpinning()
    {
        return isSpinning;
    }
}

// ──────────────────────────────────────────────────────────────────────
// ▼ 這裡就是新增的 Editor 程式碼，負責在 Inspector 畫出按鈕 ▼
// ──────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
[CustomEditor(typeof(SlotSpinManager))]
public class SlotSpinManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 1. 繪製原本的變數欄位 (public 變數)
        DrawDefaultInspector();

        // 取得原本的腳本
        SlotSpinManager script = (SlotSpinManager)target;

        GUILayout.Space(10); // 空行，讓版面好看一點

        // 2. 繪製按鈕
        if (GUILayout.Button("開始初始化 (Start Init)", GUILayout.Height(40)))
        {
            script.slotSetting.CreateSlotMachine();
            script.InitializeReels();
        }
        
        if (GUILayout.Button("開始旋轉 (Start Spin)", GUILayout.Height(40)))
        {
            // 檢查是否正在播放模式 (因為 Coroutine 只有在 Play Mode 才能跑)
            if (Application.isPlaying)
            {
                script.PlaySlotMachine();
            }
            else
            {
                Debug.LogWarning("⚠️ 請先按下 Play 執行遊戲，才能測試旋轉功能喔！(因為用到了 Coroutine)");
            }
        }
    }
}
#endif