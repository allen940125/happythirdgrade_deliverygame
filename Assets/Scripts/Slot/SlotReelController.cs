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

    [Header("旋轉狀態")]
    private bool isSpinning = false;
    private float currentSpeed = 0f;
    private float targetStopPosition = 0f;

    // 內部變數
    private int totalCells; // 總格子數
    private float spinProgress = 0f; // 旋轉進度 (0~1)
    private AnimationCurve speedCurve; // 速度曲線(從SpinManager傳入)

    void Start()
    {
        totalCells = visibleRows + (bufferRows * 2);
        InitializeCellPositions();
    }

    // === 初始化格子位置 ===
    void InitializeCellPositions()
    {
        int centerIndex = visibleRows / 2; // 中心格索引

        // 設定每個格子的初始位置
        for (int i = 0; i < cellPool.Count; i++)
        {
            // 計算相對於中心的偏移
            int offsetFromCenter = i - centerIndex;
            float yPos = -offsetFromCenter * cellTotalHeight;

            cellPool[i].transform.localPosition = new Vector3(0, yPos, 0);
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

    // === 停止旋轉(對齊到目標位置) ===
    public void StopSpin(int targetCellIndex)
    {
        isSpinning = false;

        // 計算目標格子應該對齊的位置
        int centerIndex = visibleRows / 2;
        int offsetFromCenter = targetCellIndex - centerIndex;
        targetStopPosition = -offsetFromCenter * cellTotalHeight;

        // TODO: 這裡之後加入平滑停止動畫
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
        currentSpeed = curveSpeed * cellTotalHeight * 5f; // 基礎速度倍率

        // 移動所有格子
        foreach (var cell in cellPool)
        {
            Vector3 pos = cell.transform.localPosition;
            pos.y -= currentSpeed * Time.deltaTime;

            // 循環重置:如果格子移出下方,重置到上方
            float bottomBound = -(visibleRows / 2 + bufferRows + 1) * cellTotalHeight;
            if (pos.y < bottomBound)
            {
                pos.y += totalCells * cellTotalHeight;

                // TODO: 重置時隨機更換符號(之後接入SymbolSelector)
            }

            cell.transform.localPosition = pos;
        }

        spinProgress += Time.deltaTime * 0.5f; // 進度增加速度
        if (spinProgress > 1f) spinProgress = 1f;
    }

    // === 取得當前顯示的格子索引 ===
    public List<int> GetVisibleCellIndices()
    {
        List<int> indices = new List<int>();
        int centerIndex = visibleRows / 2;

        for (int i = 0; i < visibleRows; i++)
        {
            indices.Add(centerIndex - (visibleRows / 2) + i);
        }

        return indices;
    }

    // === 檢查是否正在旋轉 ===
    public bool IsSpinning()
    {
        return isSpinning;
    }
}