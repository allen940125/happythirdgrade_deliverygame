using UnityEngine;
using System.Collections.Generic;

public class SlotMachineController : MonoBehaviour
{
    [Header("拉霸設定")]
    public int reelCount = 3; // 老虎機捲軸數量（列數）
    public int visibleRows = 3; // 每個捲軸可見的行數
    public float reelsSpacing = 2.0f; // 捲軸間距
    //public GameObject cellPrefab; // 用於顯示符號的預製件
    public float cellSpacing = 1.1f; // 格子間距
    public SlotSymbolData symbolData;

    private SpriteRenderer[,] cells; // 用於存儲格子對象的二維數組
    void Start()
    {
        cells = new SpriteRenderer[rowCount, reelCount];// 初始化二維數組
        for (int row = 0; row < rowCount; row++)//行數
        {
            for (int col=0; col<reelCount; col++)//軸數
            {
                GameObject cellObj = Instantiate(cellPrefab, transform); 
                cellObj.name=$"Cell_{row}_{col}"; // 命名格子物件以便識別
                cellObj.transform.localPosition = new Vector3(col * cellSpacing, -row * cellSpacing, 0); // 設置格子位置
                cells[row, col] = cellObj.GetComponent<SpriteRenderer>(); // 獲取並存儲SpriteRenderer組件
            }
        }
    }


}
