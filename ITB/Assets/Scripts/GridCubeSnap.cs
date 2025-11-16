using System.Collections.Generic;
using UnityEngine;

public class GridCubeSnap : MonoBehaviour
{
    public float snapRange = 0.15f;
    public float gridSize = 0.5f;
    public float breakForce = 50f;

    private Rigidbody rb;
    private List<Transform> snapPoints = new List<Transform>();
    private FixedJoint joint;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Find all snap points automatically
        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains("snap"))
                snapPoints.Add(child);
        }
    }

    void Update()
    {
        // Only snap if cube is moving slowly
        if (rb.linearVelocity.magnitude < 0.2f)
        {
            TrySnap();
        }
    }

    void TrySnap()
    {
        GridCubeSnap[] allCubes = FindObjectsOfType<GridCubeSnap>();

        foreach (var otherCube in allCubes)
        {
            if (otherCube == this)
                continue;

            foreach (var myPoint in snapPoints)
            foreach (var otherPoint in otherCube.snapPoints)
            {
                float d = Vector3.Distance(myPoint.position, otherPoint.position);

                if (d < snapRange)
                {
                    SnapTo(otherCube, myPoint, otherPoint);
                    return;
                }
            }
        }
    }

    void SnapTo(GridCubeSnap targetCube, Transform myPoint, Transform targetPoint)
    {
        // Remove any old joint
        if (joint != null)
            Destroy(joint);

        // Match rotation first
        transform.rotation = targetCube.transform.rotation;

        // Calculate new position
        Vector3 offset = transform.position - myPoint.position;
        Vector3 newPos = targetPoint.position + offset;

        // Grid-align
        newPos.x = Mathf.Round(newPos.x / gridSize) * gridSize;
        newPos.y = Mathf.Round(newPos.y / gridSize) * gridSize;
        newPos.z = Mathf.Round(newPos.z / gridSize) * gridSize;

        // Apply position
        transform.position = newPos;

        // Lock pieces together
        joint = gameObject.AddComponent<FixedJoint>();
        joint.connectedBody = targetCube.rb;
        joint.breakForce = breakForce;
        joint.breakTorque = breakForce;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // Called by XR when you grab the cube
    public void Unsnap()
    {
        if (joint != null)
            Destroy(joint);
    }
}

