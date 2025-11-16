using System.Collections.Generic;
using UnityEngine;

namespace LegoBuilder.BlockSystem
{
    /// <summary>
    /// Simple undo system that tracks spawned blocks and allows undoing the last placement
    /// </summary>
    public class SimpleUndoSystem : MonoBehaviour
    {
        private static SimpleUndoSystem _instance;
        public static SimpleUndoSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SimpleUndoSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SimpleUndoSystem");
                        _instance = go.AddComponent<SimpleUndoSystem>();
                    }
                }
                return _instance;
            }
        }

        [Tooltip("Maximum number of undo actions to keep in history")]
        [SerializeField]
        private int maxUndoHistory = 20;

        private Stack<GameObject> undoStack = new Stack<GameObject>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Registers a spawned object for undo tracking
        /// </summary>
        public void RegisterSpawnedObject(GameObject obj)
        {
            if (obj == null) return;

            undoStack.Push(obj);

            // Limit stack size
            if (undoStack.Count > maxUndoHistory)
            {
                // Remove oldest item (at bottom of stack)
                var tempStack = new Stack<GameObject>();
                var items = undoStack.ToArray();

                for (int i = 0; i < maxUndoHistory; i++)
                {
                    tempStack.Push(items[i]);
                }

                undoStack = tempStack;
            }

            Debug.Log($"[UndoSystem] Registered object: {obj.name}. Stack size: {undoStack.Count}");
        }

        /// <summary>
        /// Undoes the last placement by destroying the most recently spawned object
        /// </summary>
        public bool Undo()
        {
            if (undoStack.Count == 0)
            {
                Debug.Log("[UndoSystem] Nothing to undo");
                return false;
            }

            GameObject obj = undoStack.Pop();

            if (obj != null)
            {
                Debug.Log($"[UndoSystem] Undoing: {obj.name}");
                Destroy(obj);
                return true;
            }
            else
            {
                // Object was already destroyed, try next one
                Debug.LogWarning("[UndoSystem] Object was already destroyed, trying next");
                return Undo();
            }
        }

        /// <summary>
        /// Clears all undo history
        /// </summary>
        public void ClearHistory()
        {
            undoStack.Clear();
            Debug.Log("[UndoSystem] History cleared");
        }

        /// <summary>
        /// Gets whether there are any actions to undo
        /// </summary>
        public bool CanUndo => undoStack.Count > 0;

        /// <summary>
        /// Gets the current undo stack size
        /// </summary>
        public int UndoCount => undoStack.Count;
    }
}
