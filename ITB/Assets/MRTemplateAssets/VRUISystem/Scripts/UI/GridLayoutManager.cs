using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MRTemplateAssets.Scripts
{
    /// <summary>
    /// Manages the 3x3 grid layout for block selection
    /// </summary>
    public class GridLayoutManager : MonoBehaviour
    {
        [Header("Grid Configuration")]
        [Tooltip("The parent transform containing the grid layout group")]
        public Transform gridContainer;

        [Tooltip("Prefab for block selection buttons")]
        public GameObject blockButtonPrefab;

        [Header("Grid Settings")]
        [Tooltip("Number of rows in the grid")]
        public int rows = 3;

        [Tooltip("Number of columns in the grid")]
        public int columns = 3;

        private BlockCatalogData catalogData;
        private List<BlockButton> currentButtons = new List<BlockButton>();
        private BlockCategory currentCategory;

        public void Initialize(BlockCatalogData catalog)
        {
            catalogData = catalog;

            // Ensure grid layout group is configured
            var gridLayout = gridContainer.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = columns;
            }
        }

        /// <summary>
        /// Update the grid to show blocks from a specific category
        /// </summary>
        public void UpdateGrid(BlockCategory category)
        {
            currentCategory = category;

            // Clear existing buttons
            ClearGrid();

            // Get blocks for this category
            List<BlockData> blocks = catalogData.GetBlocksByCategory(category);

            // Limit to grid size (3x3 = 9 items)
            int maxItems = rows * columns;
            int itemCount = Mathf.Min(blocks.Count, maxItems);

            // Create buttons for each block
            for (int i = 0; i < itemCount; i++)
            {
                CreateBlockButton(blocks[i]);
            }
        }

        private void CreateBlockButton(BlockData blockData)
        {
            if (blockButtonPrefab == null)
            {
                Debug.LogError("GridLayoutManager: Block button prefab is not assigned!");
                return;
            }

            GameObject buttonObj = Instantiate(blockButtonPrefab, gridContainer);
            BlockButton blockButton = buttonObj.GetComponent<BlockButton>();

            if (blockButton != null)
            {
                blockButton.Initialize(blockData);
                currentButtons.Add(blockButton);
            }
        }

        private void ClearGrid()
        {
            foreach (var button in currentButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            currentButtons.Clear();
        }

        private void OnDestroy()
        {
            ClearGrid();
        }
    }
}
