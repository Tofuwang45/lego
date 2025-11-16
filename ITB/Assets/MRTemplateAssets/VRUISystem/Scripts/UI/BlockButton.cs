using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;
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
        private GameObject ghostPreview;
        private Color selectedColor = Color.red;
        private bool isHovering = false;
        private Vector3 lastHitPosition = Vector3.zero;
        private Vector3 lastHitNormal = Vector3.up;

        /// <summary>
        /// Initialize the button with block data
        /// </summary>
        public void Initialize(BlockData data)
        {
            blockData = data;

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
            
            // Update position with stored hit info
            if (isHovering)
            {
                UpdateGhostPosition(lastHitPosition, lastHitNormal);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
            // Store the hit information from event data
            if (eventData.worldPosition != Vector3.zero)
            {
                lastHitPosition = eventData.worldPosition;
            }
            ShowGhostPreview();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            HideGhostPreview();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Update hit position from the click event
            if (eventData.worldPosition != Vector3.zero)
            {
                lastHitPosition = eventData.worldPosition;
            }
            SpawnBlock();
        }

        private void Update()
        {
            // Ghost preview position is updated via event data in OnPointerEnter
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

        private void UpdateGhostPosition(Vector3 hitPosition, Vector3 hitNormal)
        {
            if (ghostPreview == null) return;

            // Position ghost at the hit point with offset along the normal
            ghostPreview.transform.position = hitPosition + hitNormal * ghostDistance;
            ghostPreview.transform.rotation = Quaternion.LookRotation(hitNormal);
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

            // Position it at the ghost location
            if (ghostPreview != null)
            {
                spawnedBlock.transform.position = ghostPreview.transform.position;
                spawnedBlock.transform.rotation = ghostPreview.transform.rotation;
            }
            else
            {
                // Fallback to last known hit position
                spawnedBlock.transform.position = lastHitPosition + lastHitNormal * ghostDistance;
                spawnedBlock.transform.rotation = Quaternion.LookRotation(lastHitNormal);
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
