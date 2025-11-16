using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MRTemplateAssets.Scripts
{
    /// <summary>
    /// Main controller for the Forearm Slate UI that attaches to the left hand controller
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class ForearmSlateUI : MonoBehaviour
    {
        [Header("Hand Attachment")]
        [Tooltip("The left hand controller transform to attach to")]
        public Transform leftHandController;

        [Header("Positioning")]
        [Tooltip("Offset from the hand controller")]
        public Vector3 positionOffset = new Vector3(0.1f, 0.05f, 0.1f);

        [Tooltip("Rotation offset from the hand controller")]
        public Vector3 rotationOffset = new Vector3(45f, 0f, 0f);

        [Tooltip("Scale of the UI slate")]
        public Vector3 slateScale = new Vector3(0.001f, 0.001f, 0.001f);

        [Header("References")]
        public BlockCatalogData blockCatalog;
        public TabSystem tabSystem;
        public GridLayoutManager gridManager;
        public RecentsManager recentsManager;
        public GameObject statsPanelPrefab;

        [Header("Interaction")]
        [Tooltip("Right hand ray interactor for UI interaction")]
        public UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rightHandRayInteractor;

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

            // Auto-find left hand controller if not assigned
            if (leftHandController == null)
            {
                leftHandController = FindLeftHandController();
            }

            // Auto-find right hand ray interactor if not assigned
            if (rightHandRayInteractor == null)
            {
                rightHandRayInteractor = FindRightHandRayInteractor();
            }

            // Attach to hand
            if (leftHandController != null)
            {
                AttachToHand();
            }
            else
            {
                Debug.LogError("ForearmSlateUI: Could not find left hand controller!");
            }

            // Initialize subsystems
            if (tabSystem != null)
            {
                tabSystem.Initialize(blockCatalog);
                tabSystem.OnTabChanged += OnTabChanged;
            }

            if (gridManager != null)
            {
                gridManager.Initialize(blockCatalog, rightHandRayInteractor);
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
            transform.localScale = slateScale;

            // Set canvas size (A5-ish dimensions: 200x150)
            RectTransform rectTransform = canvas.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(200f, 150f);
            }
        }

        private void AttachToHand()
        {
            transform.SetParent(leftHandController);
            transform.localPosition = positionOffset;
            transform.localRotation = Quaternion.Euler(rotationOffset);
        }

        private Transform FindLeftHandController()
        {
            // Search for any controller with "Left" in the name
            var controllers = FindObjectsByType<XRController>(FindObjectsSortMode.None);
            foreach (var controller in controllers)
            {
                if (controller.name.Contains("Left"))
                {
                    return controller.transform;
                }
            }

            // If no controller found, try searching for any object with "LeftHand" in the name
            var allObjects = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.name.Contains("LeftHand") || obj.name.Contains("Left Hand"))
                {
                    return obj;
                }
            }
            return null;
        }

        private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor FindRightHandRayInteractor()
        {
            var rayInteractors = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(FindObjectsSortMode.None);
            foreach (var interactor in rayInteractors)
            {
                if (interactor.name.Contains("Right"))
                {
                    return interactor;
                }
            }
            return null;
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
