using System.Collections.Generic;
using UnityEngine;

namespace MRTemplateAssets.Scripts
{
    /// <summary>
    /// Represents a single Lego block type with its metadata
    /// </summary>
    [System.Serializable]
    public class BlockData
    {
        public string blockName;
        public string blockId;
        public GameObject prefab;
        public Sprite icon;
        public BlockCategory category;
        public Vector3 defaultScale = Vector3.one;

        // Color variants available for this block
        public List<Color> availableColors = new List<Color>();
    }

    /// <summary>
    /// Categories for organizing blocks in tabs
    /// </summary>
    public enum BlockCategory
    {
        Bricks,
        Plates,
        Slopes,
        Special,
        Technic
    }

    /// <summary>
    /// ScriptableObject that holds the complete catalog of available Lego blocks
    /// </summary>
    [CreateAssetMenu(fileName = "BlockCatalog", menuName = "VR Lego/Block Catalog Data")]
    public class BlockCatalogData : ScriptableObject
    {
        [Header("Block Definitions")]
        public List<BlockData> allBlocks = new List<BlockData>();

        [Header("Default Colors")]
        public List<Color> defaultColors = new List<Color>
        {
            Color.red,
            Color.blue,
            Color.yellow,
            Color.green,
            Color.white,
            Color.black
        };

        /// <summary>
        /// Get all blocks in a specific category
        /// </summary>
        public List<BlockData> GetBlocksByCategory(BlockCategory category)
        {
            var blocks = allBlocks.FindAll(block => block.category == category);
            Debug.Log($"[BlockCatalog] GetBlocksByCategory({category}): Found {blocks.Count} blocks");
            foreach (var block in blocks)
            {
                Debug.Log($"  - {block.blockName} (ID: {block.blockId})");
            }
            return blocks;
        }

        /// <summary>
        /// Get a block by its ID
        /// </summary>
        public BlockData GetBlockById(string blockId)
        {
            return allBlocks.Find(block => block.blockId == blockId);
        }
    }
}
