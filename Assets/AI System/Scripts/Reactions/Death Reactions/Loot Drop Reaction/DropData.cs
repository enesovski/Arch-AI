using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Archura
{
    [MovedFrom(true, sourceNamespace: "Artifika.AI", sourceAssembly: "Assembly-CSharp")]
    [System.Serializable]
    public class DropData
    {
        [TableColumnWidth(0, Width = 220)]
        [LabelText("Item")]
        [Tooltip("Host item asset. Loot is resolved by the host's own death reaction.")]
        public ScriptableObject item;

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
}
