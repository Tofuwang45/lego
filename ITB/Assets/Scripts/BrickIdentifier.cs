using UnityEngine;

/// <summary>
/// Assigns a unique ID to each brick instance in the scene.
/// This ID is used to track bricks in the build history.
/// </summary>
public class BrickIdentifier : MonoBehaviour
{
    [Header("Brick Information")]
    [Tooltip("Unique identifier for this brick instance (auto-generated)")]
    public string uniqueID;

    [Tooltip("Human-readable name (e.g., '2x4_Red', '1x1_Blue')")]
    public string brickName;

    [Header("Optional: Brick Properties")]
    [Tooltip("Number of studs along the length")]
    public int studsLength = 2;

    [Tooltip("Number of studs along the width")]
    public int studsWidth = 1;

    [Tooltip("Brick color for documentation")]
    public string brickColor = "Red";

    private void Awake()
    {
        // Generate unique ID if not already set
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = System.Guid.NewGuid().ToString();
        }

        // Auto-generate brick name if not set
        if (string.IsNullOrEmpty(brickName))
        {
            brickName = $"{studsLength}x{studsWidth}_{brickColor}_{gameObject.name}";
        }
    }

    /// <summary>
    /// Get a formatted display name for this brick
    /// </summary>
    public string GetDisplayName()
    {
        return $"{studsLength}x{studsWidth} {brickColor} Brick";
    }

    /// <summary>
    /// Reset the unique ID (useful for prefab instances)
    /// </summary>
    public void RegenerateID()
    {
        uniqueID = System.Guid.NewGuid().ToString();
    }
}
