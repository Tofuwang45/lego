using UnityEngine;

public class LegoSnapSystem : MonoBehaviour
{
    [Header("Snap Points")]
    public Transform[] studs;      // Top connection points
    public Transform[] sockets;    // Bottom connection points
    
    [Header("Snap Settings")]
    public float snapDistance = 0.15f;
    public float snapForce = 10f;
    public LayerMask legoLayer;
    
    [Header("Audio")]
    public AudioClip snapSound;
    
    private Rigidbody rb;
    private AudioSource audioSource;
    private bool isSnapped = false;
    private FixedJoint snapJoint;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        
        // Find all studs and sockets if not manually assigned
        if (studs.Length == 0)
        {
            studs = FindChildrenByName("stud");
        }
        if (sockets.Length == 0)
        {
            sockets = FindChildrenByName("socket");
        }
    }
    
    void Update()
    {
        // Only try to snap when moving slowly
        if (rb != null && rb.linearVelocity.magnitude < 0.5f && !isSnapped)
        {
            TrySnap();
        }
    }
    
    void TrySnap()
    {
        LegoSnapSystem[] allBricks = FindObjectsByType<LegoSnapSystem>(FindObjectsSortMode.None);
        Transform bestSocket = null;
        Transform bestStud = null;
        float closestDistance = snapDistance;
        
        // Check each of our sockets against all other bricks' studs
        foreach (var socket in sockets)
        {
            foreach (var otherBrick in allBricks)
            {
                if (otherBrick == this) continue;
                
                foreach (var stud in otherBrick.studs)
                {
                    float dist = Vector3.Distance(socket.position, stud.position);
                    
                    if (dist < closestDistance)
                    {
                        // Check if alignment is good (not at weird angle)
                        float angle = Vector3.Angle(socket.up, -stud.up);
                        if (angle < 30f)
                        {
                            closestDistance = dist;
                            bestSocket = socket;
                            bestStud = stud;
                        }
                    }
                }
            }
        }
        
        if (bestStud != null && bestSocket != null)
        {
            SnapTo(bestSocket, bestStud);
        }
    }
    
void SnapTo(Transform socket, Transform stud)
{
    // Find which socket is being used
    int socketIndex = System.Array.IndexOf(sockets, socket);
    
    // Calculate the offset between our socket and their stud
    Vector3 socketToStud = stud.position - socket.position;
    
    // Move the entire brick by this offset
    transform.position += socketToStud;
    
    // Align rotation to match the other brick (both should be on same grid)
    Vector3 targetRotation = stud.GetComponentInParent<LegoSnapSystem>().transform.eulerAngles;
    Vector3 euler = transform.eulerAngles;
    
    // Snap to nearest 90-degree increment matching the target brick
    euler.y = Mathf.Round(targetRotation.y / 90f) * 90f;
    transform.eulerAngles = euler;
    
    // Fine-tune: Snap to grid for perfect alignment
    float gridSize = 0.008f; // Standard LEGO unit
    Vector3 pos = transform.position;
    pos.x = Mathf.Round(pos.x / gridSize) * gridSize;
    pos.y = Mathf.Round(pos.y / gridSize) * gridSize;
    pos.z = Mathf.Round(pos.z / gridSize) * gridSize;
    transform.position = pos;
    
    // Stop all movement
    if (rb != null)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true; // Lock it completely
    }
    
    // Create physical connection
    if (snapJoint == null)
    {
        snapJoint = gameObject.AddComponent<FixedJoint>();
        snapJoint.connectedBody = stud.GetComponentInParent<Rigidbody>();
        snapJoint.breakForce = Mathf.Infinity;
    }
    
    isSnapped = true;
    
    if (audioSource != null && snapSound != null)
    {
        audioSource.PlayOneShot(snapSound);
    }
    
    Debug.Log($"{gameObject.name} snapped to {stud.parent.name}");
}
    
    // Helper function to find children by name pattern
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
    
    // Call this to unsnap (e.g., when picked up)
    public void Unsnap()
    {
        if (snapJoint != null)
        {
            Destroy(snapJoint);
        }
        isSnapped = false;
    }
    
    
    
}