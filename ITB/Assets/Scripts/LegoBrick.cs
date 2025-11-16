using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minimal representation of a LEGO brick for snap-point management.
/// </summary>
public class LegoBrick : MonoBehaviour
{
    // Brick dimensions in studs (not meters). Studs are spaced 0.8m apart center-to-center
    // A 2x4 brick has 2 studs wide (1.5m total) and 4 studs long (3.15m total)
    // Total physical size = (studs - 1) * 0.8m + 0.7m (includes 0.35m edge margins)
    /// <summary>
    /// Brick width in studs.
    /// </summary>
    [SerializeField]
    private int width = 1;

    /// <summary>
    /// Brick length in studs.
    /// </summary>
    [SerializeField]
    private int length = 1;

    // Standard LEGO brick height in meters. Real LEGO bricks are 9.6mm, scaled to 0.2m for VR
    /// <summary>
    /// Brick height in meters.
    /// </summary>
    public float height = 0.2f;

    /// <summary>
    /// Prefab used to create snap points (studs/sockets) when generating or visualizing.
    /// </summary>
    public GameObject snapPointPrefab;

    /// <summary>
    /// List of stud (male) snap points belonging to this brick.
    /// </summary>
    public List<LegoSnapPoint> studSnapPoints = new List<LegoSnapPoint>();

    /// <summary>
    /// List of socket (female) snap points belonging to this brick.
    /// </summary>
    public List<LegoSnapPoint> socketSnapPoints = new List<LegoSnapPoint>();

    /// <summary>
    /// Sound played when this brick snaps into place.
    /// </summary>
    public AudioClip snapSound;

    /// <summary>
    /// If true, detect stud positions from mesh geometry instead of generating a grid based on width/length.
    /// </summary>
    [Tooltip("Detect stud positions from mesh geometry instead of grid generation")]
    public bool useDetectedStudPositions = false;

    // Private runtime fields

    private bool isBeingHeld = false;
    private Rigidbody rb;
    private List<LegoSnapPoint> potentialConnections = new List<LegoSnapPoint>();

    /// <summary>
    /// Ensure a Rigidbody is present on Awake (added if missing).
    /// </summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Default Rigidbody settings are left minimal; adjust externally as needed.
    }

    /// <summary>
    /// Initialize the brick and generate snap points on Start.
    /// </summary>
    private void Start()
    {
        GenerateSnapPoints();
    }

    /// <summary>
    /// Generate stud (top) and socket (bottom) snap points in a grid based on <see cref="width"/> and <see cref="length"/>.
    /// Instantiates <see cref="snapPointPrefab"/> as children, configures <see cref="LegoSnapPoint"/> values,
    /// adds a <see cref="SphereCollider"/> set as a trigger with radius <see cref="LegoSnapPoint.SNAP_RADIUS"/>,
    /// and registers this brick with <c>LegoSnapManager.Instance.RegisterBrick(this)</c> when available.
    /// </summary>
    public void GenerateSnapPoints()
    {
        // Clear any existing lists to avoid duplicate entries when regenerating
        studSnapPoints.Clear();
        socketSnapPoints.Clear();

        if (snapPointPrefab == null)
        {
            Debug.LogError("LegoBrick: snapPointPrefab is not assigned. Cannot generate snap points.");
            return;
        }

        List<Vector3> studPositions = null;
        
        // Choose generation method based on toggle
        if (useDetectedStudPositions)
        {
            studPositions = DetectStudPositionsFromMesh();
            if (studPositions == null || studPositions.Count == 0)
            {
                Debug.LogWarning("LegoBrick: Mesh detection found no stud positions, falling back to grid generation.");
                useDetectedStudPositions = false;
            }
        }
        
        if (!useDetectedStudPositions)
        {
            // Grid generation mode
            studPositions = new List<Vector3>();
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < length; z++)
                {
                    float localX = (x - (width - 1) / 2f) * LegoSnapPoint.STUD_SPACING;
                    float localZ = (z - (length - 1) / 2f) * LegoSnapPoint.STUD_SPACING;
                    studPositions.Add(new Vector3(localX, height / 2f, localZ));
                }
            }
        }

        // Create stud snap points from positions
        for (int i = 0; i < studPositions.Count; i++)
        {
            Vector3 studLocalPos = studPositions[i];
            
            // STUD (top)
            GameObject studObj = Instantiate(snapPointPrefab, transform);
            studObj.name = $"Stud_{i}";
            studObj.transform.localPosition = studLocalPos;
            studObj.transform.localRotation = Quaternion.identity;

            LegoSnapPoint studPoint = studObj.GetComponent<LegoSnapPoint>();
            if (studPoint == null)
                studPoint = studObj.AddComponent<LegoSnapPoint>();

            studPoint.type = LegoSnapPoint.SnapPointType.Stud;
            studPoint.parentBrick = this;
            studPoint.isConnected = false;
            studPoint.connectedTo = null;

            // Ensure a SphereCollider exists for snapping radius
            SphereCollider studCol = studObj.GetComponent<SphereCollider>();
            if (studCol == null)
                studCol = studObj.AddComponent<SphereCollider>();
            studCol.radius = LegoSnapPoint.SNAP_RADIUS;
            studCol.isTrigger = true;

            studSnapPoints.Add(studPoint);

            // SOCKET (bottom) - positioned at half height lower than studs
            Vector3 sockLocalPos = new Vector3(studLocalPos.x, 0f, studLocalPos.z);
            GameObject sockObj = Instantiate(snapPointPrefab, transform);
            sockObj.name = $"Socket_{i}";
            sockObj.transform.localPosition = sockLocalPos;
            sockObj.transform.localRotation = Quaternion.identity;

            LegoSnapPoint sockPoint = sockObj.GetComponent<LegoSnapPoint>();
            if (sockPoint == null)
                sockPoint = sockObj.AddComponent<LegoSnapPoint>();

            sockPoint.type = LegoSnapPoint.SnapPointType.Socket;
            sockPoint.parentBrick = this;
            sockPoint.isConnected = false;
            sockPoint.connectedTo = null;

            SphereCollider sockCol = sockObj.GetComponent<SphereCollider>();
            if (sockCol == null)
                sockCol = sockObj.AddComponent<SphereCollider>();
            sockCol.radius = LegoSnapPoint.SNAP_RADIUS;
            sockCol.isTrigger = true;

            socketSnapPoints.Add(sockPoint);
        }

        // Register with the snap manager if present
        try
        {
            if (LegoSnapManager.Instance != null)
            {
                LegoSnapManager.Instance.RegisterBrick(this);
            }
        }
        catch
        {
            // If the manager type isn't present or instance access fails, silently continue.
        }
    }

    /// <summary>
    /// Detects stud positions from the mesh geometry by finding vertices on the top surface,
    /// clustering them by XZ position, and validating spacing.
    /// </summary>
    /// <returns>List of detected stud positions in local space, or null if detection failed.</returns>
    private List<Vector3> DetectStudPositionsFromMesh()
    {
        MeshFilter meshFilter = GetComponentInChildren<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning("LegoBrick: No MeshFilter found for stud detection.");
            return null;
        }

        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        
        if (vertices.Length == 0)
        {
            Debug.LogWarning("LegoBrick: Mesh has no vertices.");
            return null;
        }

        // Transform vertices to world space and find max Y
        List<Vector3> worldVertices = new List<Vector3>();
        float maxY = float.MinValue;
        
        foreach (var v in vertices)
        {
            Vector3 worldPos = meshFilter.transform.TransformPoint(v);
            worldVertices.Add(worldPos);
            if (worldPos.y > maxY)
                maxY = worldPos.y;
        }

        // Find vertices on top surface (within tolerance of max Y)
        const float topSurfaceTolerance = 0.05f;
        List<Vector3> topVertices = new List<Vector3>();
        
        foreach (var v in worldVertices)
        {
            if (Mathf.Abs(v.y - maxY) <= topSurfaceTolerance)
            {
                topVertices.Add(v);
            }
        }

        if (topVertices.Count == 0)
        {
            Debug.LogWarning("LegoBrick: No top surface vertices found.");
            return null;
        }

        // Cluster top vertices by XZ position
        const float clusterTolerance = 0.2f;
        List<Vector3> clusterCenters = new List<Vector3>();
        List<bool> processed = new List<bool>(new bool[topVertices.Count]);

        for (int i = 0; i < topVertices.Count; i++)
        {
            if (processed[i]) continue;

            List<Vector3> cluster = new List<Vector3>();
            cluster.Add(topVertices[i]);
            processed[i] = true;

            // Find all nearby vertices in XZ plane
            for (int j = i + 1; j < topVertices.Count; j++)
            {
                if (processed[j]) continue;

                float distXZ = Vector2.Distance(
                    new Vector2(topVertices[i].x, topVertices[i].z),
                    new Vector2(topVertices[j].x, topVertices[j].z)
                );

                if (distXZ < clusterTolerance)
                {
                    cluster.Add(topVertices[j]);
                    processed[j] = true;
                }
            }

            // Calculate cluster center
            Vector3 center = Vector3.zero;
            foreach (var v in cluster)
            {
                center += v;
            }
            center /= cluster.Count;
            clusterCenters.Add(center);
        }

        Debug.LogFormat("LegoBrick: Detected {0} stud positions from mesh.", clusterCenters.Count);

        // Validate spacing between detected studs
        const float expectedSpacing = LegoSnapPoint.STUD_SPACING;
        const float spacingTolerance = 0.1f; // 10cm tolerance
        bool spacingValid = true;

        for (int i = 0; i < clusterCenters.Count; i++)
        {
            for (int j = i + 1; j < clusterCenters.Count; j++)
            {
                float distXZ = Vector2.Distance(
                    new Vector2(clusterCenters[i].x, clusterCenters[i].z),
                    new Vector2(clusterCenters[j].x, clusterCenters[j].z)
                );

                // Check if distance is close to expected spacing or multiples of it
                float ratio = distXZ / expectedSpacing;
                float nearestMultiple = Mathf.Round(ratio);
                float error = Mathf.Abs(distXZ - (nearestMultiple * expectedSpacing));

                if (error > spacingTolerance && nearestMultiple >= 1)
                {
                    Debug.LogWarningFormat(
                        "LegoBrick: Stud spacing mismatch - expected multiple of {0}m, got {1}m (error: {2}m)",
                        expectedSpacing, distXZ, error
                    );
                    spacingValid = false;
                }
            }
        }

        if (!spacingValid)
        {
            Debug.LogWarning("LegoBrick: Detected stud positions do not match expected 0.8m spacing.");
        }

        // Convert world positions to local space relative to this brick
        List<Vector3> localPositions = new List<Vector3>();
        foreach (var worldPos in clusterCenters)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPos);
            // Ensure Y is at expected height
            localPos.y = height / 2f;
            localPositions.Add(localPos);
        }

        return localPositions;
    }

    /// <summary>
    /// Public accessors for width and length (in studs).
    /// </summary>
    public int Width => width;
    public int Length => length;

    /// <summary>
    /// Whether the brick is currently being held (dragged) by the user.
    /// </summary>
    public bool IsBeingHeld { get => isBeingHeld; set => isBeingHeld = value; }

    /// <summary>
    /// The Rigidbody attached to this brick.
    /// </summary>
    public Rigidbody Rb => rb;

    /// <summary>
    /// Potential nearby snap points found during connection checks.
    /// </summary>
    public List<LegoSnapPoint> PotentialConnections => potentialConnections;

    /// <summary>
    /// Called when the user grabs (picks up) this brick.
    /// Sets the brick to kinematic while held and disconnects any existing snap connections.
    /// </summary>
    public void OnGrabbed()
    {
        isBeingHeld = true;
        if (rb != null)
            rb.isKinematic = true;

        DisconnectAll();
    }

    /// <summary>
    /// Called when the user releases this brick.
    /// Clears the held flag and attempts to snap to nearby snap points.
    /// </summary>
    public void OnReleased()
    {
        isBeingHeld = false;

        TrySnapToNearby();
    }

    /// <summary>
    /// Disconnects all current socket connections on this brick.
    /// Resets connected flags and parent relationships, and re-enables physics.
    /// </summary>
    private void DisconnectAll()
    {
        // Loop over sockets and disconnect any that are connected
        foreach (var socket in socketSnapPoints)
        {
            if (socket == null)
                continue;

            if (socket.isConnected)
            {
                var other = socket.connectedTo;
                // Disconnect the socket
                socket.isConnected = false;
                socket.connectedTo = null;

                // Disconnect the other snap point if present
                if (other != null)
                {
                    other.isConnected = false;
                    other.connectedTo = null;
                }
            }
        }

        // Detach from any parent so it can be moved independently
        transform.SetParent(null);

        // Re-enable physics on this brick
        if (rb != null)
            rb.isKinematic = false;
    }

    /// <summary>
    /// Attempts to snap this brick to nearby compatible snap points.
    /// </summary>
    private void TrySnapToNearby()
    {
        // Clear previous candidates
        potentialConnections.Clear();

        // Ensure the snap manager exists
        if (LegoSnapManager.Instance == null)
        {
            return;
        }

        // Loop through each socket on this brick
        foreach (var socket in socketSnapPoints)
        {
            if (socket == null)
                continue;

            // Only consider sockets that are not already connected
            if (socket.isConnected)
                continue;

            // Find nearby studs using the snap manager
            List<LegoSnapPoint> nearbyStuds = null;
            try
            {
                nearbyStuds = LegoSnapManager.Instance.FindNearbySnapPoints(
                    socket.transform.position, 
                    LegoSnapPoint.SNAP_RADIUS, 
                    LegoSnapPoint.SnapPointType.Stud);
            }
            catch
            {
                // If the manager call fails for any reason, skip this socket
                continue;
            }

            if (nearbyStuds == null)
                continue;

            foreach (var stud in nearbyStuds)
            {
                if (stud == null)
                    continue;

                // Skip already connected studs and studs that belong to this same brick
                if (stud.isConnected)
                    continue;

                if (stud.parentBrick == this)
                    continue;

                // Add stud and the socket as a potential connection pair (alternating)
                potentialConnections.Add(stud);
                potentialConnections.Add(socket);
            }
        }

        // If we have at least one pair (stud+socket), perform snapping
        if (potentialConnections.Count >= 2)
        {
            PerformSnap();
        }
    }

    /// <summary>
    /// Performs the snap action using the collected potentialConnections.
    /// Aligns this brick to the target brick and establishes connections.
    /// </summary>
    private void PerformSnap()
    {
        if (potentialConnections == null || potentialConnections.Count < 2)
            return;

        // The list is alternating [stud, socket, stud, socket, ...]
        LegoSnapPoint firstStud = potentialConnections[0];
        LegoSnapPoint firstSocket = potentialConnections.Count > 1 ? potentialConnections[1] : null;
        if (firstStud == null || firstSocket == null)
            return;

        var targetBrick = firstStud.parentBrick;
        if (targetBrick == null)
            return;

        // Align this brick so the first socket matches the first stud
        Vector3 offset = firstStud.transform.position - firstSocket.transform.position;
        transform.position += offset;

        // Parent to the target brick for simple alignment
        transform.SetParent(targetBrick.transform);

        // Keep the brick kinematic while snapped
        if (rb != null)
            rb.isKinematic = true;

        // Loop through potential connections in pairs and finalize those within snap radius
        for (int i = 0; i + 1 < potentialConnections.Count; i += 2)
        {
            var stud = potentialConnections[i];
            var socket = potentialConnections[i + 1];
            if (stud == null || socket == null)
                continue;

            float dist = Vector3.Distance(stud.transform.position, socket.transform.position);
            if (dist <= LegoSnapPoint.SNAP_RADIUS)
            {
                stud.isConnected = true;
                stud.connectedTo = socket;

                socket.isConnected = true;
                socket.connectedTo = stud;

                // Inform manager if available
                try
                {
                    if (LegoSnapManager.Instance != null)
                    {
                        LegoSnapManager.Instance.LogConnection(stud, socket);
                    }
                }
                catch
                {
                    // ignore if manager or method not present
                }
            }
        }

        // Play snap sound at this brick's position
        if (snapSound != null)
        {
            AudioSource.PlayClipAtPoint(snapSound, transform.position);
        }

        // Haptic feedback: if this brick is currently held by an XR controller, send a short impulse
        try
        {
            // Use reflection to avoid compile-time dependency on XR Interaction Toolkit
            var grabType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
            if (grabType != null)
            {
                var grab = GetComponent(grabType);
                if (grab != null)
                {
                    var isSelectedProp = grabType.GetProperty("isSelected");
                    var firstInteractorProp = grabType.GetProperty("firstInteractorSelecting");
                    
                    if (isSelectedProp != null && firstInteractorProp != null)
                    {
                        bool isSelected = (bool)isSelectedProp.GetValue(grab);
                        if (isSelected)
                        {
                            var interactor = firstInteractorProp.GetValue(grab);
                            if (interactor != null)
                            {
                                var sendHapticMethod = interactor.GetType().GetMethod("SendHapticImpulse", 
                                    new System.Type[] { typeof(float), typeof(float) });
                                if (sendHapticMethod != null)
                                {
                                    sendHapticMethod.Invoke(interactor, new object[] { 0.5f, 0.2f });
                                }
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore if XR Interaction Toolkit types are not available or something goes wrong
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Manual context menu command to generate snap points in the editor.
    /// Right-click the LegoBrick component in Inspector and select "Generate Snap Points Now".
    /// </summary>
    [ContextMenu("Generate Snap Points Now")]
    public void GenerateSnapPointsManual()
    {
        // Clear existing snap points
        LegoSnapPoint[] existingPoints = GetComponentsInChildren<LegoSnapPoint>();
        foreach (var point in existingPoints)
        {
            UnityEditor.EditorApplication.delayCall += () => 
            {
                if (point != null)
                    DestroyImmediate(point.gameObject);
            };
        }
        
        // Small delay to ensure cleanup completes before generation
        UnityEditor.EditorApplication.delayCall += () =>
        {
            // Generate new snap points
            GenerateSnapPoints();
            
            Debug.Log($"<color=green>✓ Generated {studSnapPoints.Count} studs and {socketSnapPoints.Count} sockets for {name}</color>");
        };
    }
#endif
}