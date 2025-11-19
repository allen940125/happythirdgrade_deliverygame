using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlotMachineControllerTest : MonoBehaviour
{
    [Header("拉霸設定")]
    public int reelCount = 3;       // 轉輪數量 (列數)
    public int visibleRows = 3;     // 每列可見的格子數量 (行數)
    public float reelSpacing = 2.0f;   // 轉輪之間的水平間距
    public float symbolSpacing = 1.1f; // 格子間的垂直間距
    public SlotSymbolData symbolTable; // 獎項圖案資料清單（包含機率）

    private List<Transform> reelParents;           // 每個轉輪的父物件
    private List<List<SpriteRenderer>> symbolRenderers; // 每個轉輪內各格子的 SpriteRenderer

    void Start()
    {
        // 初始化轉輪及格子
        reelParents = new List<Transform>();
        symbolRenderers = new List<List<SpriteRenderer>>();
        CreateReels(reelCount);
    }

    // 建立指定數量的轉輪，每個轉輪內建立可見Rows個格子
    void CreateReels(int count)
    {
        for (int r = 0; r < count; r++)
        {
            // 建立轉輪父物件
            GameObject reel = new GameObject("Reel" + r);
            reel.transform.parent = this.transform;
            reelParents.Add(reel.transform);

            // 為該轉輪建立 visibleRows 個格子
            List<SpriteRenderer> srList = new List<SpriteRenderer>();
            for (int i = 0; i < visibleRows; i++)
            {
                GameObject symbolObj = new GameObject("Symbol_" + r + "_" + i);
                symbolObj.transform.parent = reel.transform;
                SpriteRenderer sr = symbolObj.AddComponent<SpriteRenderer>();
                srList.Add(sr);
            }
            symbolRenderers.Add(srList);
        }
        // 重新定位所有轉輪與格子位置
        PositionReelsAndSymbols();
    }

    // 重定位轉輪與其內所有格子的位置（保持對稱）
    void PositionReelsAndSymbols()
    {
        // 依間距排列轉輪父物件
        for (int r = 0; r < reelParents.Count; r++)
        {
            float x = r * reelSpacing;
            reelParents[r].localPosition = new Vector3(x, 0, 0);
        }
        // 依對稱原理定位每個格子
        for (int r = 0; r < reelParents.Count; r++)
        {
            int rowCount = symbolRenderers[r].Count;
            int midIndex = rowCount / 2;
            for (int i = 0; i < rowCount; i++)
            {
                float y = (i - midIndex) * symbolSpacing;
                symbolRenderers[r][i].transform.localPosition = new Vector3(0, y, 0);
            }
        }
    }

    // 旋轉按鈕觸發函式：開始旋轉
    public void Spin()
    {
        StartCoroutine(SpinCoroutine());
    }

    // 旋轉模擬 Coroutine：隨機變換圖案，最後停在選中的結果
    IEnumerator SpinCoroutine()
    {
        // 先將所有圖案顏色恢復白色
        for (int r = 0; r < reelParents.Count; r++)
            for (int i = 0; i < symbolRenderers[r].Count; i++)
                symbolRenderers[r][i].color = Color.white;

        // 模擬轉動一段時間 (例如2秒)：持續以隨機符號更新以製造轉動感
        float spinDuration = 2.0f;
        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            foreach (var srList in symbolRenderers)
            {
                foreach (var sr in srList)
                {
                    SlotSymbolData data = GetRandomSymbolData();
                    if (data != null)
                        sr.sprite = data.symbolSprite;
                }
            }
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        // 停轉時為每個轉輪決定一個中獎圖案並置於中間，其餘格子隨機圖案
        int midIndex = visibleRows / 2;
        for (int r = 0; r < reelParents.Count; r++)
        {
            // 取得隨機的結果圖案
            SlotSymbolData resultData = GetRandomSymbolData();
            if (resultData != null)
                symbolRenderers[r][midIndex].sprite = resultData.symbolSprite;

            // 其餘格子（非中間列）也指定隨機圖案
            for (int i = 0; i < symbolRenderers[r].Count; i++)
            {
                if (i == midIndex) continue;
                SlotSymbolData other = GetRandomSymbolData();
                if (other != null)
                    symbolRenderers[r][i].sprite = other.symbolName;
            }
        }

        // 停轉後檢查中獎
        CheckResults();
    }

    // 增加一個整體轉輪 (在 Inspector 的ContextMenu可執行)
    [ContextMenu("Add Reel")]
    public void AddReel()
    {
        reelCount++;
        GameObject reel = new GameObject("Reel" + reelParents.Count);
        reel.transform.parent = this.transform;
        reelParents.Add(reel.transform);

        List<SpriteRenderer> srList = new List<SpriteRenderer>();
        for (int i = 0; i < visibleRows; i++)
        {
            GameObject symbolObj = new GameObject("Symbol_" + (reelParents.Count - 1) + "_" + i);
            symbolObj.transform.parent = reel.transform;
            SpriteRenderer sr = symbolObj.AddComponent<SpriteRenderer>();
            srList.Add(sr);
        }
        symbolRenderers.Add(srList);
        PositionReelsAndSymbols();
    }

    // 為每個轉輪新增一個格子 (同理可由ContextMenu調用)
    [ContextMenu("Add Symbol")]
    public void AddSymbolToReels()
    {
        visibleRows++;
        for (int r = 0; r < reelParents.Count; r++)
        {
            GameObject symbolObj = new GameObject("Symbol_" + r + "_" + (symbolRenderers[r].Count));
            symbolObj.transform.parent = reelParents[r];
            SpriteRenderer sr = symbolObj.AddComponent<SpriteRenderer>();
            symbolRenderers[r].Add(sr);
        }
        PositionReelsAndSymbols();
    }

    // 依照權重隨機選取一個圖案資料
    SlotSymbolData GetRandomSymbolData()
    {
        if (symbolTable == null || symbolTable.Count == 0) return null;
        int totalWeight = 0;
        foreach (var s in symbolTable)
            totalWeight += s.weight;
        int rand = Random.Range(0, totalWeight);
        foreach (var s in symbolTable)
        {
            if (rand < s.weight)
                return s;
            rand -= s.weight;
        }
        return symbolTable[symbolTable.Count - 1];
    }

    // 檢查每一行是否符號一致 (若是，視為中獎並標示綠色)
    void CheckResults()
    {
        for (int i = 0; i < visibleRows; i++)
        {
            Sprite firstSprite = symbolRenderers[0][i].sprite;
            bool allMatch = true;
            for (int r = 1; r < reelParents.Count; r++)
            {
                if (symbolRenderers[r][i].sprite != firstSprite)
                {
                    allMatch = false;
                    break;
                }
            }
            if (allMatch && firstSprite != null)
            {
                // 標示整行中獎格子為綠色
                for (int r = 0; r < reelParents.Count; r++)
                {
                    symbolRenderers[r][i].color = Color.green;
                }
                Debug.Log("Row " + i + " WIN!");
            }
        }
    }
}
