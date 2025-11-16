using UnityEngine;

public class UniversalLegoSnap : MonoBehaviour
{
    [Header("Connection Points")]
    public Transform[] connectors;  // Universal connection points on all sides
    
    [Header("Snap Settings")]
    public float snapDistance = 0.1f;
    public bool useGridSnapping = true;
    public float gridSize = 0.016f;  // LEGO standard unit
    
    [Header("Unsnap Settings")]
    public float pullApartDistance = 0.05f;
    public float pullApartForce = 5f;
    
    [Header("Audio")]
    public AudioClip snapSound;
    public AudioClip unsnapSound;
    
    private Rigidbody rb;
    private AudioSource audioSource;
    private bool isSnapped = false;
    private FixedJoint snapJoint;
    private UniversalLegoSnap snappedToBrick = null;
    private Vector3 snapPosition;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        
        // Auto-find all connectors
        if (connectors.Length == 0)
        {
            connectors = FindChildrenByName("connector");
            Debug.Log($"Found {connectors.Length} connectors on {gameObject.name}");
        }
    }
    
    void Update()
    {
        // Try to snap when brick is slow/stationary
        if (rb != null && rb.linearVelocity.magnitude < 0.5f && !isSnapped)
        {
            TrySnap();
        }
        
        // Check if being pulled apart
        if (isSnapped && snappedToBrick != null)
        {
            CheckPullApart();
        }
    }
    
    void CheckPullApart()
    {
        // Distance-based detection
        float currentDistance = Vector3.Distance(transform.position, snapPosition);
        if (currentDistance > pullApartDistance)
        {
            Unsnap();
            return;
        }
        
        // Force-based detection (two-handed pull)
        if (snapJoint != null)
        {
            Rigidbody otherRb = snappedToBrick.GetComponent<Rigidbody>();
            
            // If both bricks are being grabbed (non-kinematic)
            if (!rb.isKinematic && otherRb != null && !otherRb.isKinematic)
            {
                // Check if moving in opposite directions
                float velocityDifference = Vector3.Distance(rb.linearVelocity, otherRb.linearVelocity);
                if (velocityDifference > pullApartForce)
                {
                    Unsnap();
                }
            }
        }
    }
    
    void TrySnap()
    {
        UniversalLegoSnap[] allBricks = FindObjectsOfType<UniversalLegoSnap>();
        Transform bestMyConnector = null;
        Transform bestTheirConnector = null;
        float closestDistance = snapDistance;
        
        // Check each of my connectors against all other bricks' connectors
        foreach (var myConnector in connectors)
        {
            if (myConnector == null) continue;
            
            foreach (var otherBrick in allBricks)
            {
                if (otherBrick == this) continue;
                
                foreach (var theirConnector in otherBrick.connectors)
                {
                    if (theirConnector == null) continue;
                    
                    float dist = Vector3.Distance(myConnector.position, theirConnector.position);
                    
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        bestMyConnector = myConnector;
                        bestTheirConnector = theirConnector;
                    }
                }
            }
        }
        
        if (bestMyConnector != null && bestTheirConnector != null)
        {
            SnapTo(bestMyConnector, bestTheirConnector);
        }
    }
    
    void SnapTo(Transform myConnector, Transform theirConnector)
    {
        Transform targetBrick = theirConnector.GetComponentInParent<UniversalLegoSnap>().transform;
        snappedToBrick = targetBrick.GetComponent<UniversalLegoSnap>();
        
        // STEP 1: Calculate alignment
        // Determine if this is a top/bottom connection or side connection
        Vector3 connectDirection = (myConnector.position - theirConnector.position).normalized;
        float verticalAlignment = Mathf.Abs(Vector3.Dot(connectDirection, Vector3.up));
        
        // If mostly vertical (top/bottom connection), match rotation
        if (verticalAlignment > 0.7f)
        {
            transform.rotation = targetBrick.rotation;
        }
        else
        {
            // Side connection - try to face opposite
            Vector3 targetForward = -theirConnector.forward;
            Vector3 targetUp = Vector3.up;
            transform.rotation = Quaternion.LookRotation(targetForward, targetUp);
            
            // Snap rotation to 90-degree increments
            Vector3 euler = transform.eulerAngles;
            euler.y = Mathf.Round(euler.y / 90f) * 90f;
            transform.eulerAngles = euler;
        }
        
        // STEP 2: Position so connectors align perfectly
        Vector3 offsetFromConnectorToOrigin = transform.position - myConnector.position;
        Vector3 newPosition = theirConnector.position + offsetFromConnectorToOrigin;
        
        // STEP 3: Apply grid snapping for perfect alignment
        if (useGridSnapping)
        {
            newPosition.x = Mathf.Round(newPosition.x / gridSize) * gridSize;
            newPosition.y = Mathf.Round(newPosition.y / gridSize) * gridSize;
            newPosition.z = Mathf.Round(newPosition.z / gridSize) * gridSize;
        }
        
        transform.position = newPosition;
        snapPosition = newPosition;
        
        // STEP 4: Lock the brick
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        
        // STEP 5: Create physical connection
        if (snapJoint == null)
        {
            snapJoint = gameObject.AddComponent<FixedJoint>();
            snapJoint.connectedBody = targetBrick.GetComponent<Rigidbody>();
            snapJoint.breakForce = 50f;
            snapJoint.breakTorque = 50f;
        }
        
        isSnapped = true;
        
        // Play snap sound
        if (audioSource != null && snapSound != null)
        {
            audioSource.PlayOneShot(snapSound);
        }
        
        Debug.Log($"{gameObject.name} snapped to {targetBrick.name}");
    }
    
    // Called when joint breaks from force
    void OnJointBreak(float breakForce)
    {
        Debug.Log($"Joint broken with force: {breakForce}");
        Unsnap();
    }
    
    // Unsnap the connection
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
        snappedToBrick = null;
        
        // Play unsnap sound
        if (audioSource != null && unsnapSound != null)
        {
            audioSource.PlayOneShot(unsnapSound);
        }
        
        Debug.Log($"{gameObject.name} unsnapped!");
    }
    
    // Find all child objects matching a name pattern
    Transform[] FindChildrenByName(string namePattern)
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        System.Collections.Generic.List<Transform> matches = new System.Collections.Generic.List<Transform>();
        
        foreach (Transform child in allChildren)
        {
            if (child != transform && child.name.ToLower().Contains(namePattern.ToLower()))
            {
                matches.Add(child);
            }
        }
        
        return matches.ToArray();
    }
    
    // Visualize connectors in Scene view
    void OnDrawGizmos()
    {
        if (connectors == null || connectors.Length == 0)
        {
            // Try to find them even in edit mode
            connectors = FindChildrenByName("connector");
        }
        
        if (connectors != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var connector in connectors)
            {
                if (connector != null)
                {
                    // Draw sphere at connector position
                    Gizmos.DrawWireSphere(connector.position, 0.01f);
                    
                    // Draw arrow showing connector direction
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(connector.position, connector.forward * 0.02f);
                    Gizmos.color = Color.cyan;
                }
            }
        }
    }
    
    // Visualize snap range when selected
    void OnDrawGizmosSelected()
    {
        if (connectors != null)
        {
            foreach (var connector in connectors)
            {
                if (connector != null)
                {
                    Gizmos.color = new Color(1, 1, 0, 0.3f);
                    Gizmos.DrawWireSphere(connector.position, snapDistance);
                }
            }
        }
    }
}