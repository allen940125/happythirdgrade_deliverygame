using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu]// 屬性可以從 Unity 的 "Create" 選單中創建這個檔案

public class ReelStrip : ScriptableObject
{
    //陣列，可以在檢視面板中編輯 [25]
    public List<SlotSymbol> symbols = new List<SlotSymbol>();
}
