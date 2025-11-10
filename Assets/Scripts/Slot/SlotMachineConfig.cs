using Ink.Parsed;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SlotMachineConfig", menuName = "Data/SlotMachineConfig")]
public class SlotMachineConfig : ScriptableObject
{
    public List<SlotData> slotSprites = new List<SlotData>();
}

[Serializable]
public class SlotData
{
    public Sprite sprite;
    public float weight;
}
