using UnityEngine;
using System.Collections.Generic;

public class SlotMachineController : MonoBehaviour
{
    public int reelCount = 3; // 老虎機捲軸數量（列數）
    public int rowCount = 3; // 每個捲軸的行數
    public GameObject cellPrefab; // 用於顯示符號的預製件
    public float cellSpacing = 1.5f; // 格子間距

    private SpriteRenderer[,] cells; // 用於存儲格子對象的二維數組
    void Start()
    {
        cells= new SpriteRenderer[reelCount, rowCount];
    }

   
}
