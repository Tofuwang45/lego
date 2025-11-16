using UnityEngine;
using Unity.XR.CoreUtils;

namespace MRTemplateAssets.Scripts
{
    /// <summary>
    /// Main controller for the Forearm Slate UI
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class ForearmSlateUI : MonoBehaviour
    {
        [Header("References")]
        public BlockCatalogData blockCatalog;
        public TabSystem tabSystem;
        public GridLayoutManager gridManager;
        public RecentsManager recentsManager;
        public GameObject statsPanelPrefab;

        private Canvas canvas;
        private StatsPanel statsPanel;
        private bool isInitialized = false;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            SetupCanvas();
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (isInitialized) return;

            // Initialize subsystems
            if (tabSystem != null)
            {
                tabSystem.Initialize(blockCatalog);
                tabSystem.OnTabChanged += OnTabChanged;
            }

            if (gridManager != null)
            {
                gridManager.Initialize(blockCatalog);
            }

            if (recentsManager != null)
            {
                recentsManager.Initialize(blockCatalog);
            }

            // Create stats panel if prefab is assigned
            if (statsPanelPrefab != null)
            {
                CreateStatsPanel();
            }

            isInitialized = true;
        }

        private void SetupCanvas()
        {
            canvas.renderMode = RenderMode.WorldSpace;
        }



        private void OnTabChanged(BlockCategory category)
        {
            if (gridManager != null)
            {
                gridManager.UpdateGrid(category);
            }
        }

        private void CreateStatsPanel()
        {
            GameObject panelObj = Instantiate(statsPanelPrefab);
            statsPanel = panelObj.GetComponent<StatsPanel>();

            if (statsPanel != null)
            {
                // Position the stats panel in world space (lazy follow or wrist position)
                statsPanel.Initialize();
            }
        }

        private void OnDestroy()
        {
            if (tabSystem != null)
            {
                tabSystem.OnTabChanged -= OnTabChanged;
            }
        }

        /// <summary>
        /// Show or hide the forearm slate
        /// </summary>
        public void SetSlateActive(bool active)
        {
            canvas.enabled = active;
        }

        /// <summary>
        /// Toggle the slate visibility
        /// </summary>
        public void ToggleSlate()
        {
            SetSlateActive(!canvas.enabled);
        }
    }
}
