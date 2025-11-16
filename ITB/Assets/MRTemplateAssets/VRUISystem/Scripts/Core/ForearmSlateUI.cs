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

            Debug.Log("[ForearmSlateUI] Initializing ForearmSlateUI");
            Debug.Log($"  - blockCatalog: {(blockCatalog != null ? "Assigned" : "NULL")}");
            Debug.Log($"  - tabSystem: {(tabSystem != null ? "Assigned" : "NULL")}");
            Debug.Log($"  - gridManager: {(gridManager != null ? "Assigned" : "NULL")}");
            Debug.Log($"  - recentsManager: {(recentsManager != null ? "Assigned" : "NULL")}");
            
            if (blockCatalog == null)
            {
                Debug.LogError("[ForearmSlateUI] blockCatalog is NOT assigned! Grid will not populate.");
            }
            else
            {
                Debug.Log($"[ForearmSlateUI] blockCatalog has {blockCatalog.allBlocks.Count} total blocks");
            }

            // Initialize subsystems
            if (tabSystem != null)
            {
                Debug.Log("[ForearmSlateUI] Initializing TabSystem");
                tabSystem.Initialize(blockCatalog);
                tabSystem.OnTabChanged += OnTabChanged;
            }
            else
            {
                Debug.LogError("[ForearmSlateUI] TabSystem not assigned!");
            }

            if (gridManager != null)
            {
                Debug.Log("[ForearmSlateUI] Initializing GridLayoutManager");
                gridManager.Initialize(blockCatalog);
                
                // Auto-populate grid with first category
                Debug.Log("[ForearmSlateUI] Auto-populating grid with first category");
                if (tabSystem != null)
                {
                    BlockCategory firstCategory = tabSystem.GetCurrentCategory();
                    gridManager.UpdateGrid(firstCategory);
                }
            }
            else
            {
                Debug.LogError("[ForearmSlateUI] GridLayoutManager not assigned!");
            }

            if (recentsManager != null)
            {
                Debug.Log("[ForearmSlateUI] Initializing RecentsManager");
                recentsManager.Initialize(blockCatalog);
            }

            // Create stats panel if prefab is assigned
            if (statsPanelPrefab != null)
            {
                Debug.Log("[ForearmSlateUI] Creating stats panel");
                CreateStatsPanel();
            }

            Debug.Log("[ForearmSlateUI] Initialization complete");
            isInitialized = true;
        }

        private void SetupCanvas()
        {
            canvas.renderMode = RenderMode.WorldSpace;
        }



        private void OnTabChanged(BlockCategory category)
        {
            Debug.Log($"[ForearmSlateUI] OnTabChanged event: {category}");
            if (gridManager != null)
            {
                Debug.Log($"[ForearmSlateUI] Calling gridManager.UpdateGrid({category})");
                gridManager.UpdateGrid(category);
            }
            else
            {
                Debug.LogError($"[ForearmSlateUI] gridManager is NULL! Cannot update grid.");
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
