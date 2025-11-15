using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detects which bricks this brick is connected to using raycasting.
/// Solves the "bridge scenario" where one brick connects to multiple bricks below.
/// </summary>
public class BrickScanner : MonoBehaviour
{
    [Header("Scan Points Configuration")]
    [Tooltip("Transform points representing the bottom tube locations of this brick")]
    public List<Transform> bottomTubePoints = new List<Transform>();

    [Header("Scan Settings")]
    [Tooltip("How far down to raycast (in Unity units)")]
    [Range(0.01f, 0.2f)]
    public float scanDistance = 0.05f;

    [Tooltip("Layer mask for detecting other bricks")]
    public LayerMask brickLayerMask = ~0; // Default to all layers

    [Header("Debug Visualization")]
    [Tooltip("Draw debug rays in Scene view")]
    public bool showDebugRays = true;

    [Tooltip("Color of debug rays")]
    public Color debugRayColor = Color.yellow;

    private BrickIdentifier brickIdentifier;

    private void Awake()
    {
        brickIdentifier = GetComponent<BrickIdentifier>();
        if (brickIdentifier == null)
        {
            Debug.LogError($"BrickScanner on {gameObject.name} requires a BrickIdentifier component!");
        }

        // Auto-populate bottom tube points if empty
        if (bottomTubePoints.Count == 0)
        {
            AutoDetectTubePoints();
        }
    }

    /// <summary>
    /// Scan for all bricks that this brick is connected to
    /// Returns a list of unique brick IDs
    /// </summary>
    public List<string> GetConnectedBricks()
    {
        List<string> foundIDs = new List<string>();

        if (bottomTubePoints.Count == 0)
        {
            Debug.LogWarning($"No bottom tube points configured on {gameObject.name}");
            return foundIDs;
        }

        foreach (Transform point in bottomTubePoints)
        {
            if (point == null) continue;

            Vector3 rayStart = point.position;
            Vector3 rayDirection = -transform.up;

            // Cast a short ray downward
            if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, scanDistance, brickLayerMask))
            {
                // Check if we hit another brick
                BrickIdentifier otherBrick = hit.collider.GetComponentInParent<BrickIdentifier>();

                if (otherBrick != null && otherBrick != brickIdentifier)
                {
                    // Add unique IDs only
                    if (!foundIDs.Contains(otherBrick.uniqueID))
                    {
                        foundIDs.Add(otherBrick.uniqueID);

                        if (showDebugRays)
                        {
                            Debug.DrawRay(rayStart, rayDirection * hit.distance, Color.green, 2f);
                        }
                    }
                }
            }
            else if (showDebugRays)
            {
                Debug.DrawRay(rayStart, rayDirection * scanDistance, debugRayColor, 2f);
            }
        }

        return foundIDs;
    }

    /// <summary>
    /// Get connected brick names (for debugging)
    /// </summary>
    public List<string> GetConnectedBrickNames()
    {
        List<string> foundIDs = GetConnectedBricks();
        List<string> brickNames = new List<string>();

        foreach (string id in foundIDs)
        {
            // Find the brick with this ID in the scene
            BrickIdentifier[] allBricks = FindObjectsOfType<BrickIdentifier>();
            foreach (var brick in allBricks)
            {
                if (brick.uniqueID == id)
                {
                    brickNames.Add(brick.brickName);
                    break;
                }
            }
        }

        return brickNames;
    }

    /// <summary>
    /// Auto-detect tube points based on child objects named "TubePoint" or similar
    /// </summary>
    private void AutoDetectTubePoints()
    {
        Transform[] children = GetComponentsInChildren<Transform>();

        foreach (Transform child in children)
        {
            if (child == transform) continue; // Skip self

            // Look for objects with "tube", "point", or "socket" in their name
            string lowerName = child.name.ToLower();
            if (lowerName.Contains("tube") || lowerName.Contains("point") || lowerName.Contains("socket"))
            {
                bottomTubePoints.Add(child);
            }
        }

        if (bottomTubePoints.Count > 0)
        {
            Debug.Log($"Auto-detected {bottomTubePoints.Count} tube points on {gameObject.name}");
        }
    }

    /// <summary>
    /// Manual method to create tube points at runtime based on brick dimensions
    /// Call this if you don't have pre-configured tube point transforms
    /// </summary>
    public void CreateTubePointsFromDimensions()
    {
        if (brickIdentifier == null) return;

        // Clear existing points
        foreach (Transform point in bottomTubePoints)
        {
            if (point != null && point.gameObject.name.StartsWith("GeneratedTubePoint"))
            {
                Destroy(point.gameObject);
            }
        }
        bottomTubePoints.Clear();

        // Create new points based on brick dimensions
        float studSpacing = 0.008f; // Standard LEGO stud spacing in meters (8mm)
        float bottomOffset = -0.0096f; // Height offset to bottom of brick

        int length = brickIdentifier.studsLength;
        int width = brickIdentifier.studsWidth;

        for (int x = 0; x < length; x++)
        {
            for (int z = 0; z < width; z++)
            {
                GameObject tubePoint = new GameObject($"GeneratedTubePoint_{x}_{z}");
                tubePoint.transform.SetParent(transform);

                // Center the grid
                float xPos = (x - (length - 1) / 2f) * studSpacing;
                float zPos = (z - (width - 1) / 2f) * studSpacing;

                tubePoint.transform.localPosition = new Vector3(xPos, bottomOffset, zPos);
                tubePoint.transform.localRotation = Quaternion.identity;

                bottomTubePoints.Add(tubePoint.transform);
            }
        }

        Debug.Log($"Created {bottomTubePoints.Count} tube points for {brickIdentifier.brickName}");
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugRays) return;

        // Visualize tube points in editor
        Gizmos.color = debugRayColor;

        foreach (Transform point in bottomTubePoints)
        {
            if (point == null) continue;

            Vector3 start = point.position;
            Vector3 end = start + (-transform.up * scanDistance);

            Gizmos.DrawSphere(start, 0.002f);
            Gizmos.DrawLine(start, end);
        }
    }

    /// <summary>
    /// Context menu helper to test scanning
    /// </summary>
    [ContextMenu("Test Scan")]
    public void TestScan()
    {
        List<string> connected = GetConnectedBrickNames();

        if (connected.Count == 0)
        {
            Debug.Log($"{brickIdentifier.brickName}: No connected bricks found");
        }
        else
        {
            Debug.Log($"{brickIdentifier.brickName} is connected to: {string.Join(", ", connected)}");
        }
    }
}
