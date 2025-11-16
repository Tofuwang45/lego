using UnityEngine;

namespace LegoBuilder.BlockSystem
{
    /// <summary>
    /// ScriptableObject that holds the catalog of available blocks
    /// </summary>
    [CreateAssetMenu(fileName = "BlockCatalog", menuName = "Lego Builder/Block Catalog", order = 1)]
    public class BlockCatalog : ScriptableObject
    {
        [Tooltip("List of available blocks (max 9 for 3x3 grid)")]
        [SerializeField]
        private BlockData[] blocks = new BlockData[9];

        /// <summary>
        /// Gets the array of blocks (max 9)
        /// </summary>
        public BlockData[] Blocks => blocks;

        /// <summary>
        /// Gets the number of blocks in the catalog
        /// </summary>
        public int BlockCount => blocks != null ? blocks.Length : 0;

        /// <summary>
        /// Gets a block by index
        /// </summary>
        public BlockData GetBlock(int index)
        {
            if (blocks == null || index < 0 || index >= blocks.Length)
                return null;

            return blocks[index];
        }

        private void OnValidate()
        {
            // Ensure we don't have more than 9 blocks
            if (blocks != null && blocks.Length > 9)
            {
                Debug.LogWarning("BlockCatalog: Maximum of 9 blocks supported for 3x3 grid. Extra blocks will be ignored.");
                System.Array.Resize(ref blocks, 9);
            }
        }
    }
}
