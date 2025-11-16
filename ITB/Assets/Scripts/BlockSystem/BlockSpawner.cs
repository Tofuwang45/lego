using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace LegoBuilder.BlockSystem
{
    /// <summary>
    /// Spawns blocks in the VR environment and registers them with the undo system
    /// </summary>
    public class BlockSpawner : MonoBehaviour
    {
        private static BlockSpawner _instance;
        public static BlockSpawner Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<BlockSpawner>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("BlockSpawner");
                        _instance = go.AddComponent<BlockSpawner>();
                    }
                }
                return _instance;
            }
        }

        [Header("Spawn Settings")]
        [Tooltip("Camera to use for spawn position calculation (usually Main Camera)")]
        [SerializeField]
        private Camera mainCamera;

        [Tooltip("Distance in front of camera to spawn blocks")]
        [SerializeField]
        private float spawnDistance = 0.5f;

        [Tooltip("Parent transform for spawned blocks (optional)")]
        [SerializeField]
        private Transform spawnParent;

        [Tooltip("Make spawned blocks interactable (grabbable)")]
        [SerializeField]
        private bool makeInteractable = true;

        [Tooltip("Scale factor for spawned blocks")]
        [SerializeField]
        private float blockScale = 0.1f;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Find main camera if not assigned
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogWarning("[BlockSpawner] Main camera not found!");
                }
            }
        }

        /// <summary>
        /// Spawns a block from the given BlockData
        /// </summary>
        public GameObject SpawnBlock(BlockData blockData)
        {
            if (blockData == null || blockData.blockPrefab == null)
            {
                Debug.LogError("[BlockSpawner] Invalid block data or missing prefab");
                return null;
            }

            // Calculate spawn position in front of camera
            Vector3 spawnPosition = GetSpawnPosition();
            Quaternion spawnRotation = GetSpawnRotation();

            // Instantiate the block
            GameObject spawnedBlock = Instantiate(blockData.blockPrefab, spawnPosition, spawnRotation, spawnParent);
            spawnedBlock.name = $"{blockData.blockName} (Spawned)";

            // Apply scale
            spawnedBlock.transform.localScale = Vector3.one * blockScale;

            // Make it interactable if needed
            if (makeInteractable)
            {
                EnsureInteractable(spawnedBlock);
            }

            // Register with undo system
            SimpleUndoSystem.Instance.RegisterSpawnedObject(spawnedBlock);

            Debug.Log($"[BlockSpawner] Spawned {blockData.blockName} at {spawnPosition}");

            return spawnedBlock;
        }

        /// <summary>
        /// Gets the spawn position in front of the camera
        /// </summary>
        private Vector3 GetSpawnPosition()
        {
            if (mainCamera != null)
            {
                return mainCamera.transform.position + mainCamera.transform.forward * spawnDistance;
            }
            else
            {
                // Fallback to world origin
                return Vector3.forward * spawnDistance;
            }
        }

        /// <summary>
        /// Gets the spawn rotation facing the user
        /// </summary>
        private Quaternion GetSpawnRotation()
        {
            if (mainCamera != null)
            {
                // Face the camera
                return Quaternion.LookRotation(mainCamera.transform.forward);
            }
            else
            {
                return Quaternion.identity;
            }
        }

        /// <summary>
        /// Ensures the spawned block has the necessary components to be grabbable
        /// </summary>
        private void EnsureInteractable(GameObject obj)
        {
            // Add XRGrabInteractable if not present
            if (obj.GetComponent<XRGrabInteractable>() == null)
            {
                var grab = obj.AddComponent<XRGrabInteractable>();
                grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
                grab.throwOnDetach = false;
            }

            // Add Rigidbody if not present
            if (obj.GetComponent<Rigidbody>() == null)
            {
                var rb = obj.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = false;
            }

            // Add Collider if not present
            if (obj.GetComponent<Collider>() == null)
            {
                var collider = obj.AddComponent<BoxCollider>();
                Debug.LogWarning($"[BlockSpawner] Added default BoxCollider to {obj.name}. You may want to adjust or replace it.");
            }
        }

        /// <summary>
        /// Sets the main camera reference (useful for runtime setup)
        /// </summary>
        public void SetCamera(Camera cam)
        {
            mainCamera = cam;
        }

        /// <summary>
        /// Sets the spawn distance
        /// </summary>
        public void SetSpawnDistance(float distance)
        {
            spawnDistance = distance;
        }
    }
}
