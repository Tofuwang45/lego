using UnityEngine;
using UnityEngine.UI;

namespace MRTemplateAssets.Scripts
{
    /// <summary>
    /// Manages delete mode for removing placed blocks
    /// </summary>
    public class DeleteMode : MonoBehaviour
    {
        [Header("Delete Mode Settings")]
        [Tooltip("Toggle button for delete mode")]
        public Toggle deleteModeToggle;

        [Tooltip("Transform to use as the raycast origin (e.g., controller or camera)")]
        public Transform raycastOrigin;

        [Header("Visual Feedback")]
        [Tooltip("Color of the ray when in delete mode")]
        public Color deleteRayColor = Color.red;

        [Tooltip("Normal color of the ray")]
        public Color normalRayColor = Color.blue;

        [Tooltip("Visual line renderer for the ray")]
        public LineRenderer rayLineRenderer;

        [Header("Delete Settings")]
        [Tooltip("Layer mask for blocks that can be deleted")]
        public LayerMask deletableLayerMask = -1;

        [Tooltip("Maximum distance for deletion")]
        public float maxDeleteDistance = 10f;

        private bool isDeleteModeActive = false;
        private GameObject highlightedObject;
        private Material originalMaterial;
        private Material highlightMaterial;
        private bool isDeleteInputPressed = false;

        private void Awake()
        {
            // Create highlight material
            highlightMaterial = new Material(Shader.Find("Standard"));
            highlightMaterial.color = new Color(1f, 0.3f, 0.3f, 0.5f);
            highlightMaterial.SetFloat("_Mode", 3); // Transparent mode
            highlightMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            highlightMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            highlightMaterial.SetInt("_ZWrite", 0);
            highlightMaterial.DisableKeyword("_ALPHATEST_ON");
            highlightMaterial.EnableKeyword("_ALPHABLEND_ON");
            highlightMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            highlightMaterial.renderQueue = 3000;

            // Auto-find raycast origin if not assigned
            if (raycastOrigin == null)
            {
                raycastOrigin = Camera.main?.transform;
            }
        }

        private void Start()
        {
            // Setup toggle listener
            if (deleteModeToggle != null)
            {
                deleteModeToggle.onValueChanged.AddListener(OnDeleteModeToggled);
            }

            // Initialize ray color
            UpdateRayColor();
        }

        private void Update()
        {
            if (!isDeleteModeActive) return;

            // Highlight object under ray
            HighlightObjectUnderRay();

            // Handle delete input
            if (isDeleteInputPressed)
            {
                DeleteHighlightedObject();
                isDeleteInputPressed = false;
            }
        }

        /// <summary>
        /// Call this method when delete input is detected (e.g., from a button or input event)
        /// </summary>
        public void OnDeleteInputPressed()
        {
            isDeleteInputPressed = true;
        }

        private void OnDeleteModeToggled(bool isActive)
        {
            isDeleteModeActive = isActive;
            UpdateRayColor();

            // Clear highlight when exiting delete mode
            if (!isActive)
            {
                ClearHighlight();
            }
        }

        private void UpdateRayColor()
        {
            if (rayLineRenderer != null)
            {
                Color startColor = isDeleteModeActive ? deleteRayColor : normalRayColor;
                Color endColor = startColor;
                endColor.a = 0.5f;

                rayLineRenderer.startColor = startColor;
                rayLineRenderer.endColor = endColor;
            }
        }

        private void HighlightObjectUnderRay()
        {
            if (raycastOrigin == null)
            {
                Debug.LogWarning("DeleteMode: Raycast origin is not set!");
                return;
            }

            RaycastHit hit;
            Ray ray = new Ray(raycastOrigin.position, raycastOrigin.forward);

            if (Physics.Raycast(ray, out hit, maxDeleteDistance, deletableLayerMask))
            {
                ProcessHighlight(hit);
            }
            else
            {
                ClearHighlight();
            }
        }

        private void ProcessHighlight(RaycastHit hit)
        {
            GameObject hitObject = hit.collider.gameObject;

            // Check if this is a new object to highlight
            if (highlightedObject != hitObject)
            {
                ClearHighlight();
                highlightedObject = hitObject;
                ApplyHighlight();
            }
        }

        private void ApplyHighlight()
        {
            if (highlightedObject == null) return;

            Renderer renderer = highlightedObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                originalMaterial = renderer.material;
                renderer.material = highlightMaterial;
            }
        }

        private void ClearHighlight()
        {
            if (highlightedObject != null)
            {
                Renderer renderer = highlightedObject.GetComponent<Renderer>();
                if (renderer != null && originalMaterial != null)
                {
                    renderer.material = originalMaterial;
                }

                highlightedObject = null;
                originalMaterial = null;
            }
        }

        private void DeleteHighlightedObject()
        {
            if (highlightedObject == null) return;

            // Record undo action
            if (UndoSystem.Instance != null)
            {
                UndoSystem.Instance.RecordDeletion(highlightedObject);
            }

            // Destroy the object
            Destroy(highlightedObject);
            highlightedObject = null;

            Debug.Log("Deleted block");
        }

        /// <summary>
        /// Set delete mode programmatically
        /// </summary>
        public void SetDeleteMode(bool active)
        {
            if (deleteModeToggle != null)
            {
                deleteModeToggle.isOn = active;
            }
            else
            {
                OnDeleteModeToggled(active);
            }
        }

        private void OnDestroy()
        {
            if (deleteModeToggle != null)
            {
                deleteModeToggle.onValueChanged.RemoveListener(OnDeleteModeToggled);
            }

            ClearHighlight();

            if (highlightMaterial != null)
            {
                Destroy(highlightMaterial);
            }
        }
    }
}
