using NewInventorySystem;
using Sirenix.OdinInspector;
using UnityEngine;


[System.Serializable]
public struct DropData
{
    [HorizontalGroup("MainGroup", Width = 300)] 
    [BoxGroup("MainGroup/Item", ShowLabel = false)] 
    [HideLabel]
    public ItemEntry itemData;

    [HorizontalGroup("MainGroup", Width = 100)] 
    [BoxGroup("MainGroup/Chance", ShowLabel = false)] 
    [Range(0, 100)]
    [LabelText("Drop Chance")]
    public float dropChance;

}