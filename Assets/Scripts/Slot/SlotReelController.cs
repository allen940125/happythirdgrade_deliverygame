using UnityEngine;
using System.Collections.Generic;

public class SlotReelController : MonoBehaviour
{
    [Header("格子池")]
    public List<GameObject> cellPool = new List<GameObject>(); // 所有格子
    public List<SpriteRenderer> symbolRenderers = new List<SpriteRenderer>(); // 格子內的符號渲染器

    [Header("設定")]
    public int visibleRows = 3; // Mask顯示幾格
    public int bufferRows = 2; // 上下緩衝
    public float cellTotalHeight = 110f; // 格子總高度(cellHeight + cellSpacing)

    [Header("對齊設定")]
    private float centerAlignY = 0f; // 中心對齊點的Y座標
    private List<float> initialPositions = new List<float>(); // 記錄每個格子的初始Y位置

    [Header("旋轉狀態")]
    private bool isSpinning = false;
    private float currentSpeed = 0f;

    // 內部變數
    private int totalCells; // 總格子數
    private float spinProgress = 0f; // 旋轉進度 (0~1)
    private AnimationCurve speedCurve; // 速度曲線

    // === 初始化(由SlotSetting建立完成後呼叫) ===
    public void Initialize()
    {
        totalCells = cellPool.Count;

        // 記錄每個格子的初始位置
        initialPositions.Clear();
        foreach (var cell in cellPool)
        {
            initialPositions.Add(cell.transform.localPosition.y);
        }

        // 抓取中間格子的位置當作對齊中心點
        int centerIndex = visibleRows / 2;
        if (totalCells > centerIndex)
        {
            centerAlignY = cellPool[centerIndex].transform.localPosition.y;
            Debug.Log($"{gameObject.name} 中心對齊點: Y={centerAlignY}, 總格數:{totalCells}");
        }
    }

    // === 開始旋轉 ===
    public void StartSpin(AnimationCurve curve, float duration)
    {
        isSpinning = true;
        spinProgress = 0f;
        speedCurve = curve;
        currentSpeed = 0f;
    }

    // === 停止旋轉 ===
    public void StopSpin(int targetCellIndex)
    {
        isSpinning = false;

        // TODO: 之後加入平滑停止動畫
        // 暫時直接停止
        Debug.Log($"{gameObject.name} 停止在格子 {targetCellIndex}");
    }

    void Update()
    {
        if (isSpinning)
        {
            SpinUpdate();
        }
    }

    // === 旋轉更新 ===
    void SpinUpdate()
    {
        // 根據曲線計算當前速度
        float curveSpeed = speedCurve.Evaluate(spinProgress);
        currentSpeed = curveSpeed * cellTotalHeight * 3f; // 速度倍率

        // 移動每個格子
        for (int i = 0; i < cellPool.Count; i++)
        {
            GameObject cell = cellPool[i];
            Vector3 pos = cell.transform.localPosition;

            // 向下移動
            pos.y -= currentSpeed * Time.deltaTime;

            // === 循環重置邏輯 ===
            // 計算這個格子相對於初始位置移動了多遠
            float distanceMoved = initialPositions[i] - pos.y;

            // 如果移動距離超過一整輪(totalCells * cellTotalHeight)
            // 就重置到上方
            if (distanceMoved >= totalCells * cellTotalHeight)
            {
                pos.y += totalCells * cellTotalHeight;

                // TODO: 重置時隨機更換符號
                Debug.Log($"{gameObject.name} 格子 {i} 循環重置");
            }

            cell.transform.localPosition = pos;
        }

        // 更新進度
        spinProgress += Time.deltaTime * 0.3f;
        if (spinProgress > 1f) spinProgress = 1f;
    }

    // === 檢查是否正在旋轉 ===
    public bool IsSpinning()
    {
        return isSpinning;
    }
}