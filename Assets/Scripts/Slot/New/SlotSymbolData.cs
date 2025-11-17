using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SlotSymbolData", menuName = "SlotMachine/SymbolData")]
public class SlotSymbolData : ScriptableObject
// 用於定義老虎機符號的資料(獎項符號圖形 權重)
{
    [System.Serializable]
    public class SymbolInfo
    {
        public string symbolName; // 例如 "Diamond", "Crown"
        public Sprite symbolSprite; // 要顯示的圖形
        public int Weight; // 機率權重 
    }
}