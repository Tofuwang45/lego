using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using TMPro;

namespace MRTemplateAssets.Scripts
{
    /// <summary>
    /// Interactive button for selecting a Lego block with ghost preview functionality
    /// </summary>
    public class BlockButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("UI References")]
        public Image iconImage;
        public TextMeshProUGUI nameLabel;
        public Button button;

        [Header("Color Selection")]
        public Transform colorDotsContainer;
        public GameObject colorDotPrefab;

        [Header("Ghost Preview")]
        [Tooltip("Material to use for ghost preview (semi-transparent)")]
        public Material ghostMaterial;

        [Tooltip("Distance from ray hit point to show ghost")]
        public float ghostDistance = 0.3f;

        private BlockData blockData;
        private XRRayInteractor rayInteractor;
        private GameObject ghostPreview;
        private Color selectedColor = Color.red;
        private bool isHovering = false;

        /// <summary>
        /// Initialize the button with block data
        /// </summary>
        public void Initialize(BlockData data, XRRayInteractor interactor)
        {
            blockData = data;
            rayInteractor = interactor;

            // Set up UI
            if (iconImage != null && data.icon != null)
            {
                iconImage.sprite = data.icon;
            }

            if (nameLabel != null)
            {
                nameLabel.text = data.blockName;
            }

            // Create color selection dots
            CreateColorDots();

            // Default to first available color
            if (data.availableColors.Count > 0)
            {
                selectedColor = data.availableColors[0];
            }
        }

        private void CreateColorDots()
        {
            if (colorDotsContainer == null || colorDotPrefab == null) return;

            // Clear existing dots
            foreach (Transform child in colorDotsContainer)
            {
                Destroy(child.gameObject);
            }

            // Create a dot for each available color
            foreach (Color color in blockData.availableColors)
            {
                GameObject dotObj = Instantiate(colorDotPrefab, colorDotsContainer);
                Image dotImage = dotObj.GetComponent<Image>();
                if (dotImage != null)
                {
                    dotImage.color = color;
                }

                Button dotButton = dotObj.GetComponent<Button>();
                if (dotButton != null)
                {
                    Color capturedColor = color; // Capture for lambda
                    dotButton.onClick.AddListener(() => SelectColor(capturedColor));
                }
            }
        }

        private void SelectColor(Color color)
        {
            selectedColor = color;

            // Update ghost preview color if it exists
            if (ghostPreview != null)
            {
                UpdateGhostColor();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
            ShowGhostPreview();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            HideGhostPreview();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SpawnBlock();
        }

        private void Update()
        {
            // Update ghost preview position while hovering
            if (isHovering && ghostPreview != null && rayInteractor != null)
            {
                UpdateGhostPosition();
            }
        }

        private void ShowGhostPreview()
        {
            if (blockData == null || blockData.prefab == null) return;

            // Create ghost preview
            ghostPreview = Instantiate(blockData.prefab);

            // Apply ghost material to all renderers
            Renderer[] renderers = ghostPreview.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = new Material[renderer.materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    if (ghostMaterial != null)
                    {
                        materials[i] = ghostMaterial;
                    }
                    else
                    {
                        // Create a transparent version of the original material
                        materials[i] = new Material(renderer.materials[i]);
                        Color color = materials[i].color;
                        color.a = 0.5f;
                        materials[i].color = color;
                    }
                }
                renderer.materials = materials;
            }

            UpdateGhostColor();

            // Disable colliders on ghost
            Collider[] colliders = ghostPreview.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }
        }

        private void HideGhostPreview()
        {
            if (ghostPreview != null)
            {
                Destroy(ghostPreview);
                ghostPreview = null;
            }
        }

        private void UpdateGhostPosition()
        {
            if (rayInteractor == null || ghostPreview == null) return;

            // Get the ray hit information
            RaycastHit hit;
            if (rayInteractor.TryGetCurrent3DRaycastHit(out hit))
            {
                // Position ghost at ray hit point
                ghostPreview.transform.position = hit.point + hit.normal * ghostDistance;
                ghostPreview.transform.rotation = Quaternion.LookRotation(hit.normal);
            }
            else
            {
                // Position ghost along the ray at a fixed distance
                Ray ray = new Ray(rayInteractor.transform.position, rayInteractor.transform.forward);
                ghostPreview.transform.position = ray.GetPoint(1.0f);
                ghostPreview.transform.rotation = rayInteractor.transform.rotation;
            }
        }

        private void UpdateGhostColor()
        {
            if (ghostPreview == null) return;

            Renderer[] renderers = ghostPreview.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                foreach (Material mat in renderer.materials)
                {
                    // Apply selected color with transparency
                    Color ghostColor = selectedColor;
                    ghostColor.a = 0.5f;
                    mat.color = ghostColor;
                }
            }
        }

        private void SpawnBlock()
        {
            if (blockData == null || blockData.prefab == null) return;

            // Spawn the actual block
            GameObject spawnedBlock = Instantiate(blockData.prefab);

            // Position it at the ghost location or ray hit point
            if (ghostPreview != null)
            {
                spawnedBlock.transform.position = ghostPreview.transform.position;
                spawnedBlock.transform.rotation = ghostPreview.transform.rotation;
            }
            else if (rayInteractor != null)
            {
                RaycastHit hit;
                if (rayInteractor.TryGetCurrent3DRaycastHit(out hit))
                {
                    spawnedBlock.transform.position = hit.point;
                }
            }

            // Apply selected color
            Renderer[] renderers = spawnedBlock.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                foreach (Material mat in renderer.materials)
                {
                    mat.color = selectedColor;
                }
            }

            // Apply default scale
            spawnedBlock.transform.localScale = blockData.defaultScale;

            // Record usage statistics
            if (BlockUsageTracker.Instance != null)
            {
                BlockUsageTracker.Instance.RecordBlockPlacement(
                    blockData.blockId,
                    blockData.blockName,
                    selectedColor
                );
            }

            // Record undo action
            if (UndoSystem.Instance != null)
            {
                UndoSystem.Instance.RecordPlacement(spawnedBlock);
            }

            Debug.Log($"Spawned {blockData.blockName} with color {selectedColor}");
        }

        private void OnDestroy()
        {
            HideGhostPreview();
        }
    }
}
