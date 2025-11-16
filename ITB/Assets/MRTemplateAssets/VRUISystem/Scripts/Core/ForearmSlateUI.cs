using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

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
        [Tooltip("Right hand interactor for UI interaction (XRRayInteractor or NearFarInteractor)")]
        public XRRayInteractor rightHandRayInteractor;

        [Tooltip("Right hand Near-Far interactor (alternative to Ray Interactor)")]
        public NearFarInteractor rightHandNearFarInteractor;

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

            // Auto-find right hand interactor if not assigned
            if (rightHandRayInteractor == null && rightHandNearFarInteractor == null)
            {
                FindRightHandInteractor();
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
                gridManager.Initialize(blockCatalog, rightHandRayInteractor, rightHandNearFarInteractor);
            }

            if (recentsManager != null)
            {
                recentsManager.Initialize(blockCatalog, rightHandRayInteractor, rightHandNearFarInteractor);
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
            // Try to find the left hand controller in the XR Origin hierarchy
            var xrOrigin = FindFirstObjectByType<XROrigin>();
            if (xrOrigin != null)
            {
                // Look for common naming patterns
                Transform leftHand = xrOrigin.transform.Find("Camera Offset/LeftHand Controller");
                if (leftHand == null)
                {
                    leftHand = xrOrigin.transform.Find("Camera Offset/Left Hand");
                }
                if (leftHand == null)
                {
                    leftHand = xrOrigin.transform.Find("Camera Offset/LeftController");
                }
                if (leftHand == null)
                {
                    // Search for any object with "Left" and "Controller" or "Hand" in the name
                    var allTransforms = xrOrigin.GetComponentsInChildren<Transform>();
                    foreach (var t in allTransforms)
                    {
                        if (t.name.Contains("Left") && (t.name.Contains("Controller") || t.name.Contains("Hand")))
                        {
                            leftHand = t;
                            break;
                        }
                    }
                }
                return leftHand;
            }
            return null;
        }

        private void FindRightHandInteractor()
        {
            // Try to find NearFarInteractor first (modern approach)
            var nearFarInteractors = FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None);
            foreach (var interactor in nearFarInteractors)
            {
                if (interactor.name.Contains("Right"))
                {
                    rightHandNearFarInteractor = interactor;
                    Debug.Log("ForearmSlateUI: Found NearFarInteractor on right hand");
                    return;
                }
            }

            // Fallback to XRRayInteractor (legacy approach)
            var rayInteractors = FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None);
            foreach (var interactor in rayInteractors)
            {
                if (interactor.name.Contains("Right"))
                {
                    rightHandRayInteractor = interactor;
                    Debug.Log("ForearmSlateUI: Found XRRayInteractor on right hand");
                    return;
                }
            }

            Debug.LogWarning("ForearmSlateUI: Could not find right hand interactor (NearFarInteractor or XRRayInteractor)");
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
