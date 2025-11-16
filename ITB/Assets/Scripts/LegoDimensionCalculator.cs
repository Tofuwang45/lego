using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper component to calculate LEGO stud spacing and brick height from four reference transforms.
/// Attach to any GameObject and assign the four transforms in the Inspector.
/// </summary>
public class LegoDimensionCalculator : MonoBehaviour
{
    /// <summary>
    /// Reference transform for the first stud origin.
    /// </summary>
    public Transform stud1;

    /// <summary>
    /// Reference transform for the second stud (X direction).
    /// </summary>
    public Transform stud2;

    /// <summary>
    /// Reference transform for the third stud (Z direction).
    /// </summary>
    public Transform stud3;

    /// <summary>
    /// Reference transform to measure brick height from stud1.
    /// </summary>
    public Transform height;

    /// <summary>
    /// Calculates studSpacingX (between stud1 and stud2), studSpacingZ (between stud1 and stud3),
    /// and brickHeight (between stud1 and height). Logs results and suggested STUD_SPACING and SNAP_RADIUS.
    /// </summary>
    [ContextMenu("Calculate Dimensions")]
    public void CalculateDimensions()
    {
        if (!ValidateTransforms())
            return;

        float studSpacingX = Vector3.Distance(stud1.position, stud2.position);
        float studSpacingZ = Vector3.Distance(stud1.position, stud3.position);
        float brickHeight = Vector3.Distance(stud1.position, height.position);

        Debug.LogFormat("Stud spacing X: {0} m", studSpacingX.ToString("F6"));
        Debug.LogFormat("Stud spacing Z: {0} m", studSpacingZ.ToString("F6"));
        Debug.LogFormat("Brick height: {0} m", brickHeight.ToString("F6"));

        // Decide suggested STUD_SPACING
        const float tolerance = 0.0001f; // 0.1 mm tolerance
        float diff = Mathf.Abs(studSpacingX - studSpacingZ);
        float suggestedStudSpacing = (studSpacingX + studSpacingZ) * 0.5f;

        if (diff <= tolerance)
        {
            Debug.LogFormat("Suggested STUD_SPACING (average): {0} m", suggestedStudSpacing.ToString("F6"));
        }
        else
        {
            Debug.LogWarningFormat("Stud spacing X and Z differ by {0} m — they are not close. X={1} m, Z={2} m. Suggested STUD_SPACING (average): {3} m",
                diff.ToString("F6"), studSpacingX.ToString("F6"), studSpacingZ.ToString("F6"), suggestedStudSpacing.ToString("F6"));
        }

        float suggestedSnapRadius = suggestedStudSpacing * 0.5f;
        Debug.LogFormat("Suggested SNAP_RADIUS: {0} m", suggestedSnapRadius.ToString("F6"));
    }

    /// <summary>
    /// Validates that all required transforms are assigned and logs an error if not.
    /// </summary>
    /// <returns>True if all transforms are assigned; false otherwise.</returns>
    private bool ValidateTransforms()
    {
        if (stud1 == null || stud2 == null || stud3 == null || height == null)
        {
            Debug.LogError("LegoDimensionCalculator: One or more required transforms (stud1, stud2, stud3, height) are not assigned.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Draw visual debug lines and labels in the Scene view when all transforms are assigned.
    /// Uses <see cref="UnityEditor.Handles"/> to draw text labels (editor-only).
    /// </summary>
    void OnDrawGizmos()
    {
        if (stud1 == null || stud2 == null || stud3 == null || height == null)
            return;

        // X spacing (cyan)
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(stud1.position, stud2.position);
#if UNITY_EDITOR
        float spacingX = Vector3.Distance(stud1.position, stud2.position);
        Vector3 midX = (stud1.position + stud2.position) * 0.5f;
        Handles.color = Color.cyan;
        Handles.Label(midX, $"X: {spacingX:F6} m");
#endif

        // Z spacing (magenta)
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(stud1.position, stud3.position);
#if UNITY_EDITOR
        float spacingZ = Vector3.Distance(stud1.position, stud3.position);
        Vector3 midZ = (stud1.position + stud3.position) * 0.5f;
        Handles.color = Color.magenta;
        Handles.Label(midZ, $"Z: {spacingZ:F6} m");
#endif

        // Height (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(stud1.position, height.position);
#if UNITY_EDITOR
        float h = Vector3.Distance(stud1.position, height.position);
        Vector3 midH = (stud1.position + height.position) * 0.5f;
        Handles.color = Color.yellow;
        Handles.Label(midH, $"H: {h:F6} m");
#endif
    }
}
