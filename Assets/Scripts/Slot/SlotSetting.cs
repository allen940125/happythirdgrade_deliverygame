using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SlotSetting : MonoBehaviour
{
    [Header("轉盤設定")]
    [Tooltip("每個轉盤有幾格")]
    public int rowsPerReel = 3; // 每個轉盤顯示幾個符號(例如:3格)

    [Tooltip("有幾個轉盤(幾列)")]
    public int reelCount = 3; // 有幾個獨立的轉盤(例如:3列)

    [Header("間距設定")]
    [Tooltip("格子與格子之間的垂直間距")]
    public float cellSpacing = 10f; // 格子之間的間距

    [Tooltip("轉盤與轉盤之間的水平間距")]
    public float reelSpacing = 20f; // 轉盤之間的間距

    [Tooltip("美術圖大小")]
    public float cellHeight = 100f;

    [Header("旋轉設定")]
    [Tooltip("Mask顯示幾格(建議奇數)")]
    public int visibleRows = 3;

    [Tooltip("上下緩衝格數")]
    public int bufferRows = 2;

    [Header("美術資源")]
    public Sprite cellBackgroundSprite; // 格子的背景圖片(美術提供的單格圖片)

    [Header("UI參考")]
    public Transform reelContainer; // 放置所有轉盤的父物件
    public SlotSpinManager spinManager; // 旋轉管理器參考

    private List<ReelColumn> reels = new List<ReelColumn>(); // 儲存所有轉盤

    // === 單個轉盤的類別 ===
    public class ReelColumn // 改成public讓外部可以存取
    {
        public Transform reelTransform; // 這個轉盤的Transform
        public List<GameObject> cells = new List<GameObject>(); // 這個轉盤上的所有格子物件
        public List<SpriteRenderer> symbolRenderers = new List<SpriteRenderer>(); // 每格內的空SpriteRenderer(給之後放圖片用)
    }

    void Start()
    {
        // 根據設定計算實際需要的格子數
        rowsPerReel = visibleRows + (bufferRows * 2);

        CreateSlotMachine(); // 建立老虎機
    }

    // === 建立老虎機的所有轉盤 ===
    void CreateSlotMachine()
    {
        // 清空舊的(如果有的話)
        foreach (Transform child in reelContainer) // 尋找所有子物件
        {
            Destroy(child.gameObject);
        }
        reels.Clear(); // 清空轉盤列表List

        // 建立每一個轉盤
        for (int i = 0; i < reelCount; i++)
        {
            CreateReel(i);
        }

        Debug.Log($"老虎機建立完成! {reelCount}個轉盤,每個{rowsPerReel}格");

        // 建立完成後初始化旋轉管理器
        if (spinManager != null)
        {
            spinManager.InitializeReels();
        }
    }

    // === 建立單個轉盤 ===
    void CreateReel(int reelIndex)
    {
        ReelColumn newReel = new ReelColumn();

        // 建立轉盤容器(空物件)儲存格子用
        GameObject reelObj = new GameObject($"Reel_{reelIndex}");
        reelObj.transform.SetParent(reelContainer);
        reelObj.transform.localPosition = Vector3.zero;
        reelObj.transform.localScale = Vector3.one;
        newReel.reelTransform = reelObj.transform;

        // 加入 SlotReelController 組件
        SlotReelController controller = reelObj.AddComponent<SlotReelController>();
        controller.visibleRows = visibleRows;
        controller.bufferRows = bufferRows;
        controller.cellTotalHeight = cellHeight + cellSpacing;

        // 建立每一格
        for (int row = 0; row < rowsPerReel; row++)
        {
            // === 建立格子背景 ===
            GameObject cellObj = new GameObject($"Cell_{reelIndex}_{row}");
            cellObj.transform.SetParent(reelObj.transform);
            cellObj.transform.localScale = Vector3.one;

            // 設定格子的位置(垂直排列,考慮間距)
            float yPos = -row * (cellSpacing + cellHeight); // cellHeight是美術圖的高度
            cellObj.transform.localPosition = new Vector3(0, yPos, 0);

            // 加入格子背景的SpriteRenderer
            SpriteRenderer cellBg = cellObj.AddComponent<SpriteRenderer>();
            cellBg.sprite = cellBackgroundSprite;
            cellBg.sortingOrder = 1; // 背景層

            // === 建立空的符號物件(給之後放圖片用) ===
            GameObject symbolObj = new GameObject($"Symbol_{reelIndex}_{row}");
            symbolObj.transform.SetParent(cellObj.transform);
            symbolObj.transform.localPosition = Vector3.zero; // 置中在格子裡
            symbolObj.transform.localScale = Vector3.one;

            // 加入空的SpriteRenderer
            SpriteRenderer symbolRenderer = symbolObj.AddComponent<SpriteRenderer>();
            symbolRenderer.sprite = null; // 初始為空
            symbolRenderer.sortingOrder = 2; // 在背景之上

            // 儲存到轉盤資料中
            newReel.cells.Add(cellObj);
            newReel.symbolRenderers.Add(symbolRenderer);

            // 同時加到 Controller 的格子池
            controller.cellPool.Add(cellObj);
            controller.symbolRenderers.Add(symbolRenderer);
        }

        // 設定轉盤之間的水平間距
        reelObj.transform.localPosition = new Vector3(reelIndex * reelSpacing, 0, 0);

        reels.Add(newReel);
    }

    // === 提供給外部存取轉盤資料的方法 ===
    public List<ReelColumn> GetReels()
    {
        return reels;
    }

[ContextMenu("遮擋顯示外格子")]
    private void SetAllChildrenMask()
    {
        // 取得此物件底下所有 SpriteRenderer（包含子孫物件）
        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>(true);

        int count = 0;
        foreach (var sr in sprites)
        {
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            count++;
        }

        Debug.Log($"已設定 {count} 個 SpriteRenderer 為 VisibleInsideMask");
    }
    [ContextMenu("顯示顯示外格子")]
    private void SetAllChildrenToNone()
    {
        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>(true);

        foreach (var sr in sprites)
        {
            sr.maskInteraction = SpriteMaskInteraction.None;
        }

        Debug.Log("所有子物件已設定為 None");
    }


}

