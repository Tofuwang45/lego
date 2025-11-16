using System.Collections.Generic;
using UnityEngine;

namespace MRTemplateAssets.Scripts
{
    /// <summary>
    /// Manages undo functionality for block placement and deletion
    /// </summary>
    public class UndoSystem : MonoBehaviour
    {
        public static UndoSystem Instance { get; private set; }

        [System.Serializable]
        private class UndoAction
        {
            public enum ActionType
            {
                Placement,
                Deletion
            }

            public ActionType type;
            public GameObject targetObject;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public string prefabPath; // For recreating deleted objects
            public Color objectColor;
        }

        [Header("Undo Settings")]
        [Tooltip("Maximum number of actions to keep in history")]
        public int maxUndoHistory = 20;

        private Stack<UndoAction> undoStack = new Stack<UndoAction>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Record a block placement for undo
        /// </summary>
        public void RecordPlacement(GameObject placedObject)
        {
            if (placedObject == null) return;

            // Store the color before recording
            Color objectColor = Color.white;
            Renderer renderer = placedObject.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                objectColor = renderer.material.color;
            }

            UndoAction action = new UndoAction
            {
                type = UndoAction.ActionType.Placement,
                targetObject = placedObject,
                position = placedObject.transform.position,
                rotation = placedObject.transform.rotation,
                scale = placedObject.transform.localScale,
                objectColor = objectColor
            };

            PushAction(action);
        }

        /// <summary>
        /// Record a block deletion for undo
        /// </summary>
        public void RecordDeletion(GameObject deletedObject)
        {
            if (deletedObject == null) return;

            // Store object data before it's destroyed
            UndoAction action = new UndoAction
            {
                type = UndoAction.ActionType.Deletion,
                targetObject = deletedObject,
                position = deletedObject.transform.position,
                rotation = deletedObject.transform.rotation,
                scale = deletedObject.transform.localScale
            };

            // Try to get color from renderer
            Renderer renderer = deletedObject.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                action.objectColor = renderer.material.color;
            }

            // Deactivate instead of destroying immediately (for undo)
            deletedObject.SetActive(false);

            PushAction(action);
        }

        /// <summary>
        /// Undo the last action
        /// </summary>
        public void Undo()
        {
            if (undoStack.Count == 0)
            {
                Debug.Log("Nothing to undo");
                return;
            }

            UndoAction action = undoStack.Pop();

            switch (action.type)
            {
                case UndoAction.ActionType.Placement:
                    UndoPlacement(action);
                    break;

                case UndoAction.ActionType.Deletion:
                    UndoDeletion(action);
                    break;
            }
        }

        private void UndoPlacement(UndoAction action)
        {
            // Simply destroy the placed object
            if (action.targetObject != null)
            {
                Destroy(action.targetObject);
                Debug.Log("Undid block placement");
            }
        }

        private void UndoDeletion(UndoAction action)
        {
            // Reactivate the deleted object
            if (action.targetObject != null)
            {
                action.targetObject.SetActive(true);
                Debug.Log("Undid block deletion");
            }
            else
            {
                Debug.LogWarning("Cannot undo deletion: object was permanently destroyed");
            }
        }

        private void PushAction(UndoAction action)
        {
            undoStack.Push(action);

            // Maintain max history size
            if (undoStack.Count > maxUndoHistory)
            {
                // Remove oldest action
                var tempStack = new Stack<UndoAction>();
                while (undoStack.Count > 1)
                {
                    tempStack.Push(undoStack.Pop());
                }
                // Bottom item is now on top, discard it
                undoStack.Pop();
                // Restore the stack
                while (tempStack.Count > 0)
                {
                    undoStack.Push(tempStack.Pop());
                }
            }
        }

        /// <summary>
        /// Clear all undo history
        /// </summary>
        public void ClearHistory()
        {
            undoStack.Clear();
        }

        /// <summary>
        /// Check if there are actions to undo
        /// </summary>
        public bool CanUndo()
        {
            return undoStack.Count > 0;
        }

        /// <summary>
        /// Get the number of actions in the undo history
        /// </summary>
        public int GetUndoCount()
        {
            return undoStack.Count;
        }
    }
}
