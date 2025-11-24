using UnityEngine;
using System.Collections.Generic;

public class SlotSymbolSelector : MonoBehaviour
{
    [Header("符號資料")]
    public SlotSymbolData symbolData; // 指向符號資料的ScriptableObject資源

    [Header("轉盤格子資料")]
    public SlotSetting slotSetting; // 指向SlotSetting腳本以取得轉盤設定

    //[Header("指定圖片測試")]
    //public KeyCode randomizeKey = KeyCode.Space; // 按下此鍵隨機選擇符號
    //void Update()
    //{
    //    if (Input.GetKeyDown(randomizeKey))
    //    {
    //        RandomizeAllSymbols();
    //    }
    //}
    [ContextMenu("隨機抽選符號")]
    public void RandomizeAllSymbols()
    {
        List<SlotSetting.ReelColumn> allReels = slotSetting.GetReels();

        if (allReels == null || allReels.Count == 0)
        {
            Debug.LogWarning("無轉盤可指定圖片 確認SlotSetting已正常運作");
            return;
        }
        foreach (var reel in allReels)
        {
            foreach (var symbolRenderer in reel.symbolRenderers)
            {
                // 隨機抽一個符號
                SlotSymbolData.SymbolInfo selectedSymbol = GetRandomSymbol();

                // 放到格子上
                symbolRenderer.sprite = selectedSymbol.symbolSprite;
            }
        }
        Debug.Log("符號已隨機抽選完成!");
    }
    // === 根據權重隨機抽選一個符號 ===

    SlotSymbolData.SymbolInfo GetRandomSymbol()
    {
        // 檢查資料是否正確
        if (symbolData == null || symbolData.symbols.Count == 0)
        {
            Debug.LogError("符號資料庫是空的!");
            return null;
        }

        // 計算總權重
        int totalWeight = 0;
        foreach (var symbol in symbolData.symbols)
        {
            totalWeight += symbol.weight;
        }

        // 隨機一個數字
        int randomValue = Random.Range(0, totalWeight);

        // 根據權重選擇符號
        int currentWeight = 0;
        foreach (var symbol in symbolData.symbols)
        {
            currentWeight += symbol.weight;
            if (randomValue < currentWeight)
            {
                return symbol;
            }
        }

        // 保險(理論上不會執行到這裡)
        return symbolData.symbols[0];
    }
}
