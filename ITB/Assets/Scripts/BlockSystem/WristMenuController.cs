using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LegoBuilder.BlockSystem
{
    /// <summary>
    /// Controller for the wrist menu UI, handles undo button and menu interactions
    /// </summary>
    public class WristMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Undo button")]
        [SerializeField]
        private Button undoButton;

        [Tooltip("Optional text to show undo count")]
        [SerializeField]
        private TextMeshProUGUI undoCountText;

        [Tooltip("BlockGridUI component")]
        [SerializeField]
        private BlockGridUI blockGridUI;

        private void OnEnable()
        {
            if (undoButton != null)
                undoButton.onClick.AddListener(OnUndoClicked);
        }

        private void OnDisable()
        {
            if (undoButton != null)
                undoButton.onClick.RemoveListener(OnUndoClicked);
        }

        private void Update()
        {
            UpdateUndoButton();
        }

        /// <summary>
        /// Called when the undo button is clicked
        /// </summary>
        private void OnUndoClicked()
        {
            bool undone = SimpleUndoSystem.Instance.Undo();

            if (undone)
            {
                Debug.Log("[WristMenuController] Undo successful");
                // Optional: Add haptic feedback
            }
            else
            {
                Debug.Log("[WristMenuController] Nothing to undo");
            }
        }

        /// <summary>
        /// Updates the undo button's interactable state based on undo availability
        /// </summary>
        private void UpdateUndoButton()
        {
            if (undoButton == null)
                return;

            bool canUndo = SimpleUndoSystem.Instance.CanUndo;
            undoButton.interactable = canUndo;

            // Update text if available
            if (undoCountText != null)
            {
                int undoCount = SimpleUndoSystem.Instance.UndoCount;
                undoCountText.text = canUndo ? $"Undo ({undoCount})" : "Undo (0)";
            }
        }
    }
}
