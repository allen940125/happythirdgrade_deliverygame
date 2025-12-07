using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SlotReelController : MonoBehaviour
{
    [Header("格子池")]
    public List<GameObject> cellPool = new List<GameObject>();
    public List<SpriteRenderer> symbolRenderers = new List<SpriteRenderer>();

    [Header("設定")]
    public int visibleRows = 3;
    public int bufferRows = 2;
    public float cellTotalHeight = 110f; // 請確認這裡的數值是否符合你的 Inspector 設定 (看起來可能是 5 或 7.5?)

    [Header("旋轉設定")]
    public int minSpinSteps = 20;
    public int maxSpinSteps = 40;

    [Header("平滑移動設定")]
    [Range(0.01f, 0.2f)]
    public float smoothDuration = 0.05f;

    // 內部變數
    private float centerCellY;
    private float FirstCellY;
    private float LastCellY;
    private bool isInitialized = false;
    private bool isSpinning = false;


    // === 初始化 ===
    public void Initialize()
    {
        if (isInitialized) return;

        int totalCells = cellPool.Count;
        if (totalCells == 0) return;

        // 根據你的 LOG，這裡會抓到 0 和 -15
        FirstCellY = cellPool[0].transform.localPosition.y;
        LastCellY = cellPool[totalCells - 1].transform.localPosition.y;

        // 計算中心Cell的Y位置
        int centerIndex = bufferRows + (visibleRows / 2);
        if (centerIndex < totalCells)
        {
            centerCellY = cellPool[centerIndex].transform.localPosition.y;
        }

        isInitialized = true;
        Debug.Log($"[{gameObject.name}] Init: Top={FirstCellY}, Bottom={LastCellY}");
    }

    // === 開始旋轉 ===
    public void StartSpin(AnimationCurve speedCurve, float spinDuration)
    {
        if (!isInitialized) Initialize();
        if (isSpinning) return;

        StartCoroutine(SpinCoroutine(speedCurve));
    }

    // === 旋轉協程 ===
    IEnumerator SpinCoroutine(AnimationCurve speedCurve)
    {
        isSpinning = true;
        int totalSteps = Random.Range(minSpinSteps, maxSpinSteps);

        // 旋轉前先強制歸位 (消除上次可能殘留的誤差)
        SnapToGrid();

        for (int step = 0; step < totalSteps; step++)
        {
            float progress = (float)step / totalSteps;
            float speedMultiplier = speedCurve.Evaluate(progress);
            float currentSmoothDuration = smoothDuration / Mathf.Max(speedMultiplier, 0.1f);

            // 移動一格
            yield return StartCoroutine(SmoothMoveOneStep(currentSmoothDuration));

            // 檢查循環 (正常運行時靠這裡循環)
            CheckLoop();
        }

        // 正常跑完所有步數後停止
        StopSpin();
    }

    // === 平滑移動一格 ===
    IEnumerator SmoothMoveOneStep(float duration)
    {
        List<float> startPositions = new List<float>();
        List<float> targetPositions = new List<float>();

        foreach (var cell in cellPool)
        {
            float startY = cell.transform.localPosition.y;
            startPositions.Add(startY);

            // 計算目標：當前位置往下移動一格
            float rawTarget = startY - cellTotalHeight;

            // 【修正 Bug 1: 小數點漂移】
            // 這裡做一個保護，確保目標位置是對齊網格的
            // 假設你的格子高度是固定的，我們計算它應該在哪個網格點上
            // 這樣可以防止 -15.0001 變成 -15.0002
            float fixedTarget = Mathf.Round(rawTarget / cellTotalHeight) * cellTotalHeight;
            // 如果上述公式導致位置錯誤(因為你的座標可能不是倍數)，請改用下面的簡單版：
            // float fixedTarget = rawTarget; 

            targetPositions.Add(fixedTarget);
        }

        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            for (int i = 0; i < cellPool.Count; i++)
            {
                Vector3 pos = cellPool[i].transform.localPosition;
                pos.y = Mathf.Lerp(startPositions[i], targetPositions[i], t);
                cellPool[i].transform.localPosition = pos;
            }
            yield return null;
        }

        // 確保精確到達
        for (int i = 0; i < cellPool.Count; i++)
        {
            Vector3 pos = cellPool[i].transform.localPosition;
            pos.y = targetPositions[i];
            cellPool[i].transform.localPosition = pos;
        }
    }

    // === 檢查循環 ===
    void CheckLoop()
    {
        foreach (var cell in cellPool)
        {
            Vector3 pos = cell.transform.localPosition;

            // 【邏輯關鍵】
            // 當前位置如果比 LastCellY 還要低 (考量一點點誤差容許值)
            // 例如 -16.551 < -15，就會觸發
            if (pos.y < LastCellY - (cellTotalHeight * 0.5f))
            {
                // 把它送回最上面 (0)
                pos.y = FirstCellY;
                cell.transform.localPosition = pos;
            }
        }
    }

    // === 強制對齊網格 (修正浮點數) ===
    void SnapToGrid()
    {
        foreach (var cell in cellPool)
        {
            Vector3 pos = cell.transform.localPosition;

            // 根據你的 cellTotalHeight 強制對齊最近的刻度
            // 例如 -16.551 會被拉回 -15 (如果單位是5) 或 -17.5
            // 這裡用 Round 是最保險的清除小數點方式
            if (cellTotalHeight > 0)
            {
                pos.y = Mathf.Round(pos.y / cellTotalHeight) * cellTotalHeight;
            }

            cell.transform.localPosition = pos;
        }
    }

    // === 停止旋轉 (修正 Bug 2 的核心) ===
    public void StopSpin()
    {
        // 1. 殺死所有正在跑的動畫
        StopAllCoroutines();

        // 2. 【關鍵修正】因為動畫被殺死，格子可能正好停在 -16.551
        // 所以必須在這裡「手動」再執行一次檢查，把那些出界的格子抓回來！
        CheckLoop();

        // 3. 【關鍵修正】消除 -16.xxx 這種不乾淨的數字，強制對齊網格
        SnapToGrid();

        // 4. 最後整體微調
        FinalAlign();

        isSpinning = false;
    }

    void FinalAlign()
    {
        GameObject closestCell = null;
        float minDistance = float.MaxValue;

        foreach (var cell in cellPool)
        {
            float distance = Mathf.Abs(cell.transform.localPosition.y - centerCellY);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestCell = cell;
            }
        }

        if (closestCell != null)
        {
            float offset = centerCellY - closestCell.transform.localPosition.y;
            // 只有當偏移量合理時才移動，避免瞬間跳動太大
            if (Mathf.Abs(offset) < cellTotalHeight)
            {
                transform.localPosition += new Vector3(0, offset, 0);
            }
            Debug.Log($"[{gameObject.name}] 對齊完成, Offset: {offset}");
        }
    }
}