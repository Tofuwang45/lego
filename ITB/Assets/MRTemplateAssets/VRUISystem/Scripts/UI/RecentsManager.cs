using System.Collections.Generic;
using UnityEngine;


namespace MRTemplateAssets.Scripts
{
    /// <summary>
    /// Manages the hotbar of recently used blocks
    /// </summary>
    public class RecentsManager : MonoBehaviour
    {
        [Header("Recents Configuration")]
        [Tooltip("Container for recent block buttons")]
        public Transform recentsContainer;

        [Tooltip("Prefab for recent block buttons")]
        public GameObject recentBlockButtonPrefab;

        [Tooltip("Maximum number of recent blocks to show")]
        public int maxRecents = 6;

        [Header("Interaction")]
        public UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rayInteractor;

        private BlockCatalogData catalogData;
        private List<BlockButton> recentButtons = new List<BlockButton>();

        public void Initialize(BlockCatalogData catalog)
        {
            catalogData = catalog;

            // Subscribe to usage tracker updates
            if (BlockUsageTracker.Instance != null)
            {
                BlockUsageTracker.Instance.OnUsageStatsUpdated += UpdateRecents;
            }

            // Initial update
            UpdateRecents();
        }

        private void UpdateRecents()
        {
            if (BlockUsageTracker.Instance == null) return;

            // Clear existing buttons
            ClearRecents();

            // Get recent blocks
            List<BlockUsageTracker.BlockUsage> recents = BlockUsageTracker.Instance.GetRecentBlocks();

            // Create buttons for each recent block
            int count = Mathf.Min(recents.Count, maxRecents);
            for (int i = 0; i < count; i++)
            {
                CreateRecentButton(recents[i]);
            }
        }

        private void CreateRecentButton(BlockUsageTracker.BlockUsage usage)
        {
            if (recentBlockButtonPrefab == null || catalogData == null) return;

            // Get the block data
            BlockData blockData = catalogData.GetBlockById(usage.blockId);
            if (blockData == null) return;

            // Create button
            GameObject buttonObj = Instantiate(recentBlockButtonPrefab, recentsContainer);
            BlockButton blockButton = buttonObj.GetComponent<BlockButton>();

            if (blockButton != null)
            {
                blockButton.Initialize(blockData, rayInteractor);
                recentButtons.Add(blockButton);

                // Set the default color to the one used in this usage
                // This would require modifying BlockButton to accept a default color
                // For now, it will use the block's default color
            }
        }

        private void ClearRecents()
        {
            foreach (var button in recentButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            recentButtons.Clear();
        }

        private void OnDestroy()
        {
            if (BlockUsageTracker.Instance != null)
            {
                BlockUsageTracker.Instance.OnUsageStatsUpdated -= UpdateRecents;
            }
            ClearRecents();
        }
    }
}
