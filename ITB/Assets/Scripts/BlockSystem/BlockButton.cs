using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LegoBuilder.BlockSystem
{
    /// <summary>
    /// UI button component for selecting and spawning a block
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class BlockButton : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The button component")]
        [SerializeField]
        private Button button;

        [Tooltip("Image component for block icon")]
        [SerializeField]
        private Image iconImage;

        [Tooltip("Text component for block name (optional)")]
        [SerializeField]
        private TextMeshProUGUI nameText;

        [Tooltip("Background image to colorize if no icon")]
        [SerializeField]
        private Image backgroundImage;

        [Header("Block Data")]
        private BlockData blockData;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (button != null)
                button.onClick.AddListener(OnButtonClicked);
        }

        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(OnButtonClicked);
        }

        /// <summary>
        /// Initializes the button with block data
        /// </summary>
        public void Initialize(BlockData data)
        {
            blockData = data;

            if (blockData == null)
            {
                gameObject.SetActive(false);
                return;
            }

            // Set icon if available
            if (iconImage != null)
            {
                if (blockData.icon != null)
                {
                    iconImage.sprite = blockData.icon;
                    iconImage.gameObject.SetActive(true);
                }
                else
                {
                    iconImage.gameObject.SetActive(false);
                }
            }

            // Set name text if available
            if (nameText != null)
            {
                nameText.text = blockData.blockName;
            }

            // Set background color if no icon
            if (backgroundImage != null && blockData.icon == null)
            {
                backgroundImage.color = blockData.buttonColor;
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        private void OnButtonClicked()
        {
            if (blockData == null)
            {
                Debug.LogWarning("[BlockButton] No block data assigned!");
                return;
            }

            Debug.Log($"[BlockButton] Spawning block: {blockData.blockName}");

            // Spawn the block
            GameObject spawned = BlockSpawner.Instance.SpawnBlock(blockData);

            if (spawned != null)
            {
                // Optional: Add haptic feedback here
                Debug.Log($"[BlockButton] Successfully spawned {blockData.blockName}");
            }
        }

        /// <summary>
        /// Gets the current block data
        /// </summary>
        public BlockData BlockData => blockData;
    }
}
