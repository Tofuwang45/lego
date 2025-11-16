using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MRTemplateAssets.Scripts
{
    /// <summary>
    /// Manages the tab system for switching between block categories
    /// </summary>
    public class TabSystem : MonoBehaviour
    {
        [System.Serializable]
        public class TabButton
        {
            public BlockCategory category;
            public Button button;
            public TextMeshProUGUI label;
            public Image background;
        }

        [Header("Tab Configuration")]
        public List<TabButton> tabs = new List<TabButton>();

        [Header("Visual Settings")]
        public Color activeTabColor = new Color(0.2f, 0.6f, 1f);
        public Color inactiveTabColor = new Color(0.3f, 0.3f, 0.3f);

        // Events
        public event System.Action<BlockCategory> OnTabChanged;

        private BlockCatalogData catalogData;
        private BlockCategory currentCategory = BlockCategory.Bricks;

        public void Initialize(BlockCatalogData catalog)
        {
            catalogData = catalog;

            // Setup button listeners
            foreach (var tab in tabs)
            {
                BlockCategory category = tab.category; // Capture for lambda
                tab.button.onClick.AddListener(() => SelectTab(category));
            }

            // Select the first tab by default
            if (tabs.Count > 0)
            {
                SelectTab(tabs[0].category);
            }
        }

        /// <summary>
        /// Select a tab by category
        /// </summary>
        public void SelectTab(BlockCategory category)
        {
            currentCategory = category;

            // Update visual state of all tabs
            foreach (var tab in tabs)
            {
                bool isActive = tab.category == category;
                UpdateTabVisuals(tab, isActive);
            }

            // Notify listeners
            OnTabChanged?.Invoke(category);
        }

        private void UpdateTabVisuals(TabButton tab, bool isActive)
        {
            if (tab.background != null)
            {
                tab.background.color = isActive ? activeTabColor : inactiveTabColor;
            }

            if (tab.label != null)
            {
                tab.label.fontStyle = isActive ? FontStyles.Bold : FontStyles.Normal;
            }

            // Scale effect for active tab
            if (tab.button != null)
            {
                tab.button.transform.localScale = isActive ? Vector3.one * 1.1f : Vector3.one;
            }
        }

        /// <summary>
        /// Get the currently selected category
        /// </summary>
        public BlockCategory GetCurrentCategory()
        {
            return currentCategory;
        }

        private void OnDestroy()
        {
            // Clean up button listeners
            foreach (var tab in tabs)
            {
                tab.button.onClick.RemoveAllListeners();
            }
        }
    }
}
