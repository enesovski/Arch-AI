using Sirenix.OdinInspector;
using UnityEngine;


[System.Serializable]
public class DropData
{
    [TableColumnWidth(0, Width = 220)]
    [LabelText("Item")]
    public ItemSO item;

    [TableColumnWidth(1, Width = 130)]
    [LabelText("Drop Range")]
    [MinMaxSlider(1, 100, ShowFields = true)]
    public Vector2Int dropRange;

    [TableColumnWidth(2, Width = 100)]
    [Range(0, 100)]
    [LabelText("Drop Chance")]
    [SuffixLabel("%", true)]
    public float dropChance = 100f;
}
