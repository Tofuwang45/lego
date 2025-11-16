using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace MRTemplateAssets.Scripts
{
    /// <summary>
    /// Displays block usage statistics with expandable view
    /// </summary>
    public class StatsPanel : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Text showing total block count")]
        public TextMeshProUGUI totalCountText;

        [Tooltip("Container for top 3 blocks display")]
        public Transform topBlocksContainer;

        [Tooltip("Container for full list (when expanded)")]
        public Transform fullListContainer;

        [Tooltip("Prefab for stat line items")]
        public GameObject statLinePrefab;

        [Tooltip("Button to expand/collapse the panel")]
        public Button expandButton;

        [Tooltip("Icon for expand button")]
        public Image expandIcon;

        [Header("Panel Settings")]
        [Tooltip("Position settings for the panel")]
        public Vector3 panelPosition = new Vector3(0.3f, 0.1f, 0.5f);

        [Tooltip("Whether to use lazy follow behavior")]
        public bool useLazyFollow = true;

        [Header("Visual Settings")]
        public Sprite expandedIcon;
        public Sprite collapsedIcon;

        private bool isExpanded = false;
        private List<GameObject> statLines = new List<GameObject>();
        private Canvas canvas;

        public void Initialize()
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            // Setup canvas for world space
            canvas.renderMode = RenderMode.WorldSpace;
            transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            // Always position panel at fixed location
            transform.position = panelPosition;

            // Setup expand button
            if (expandButton != null)
            {
                expandButton.onClick.AddListener(ToggleExpanded);
            }

            // Subscribe to usage tracker
            if (BlockUsageTracker.Instance != null)
            {
                BlockUsageTracker.Instance.OnUsageStatsUpdated += UpdateStats;
            }

            // Initial update
            UpdateStats();
        }

        private void UpdateStats()
        {
            if (BlockUsageTracker.Instance == null) return;

            // Update total count
            int totalCount = BlockUsageTracker.Instance.GetTotalBlockCount();
            if (totalCountText != null)
            {
                totalCountText.text = $"Total: {totalCount} blocks";
            }

            // Update display based on expanded state
            if (isExpanded)
            {
                ShowFullList();
            }
            else
            {
                ShowTopThree();
            }
        }

        private void ShowTopThree()
        {
            ClearStatLines();

            if (topBlocksContainer == null) return;

            List<BlockUsageTracker.BlockUsage> topBlocks = BlockUsageTracker.Instance.GetTopUsedBlocks(3);

            foreach (var usage in topBlocks)
            {
                CreateStatLine(usage, topBlocksContainer);
            }

            // Hide full list container
            if (fullListContainer != null)
            {
                fullListContainer.gameObject.SetActive(false);
            }
            if (topBlocksContainer != null)
            {
                topBlocksContainer.gameObject.SetActive(true);
            }
        }

        private void ShowFullList()
        {
            ClearStatLines();

            if (fullListContainer == null) return;

            List<BlockUsageTracker.BlockUsage> allBlocks = BlockUsageTracker.Instance.GetAllUsageStats();

            foreach (var usage in allBlocks)
            {
                CreateStatLine(usage, fullListContainer);
            }

            // Show full list container
            if (fullListContainer != null)
            {
                fullListContainer.gameObject.SetActive(true);
            }
            if (topBlocksContainer != null)
            {
                topBlocksContainer.gameObject.SetActive(false);
            }
        }

        private void CreateStatLine(BlockUsageTracker.BlockUsage usage, Transform parent)
        {
            if (statLinePrefab == null) return;

            GameObject lineObj = Instantiate(statLinePrefab, parent);
            TextMeshProUGUI lineText = lineObj.GetComponentInChildren<TextMeshProUGUI>();

            if (lineText != null)
            {
                lineText.text = $"{usage.count}x {usage.GetColorName()} {usage.blockName}";
            }

            statLines.Add(lineObj);
        }

        private void ClearStatLines()
        {
            foreach (var line in statLines)
            {
                if (line != null)
                {
                    Destroy(line);
                }
            }
            statLines.Clear();
        }

        private void ToggleExpanded()
        {
            isExpanded = !isExpanded;
            UpdateStats();

            // Update expand icon
            if (expandIcon != null)
            {
                expandIcon.sprite = isExpanded ? expandedIcon : collapsedIcon;
            }
        }

        private void OnDestroy()
        {
            if (BlockUsageTracker.Instance != null)
            {
                BlockUsageTracker.Instance.OnUsageStatsUpdated -= UpdateStats;
            }

            if (expandButton != null)
            {
                expandButton.onClick.RemoveListener(ToggleExpanded);
            }

            ClearStatLines();
        }
    }
}
