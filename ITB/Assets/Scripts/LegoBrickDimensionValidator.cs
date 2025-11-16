using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Validator component that checks a `LegoBrick`'s actual mesh dimensions against
/// expected LEGO dimensions derived from the brick's stud counts.
/// </summary>
[RequireComponent(typeof(LegoBrick))]
public class LegoBrickDimensionValidator : MonoBehaviour
{
    private const float STUD_SPACING = 0.8f;   // meters between stud centers
    private const float EDGE_MARGINS = 0.35f;  // margins on each side (meters)
    private const float EXPECTED_HEIGHT = 0.2f; // expected brick height in meters
    private const float TOLERANCE = 0.05f;     // tolerance (meters) to warn about

    /// <summary>
    /// Validate the brick dimensions based on its `LegoBrick` width/length.
    /// Runs from the component context menu: right-click component header -> "Validate Brick Dimensions".
    /// </summary>
    [ContextMenu("Validate Brick Dimensions")]
    public void ValidateBrickDimensions()
    {
        var brick = GetComponent<LegoBrick>();
        if (brick == null)
        {
            Debug.LogError("LegoBrickDimensionValidator: No LegoBrick found on the GameObject.");
            return;
        }

        int width = brick.Width;
        int length = brick.Length;

        // Expected totals (meters): (count-1)*spacing + 2*margins => (count-1)*0.8 + 0.7
        float expectedTotalWidth = (width - 1) * STUD_SPACING + (EDGE_MARGINS * 2f);
        float expectedTotalLength = (length - 1) * STUD_SPACING + (EDGE_MARGINS * 2f);
        float expectedTotalHeight = EXPECTED_HEIGHT;

        // Get actual mesh bounds by combining MeshRenderers or MeshFilters in children
        Bounds combinedBounds;
        bool haveBounds = TryGetCombinedMeshBounds(out combinedBounds);

        if (!haveBounds)
        {
            Debug.LogWarning("LegoBrickDimensionValidator: No mesh renderers or mesh filters found to measure bounds.");
            return;
        }

        float actualWidth = combinedBounds.size.x;
        float actualLength = combinedBounds.size.z;
        float actualHeight = combinedBounds.size.y;

        Debug.LogFormat("LegoBrickDimensionValidator: Expected (W x L x H) = {0} x {1} x {2} m",
            expectedTotalWidth.ToString("F3"), expectedTotalLength.ToString("F3"), expectedTotalHeight.ToString("F3"));

        Debug.LogFormat("LegoBrickDimensionValidator: Actual   (W x L x H) = {0} x {1} x {2} m",
            actualWidth.ToString("F3"), actualLength.ToString("F3"), actualHeight.ToString("F3"));

        // Compare and warn if differences exceed tolerance
        float diffW = Mathf.Abs(expectedTotalWidth - actualWidth);
        float diffL = Mathf.Abs(expectedTotalLength - actualLength);
        float diffH = Mathf.Abs(expectedTotalHeight - actualHeight);

        if (diffW > TOLERANCE)
        {
            Debug.LogWarningFormat("Width differs by {0} m (expected {1}, actual {2}).",
                diffW.ToString("F3"), expectedTotalWidth.ToString("F3"), actualWidth.ToString("F3"));
        }

        if (diffL > TOLERANCE)
        {
            Debug.LogWarningFormat("Length differs by {0} m (expected {1}, actual {2}).",
                diffL.ToString("F3"), expectedTotalLength.ToString("F3"), actualLength.ToString("F3"));
        }

        if (diffH > TOLERANCE)
        {
            Debug.LogWarningFormat("Height differs by {0} m (expected {1}, actual {2}).",
                diffH.ToString("F3"), expectedTotalHeight.ToString("F3"), actualHeight.ToString("F3"));
        }

        if (diffW <= TOLERANCE && diffL <= TOLERANCE && diffH <= TOLERANCE)
        {
            Debug.Log("LegoBrickDimensionValidator: Brick dimensions are within tolerance.");
        }

        // Example hints for common brick sizes
        Debug.Log("Examples: 2x4 ≈ 1.50 x 3.10 x 0.20 m; 4x2 ≈ 3.10 x 1.50 x 0.20 m; 2x2 ≈ 1.50 x 1.50 x 0.20 m; 1x1 ≈ 0.70 x 0.70 x 0.20 m.");
    }

    /// <summary>
    /// Attempts to compute combined world-space bounds from MeshRenderers or MeshFilters in children.
    /// Returns true and outputs the combined bounds if any geometry is found.
    /// </summary>
    private bool TryGetCombinedMeshBounds(out Bounds combined)
    {
        combined = new Bounds();
        bool haveAny = false;

        // Prefer MeshRenderers (already in world space)
        var renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers)
        {
            if (!haveAny)
            {
                combined = r.bounds;
                haveAny = true;
            }
            else
            {
                combined.Encapsulate(r.bounds);
            }
        }

        if (haveAny)
            return true;

        // Fallback: MeshFilters (calculate world size using lossyScale)
        var filters = GetComponentsInChildren<MeshFilter>();
        foreach (var f in filters)
        {
            if (f.sharedMesh == null)
                continue;

            var meshBounds = f.sharedMesh.bounds; // local-space bounds
            Vector3 worldSize = Vector3.Scale(meshBounds.size, f.transform.lossyScale);
            Bounds b = new Bounds(f.transform.position, worldSize);

            if (!haveAny)
            {
                combined = b;
                haveAny = true;
            }
            else
            {
                combined.Encapsulate(b);
            }
        }

        return haveAny;
    }
}
