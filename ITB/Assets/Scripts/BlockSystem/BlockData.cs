using UnityEngine;
using UnityEngine.UI;

namespace LegoBuilder.BlockSystem
{
    /// <summary>
    /// Defines a single LEGO block type with its properties
    /// </summary>
    [System.Serializable]
    public class BlockData
    {
        [Tooltip("Display name of the block")]
        public string blockName;

        [Tooltip("Prefab to spawn when this block is selected")]
        public GameObject blockPrefab;

        [Tooltip("Icon/sprite to show in the UI button (optional)")]
        public Sprite icon;

        [Tooltip("Color for the button background if no icon is provided")]
        public Color buttonColor = Color.white;
    }
}
