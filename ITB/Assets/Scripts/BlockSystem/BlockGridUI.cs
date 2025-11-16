using UnityEngine;
using UnityEngine.UI;

namespace LegoBuilder.BlockSystem
{
    /// <summary>
    /// Manages the 3x3 grid of block selection buttons
    /// </summary>
    public class BlockGridUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The block catalog to display")]
        [SerializeField]
        private BlockCatalog blockCatalog;

        [Tooltip("Grid layout group for the buttons")]
        [SerializeField]
        private GridLayoutGroup gridLayout;

        [Tooltip("Prefab for block buttons")]
        [SerializeField]
        private GameObject blockButtonPrefab;

        [Tooltip("Parent transform for button instances")]
        [SerializeField]
        private Transform buttonContainer;

        [Header("Grid Settings")]
        [Tooltip("Number of columns in the grid")]
        [SerializeField]
        private int columns = 3;

        [Tooltip("Cell size for grid")]
        [SerializeField]
        private Vector2 cellSize = new Vector2(100, 100);

        [Tooltip("Spacing between cells")]
        [SerializeField]
        private Vector2 spacing = new Vector2(10, 10);

        private BlockButton[] blockButtons;

        private void Start()
        {
            SetupGrid();
            PopulateGrid();
        }

        /// <summary>
        /// Sets up the grid layout
        /// </summary>
        private void SetupGrid()
        {
            if (gridLayout == null)
                gridLayout = GetComponentInChildren<GridLayoutGroup>();

            if (gridLayout != null)
            {
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = columns;
                gridLayout.cellSize = cellSize;
                gridLayout.spacing = spacing;
                gridLayout.childAlignment = TextAnchor.MiddleCenter;
            }

            if (buttonContainer == null)
                buttonContainer = gridLayout != null ? gridLayout.transform : transform;
        }

        /// <summary>
        /// Populates the grid with block buttons based on the catalog
        /// </summary>
        private void PopulateGrid()
        {
            // Clear existing buttons
            ClearGrid();

            if (blockCatalog == null)
            {
                Debug.LogError("[BlockGridUI] No BlockCatalog assigned!");
                return;
            }

            if (blockButtonPrefab == null)
            {
                Debug.LogError("[BlockGridUI] No BlockButton prefab assigned!");
                return;
            }

            int blockCount = blockCatalog.BlockCount;
            blockButtons = new BlockButton[blockCount];

            for (int i = 0; i < blockCount; i++)
            {
                BlockData blockData = blockCatalog.GetBlock(i);

                if (blockData == null || blockData.blockPrefab == null)
                    continue;

                // Instantiate button
                GameObject buttonObj = Instantiate(blockButtonPrefab, buttonContainer);
                buttonObj.name = $"BlockButton_{blockData.blockName}";

                // Initialize button
                BlockButton blockButton = buttonObj.GetComponent<BlockButton>();
                if (blockButton != null)
                {
                    blockButton.Initialize(blockData);
                    blockButtons[i] = blockButton;
                }
                else
                {
                    Debug.LogError($"[BlockGridUI] BlockButton component not found on prefab!");
                    Destroy(buttonObj);
                }
            }

            Debug.Log($"[BlockGridUI] Populated grid with {blockCount} blocks");
        }

        /// <summary>
        /// Clears all buttons from the grid
        /// </summary>
        private void ClearGrid()
        {
            if (buttonContainer == null)
                return;

            // Destroy all child buttons
            for (int i = buttonContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = buttonContainer.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            blockButtons = null;
        }

        /// <summary>
        /// Refreshes the grid (useful if catalog changes)
        /// </summary>
        public void RefreshGrid()
        {
            PopulateGrid();
        }

        /// <summary>
        /// Sets a new block catalog and refreshes the grid
        /// </summary>
        public void SetBlockCatalog(BlockCatalog catalog)
        {
            blockCatalog = catalog;
            RefreshGrid();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Update grid settings in editor
            if (gridLayout != null && !Application.isPlaying)
            {
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = columns;
                gridLayout.cellSize = cellSize;
                gridLayout.spacing = spacing;
            }
        }
#endif
    }
}
