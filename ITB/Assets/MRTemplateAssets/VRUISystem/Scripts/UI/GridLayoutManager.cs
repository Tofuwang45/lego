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
            
            Debug.Log($"[GridLayoutManager] Initializing GridLayoutManager");
            Debug.Log($"  - Catalog: {(catalogData != null ? "Assigned" : "NULL")}");
            Debug.Log($"  - Grid Container: {(gridContainer != null ? gridContainer.name : "NULL")}");
            Debug.Log($"  - Button Prefab: {(blockButtonPrefab != null ? blockButtonPrefab.name : "NULL")}");
            
            if (catalogData != null)
            {
                Debug.Log($"  - Total blocks in catalog: {catalogData.allBlocks.Count}");
            }

            // Ensure grid layout group is configured
            var gridLayout = gridContainer.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = columns;
                Debug.Log($"[GridLayoutManager] Configured GridLayoutGroup: {columns} columns");
            }
            else
            {
                Debug.LogWarning($"[GridLayoutManager] GridLayoutGroup not found on gridContainer!");
            }
        }

        /// <summary>
        /// Update the grid to show blocks from a specific category
        /// </summary>
        public void UpdateGrid(BlockCategory category)
        {
            Debug.Log($"[GridLayoutManager] UpdateGrid called for category: {category}");
            currentCategory = category;

            // Clear existing buttons
            ClearGrid();

            // Get blocks for this category
            if (catalogData == null)
            {
                Debug.LogError($"[GridLayoutManager] catalogData is NULL! Cannot update grid.");
                return;
            }
            
            List<BlockData> blocks = catalogData.GetBlocksByCategory(category);
            Debug.Log($"[GridLayoutManager] Retrieved {blocks.Count} blocks for category {category}");

            // Limit to grid size (3x3 = 9 items)
            int maxItems = rows * columns;
            int itemCount = Mathf.Min(blocks.Count, maxItems);
            Debug.Log($"[GridLayoutManager] Creating {itemCount} buttons (max: {maxItems})");

            // Create buttons for each block
            for (int i = 0; i < itemCount; i++)
            {
                Debug.Log($"[GridLayoutManager] Creating button {i + 1}/{itemCount} for block: {blocks[i].blockName}");
                CreateBlockButton(blocks[i]);
            }
            
            // Force full layout rebuild after all buttons are added
            Debug.Log($"[GridLayoutManager] Forcing full layout rebuild");
            RectTransform gridRect = gridContainer as RectTransform;
            if (gridRect != null)
            {
                LayoutRebuilder.MarkLayoutForRebuild(gridRect);
                Canvas.ForceUpdateCanvases();
            }
            
            // Schedule a delayed log to show positions after layout is applied
            StartCoroutine(LogButtonPositionsAfterLayout());
            
            Debug.Log($"[GridLayoutManager] Grid update complete. Total buttons: {currentButtons.Count}");
        }

        private System.Collections.IEnumerator LogButtonPositionsAfterLayout()
        {
            yield return new WaitForEndOfFrame();
            
            Debug.Log($"[GridLayoutManager] After layout rebuild - Button positions:");
            for (int i = 0; i < currentButtons.Count; i++)
            {
                RectTransform rect = currentButtons[i].GetComponent<RectTransform>();
                if (rect != null)
                {
                    Debug.Log($"  Button {i + 1}: Position = {rect.localPosition}, Size = {rect.sizeDelta}");
                }
            }
        }

        private void CreateBlockButton(BlockData blockData)
        {
            if (blockButtonPrefab == null)
            {
                Debug.LogError("[GridLayoutManager] Block button prefab is not assigned!");
                return;
            }

            GameObject buttonObj = Instantiate(blockButtonPrefab, gridContainer);
            Debug.Log($"[GridLayoutManager] Instantiated button prefab: {buttonObj.name}");
            
            BlockButton blockButton = buttonObj.GetComponent<BlockButton>();

            if (blockButton != null)
            {
                Debug.Log($"[GridLayoutManager] BlockButton component found. Initializing with: {blockData.blockName}");
                blockButton.Initialize(blockData);
                currentButtons.Add(blockButton);
                Debug.Log($"[GridLayoutManager] Button added to grid. Total buttons now: {currentButtons.Count}");
                
                // Ensure button has a LayoutElement if GridLayoutGroup needs it
                RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
                LayoutElement layoutElement = buttonObj.GetComponent<LayoutElement>();
                
                if (buttonRect != null)
                {
                    Debug.Log($"[GridLayoutManager] Before layout - Position: {buttonRect.localPosition}, Size: {buttonRect.sizeDelta}");
                    
                    if (layoutElement == null)
                    {
                        Debug.LogWarning("[GridLayoutManager] Button missing LayoutElement, adding one");
                        layoutElement = buttonObj.AddComponent<LayoutElement>();
                        layoutElement.preferredWidth = 100;
                        layoutElement.preferredHeight = 100;
                    }
                }
            }
            else
            {
                Debug.LogError($"[GridLayoutManager] BlockButton component NOT found on prefab!");
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
