using UnityEngine;

public class LegoSnapPerfect : MonoBehaviour
{
    [Header("Snap Points")]
    public Transform[] studs;      // Top connection points
    public Transform[] sockets;    // Bottom connection points
    
    [Header("Snap Settings")]
    public float snapDistance = 0.1f;
    public float snapSpeed = 10f;
    public bool useGridSnapping = true;
    public float gridSize = 0.008f;  // LEGO standard unit
    
    [Header("Audio")]
    public AudioClip snapSound;
    
    private Rigidbody rb;
    private AudioSource audioSource;
    private bool isSnapped = false;
    private FixedJoint snapJoint;
    private Transform snappedToStud = null;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        
        // Auto-find studs and sockets
        if (studs.Length == 0)
        {
            studs = FindChildrenByName("stud");
        }
        if (sockets.Length == 0)
        {
            sockets = FindChildrenByName("socket");
        }

        if (LegoSceneTracker.Instance != null)
        {
            LegoSceneTracker.Instance.RegisterSnapBrick(this);
        }
    }
    
    void Update()
    {
        if (rb != null && rb.linearVelocity.magnitude < 0.5f && !isSnapped)
        {
            TrySnap();
        }
    }
    
    void TrySnap()
    {
        LegoSnapPerfect[] allBricks = FindObjectsByType<LegoSnapPerfect>(FindObjectsSortMode.None);
        Transform bestSocket = null;
        Transform bestStud = null;
        float closestDistance = snapDistance;
        
        foreach (var socket in sockets)
        {
            foreach (var otherBrick in allBricks)
            {
                if (otherBrick == this || otherBrick.isSnapped) continue;
                
                foreach (var stud in otherBrick.studs)
                {
                    float dist = Vector3.Distance(socket.position, stud.position);
                    
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        bestSocket = socket;
                        bestStud = stud;
                    }
                }
            }
        }
        
        if (bestStud != null && bestSocket != null)
        {
            SnapToPerfect(bestSocket, bestStud);
        }
    }
    
    void SnapToPerfect(Transform socket, Transform stud)
    {
        // Get the brick we're snapping to
        Transform targetBrick = stud.GetComponentInParent<LegoSnapPerfect>().transform;
        
        // STEP 1: Match rotation exactly to target brick
        transform.rotation = targetBrick.rotation;
        
        // STEP 2: Calculate exact position offset
        // We want our socket to be exactly where their stud is
        Vector3 offsetFromSocketToOrigin = transform.position - socket.position;
        Vector3 newPosition = stud.position + offsetFromSocketToOrigin;
        
        // STEP 3: Apply grid snapping for perfect alignment
        if (useGridSnapping)
        {
            newPosition.x = Mathf.Round(newPosition.x / gridSize) * gridSize;
            newPosition.y = Mathf.Round(newPosition.y / gridSize) * gridSize;
            newPosition.z = Mathf.Round(newPosition.z / gridSize) * gridSize;
        }
        
        transform.position = newPosition;
        
        // STEP 4: Lock the brick completely
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        
        // STEP 5: Create unbreakable connection
        if (snapJoint == null)
        {
            snapJoint = gameObject.AddComponent<FixedJoint>();
            snapJoint.connectedBody = targetBrick.GetComponent<Rigidbody>();
            snapJoint.breakForce = Mathf.Infinity;
            snapJoint.breakTorque = Mathf.Infinity;
        }
        
        isSnapped = true;
        snappedToStud = stud;
        
        // Play sound
        if (audioSource != null && snapSound != null)
        {
            audioSource.PlayOneShot(snapSound);
        }
        
        if (LegoSceneTracker.Instance != null)
        {
            LegoSceneTracker.Instance.CaptureSnapshot("SnapPerfect:Snap");
        }

        Debug.Log($"{gameObject.name} snapped perfectly to {targetBrick.name}");
    }
    
    // Call this when brick is grabbed/picked up
    public void Unsnap()
    {
        if (snapJoint != null)
        {
            Destroy(snapJoint);
        }
        
        if (rb != null)
        {
            rb.isKinematic = false;
        }
        
        isSnapped = false;
        snappedToStud = null;

        if (LegoSceneTracker.Instance != null)
        {
            LegoSceneTracker.Instance.CaptureSnapshot("SnapPerfect:Unsnap");
        }
    }
    
    Transform[] FindChildrenByName(string namePattern)
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        System.Collections.Generic.List<Transform> matches = new System.Collections.Generic.List<Transform>();
        
        foreach (Transform child in allChildren)
        {
            if (child.name.ToLower().Contains(namePattern.ToLower()))
            {
                matches.Add(child);
            }
        }
        
        return matches.ToArray();
    }
    
    // Visualize snap points
    void OnDrawGizmos()
    {
        if (studs != null)
        {
            Gizmos.color = Color.green;
            foreach (var stud in studs)
            {
                if (stud != null)
                    Gizmos.DrawWireSphere(stud.position, 0.01f);
            }
        }
        
        if (sockets != null)
        {
            Gizmos.color = Color.red;
            foreach (var socket in sockets)
            {
                if (socket != null)
                    Gizmos.DrawWireSphere(socket.position, 0.01f);
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (sockets != null)
        {
            foreach (var socket in sockets)
            {
                if (socket != null)
                {
                    Gizmos.color = new Color(1, 1, 0, 0.3f);
                    Gizmos.DrawWireSphere(socket.position, snapDistance);
                }
            }
        }
    }

    void OnDestroy()
    {
        if (LegoSceneTracker.Instance != null)
        {
            LegoSceneTracker.Instance.UnregisterSnapBrick(this);
        }
    }
}