using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SlotSymbolData", menuName = "SlotMachine/SymbolData")] //還有一個order參數可設定在選單中的排序數字越小越前面
public class SlotSymbolData : ScriptableObject //ScriptableObject 可存放資料資源的類別/容器
// 用於定義老虎機符號的資料(獎項符號圖形 權重)
{
    
    [System.Serializable] //標記此類可序列化(SymbolInfo)，以便在Unity編輯器中顯示
    public class SymbolInfo
    { 
        public Sprite symbolSprite; // 要顯示的圖形
        public int weight; // 機率權重 
    }
    public List<SymbolInfo> symbols;
}