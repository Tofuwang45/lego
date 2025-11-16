using UnityEngine;

/// <summary>
/// Represents a single LEGO stud or socket connection point.
/// Attach this component to a GameObject that represents a snap point.
/// </summary>
public class LegoSnapPoint : MonoBehaviour
{
    /// <summary>
    /// Types of snap points available on a LEGO brick.
    /// </summary>
    public enum SnapPointType
    {
        /// <summary>
        /// A stud (male connection).
        /// </summary>
        Stud,

        /// <summary>
        /// A socket (female connection).
        /// </summary>
        Socket
    }

    /// <summary>
    /// The type of this snap point (Stud or Socket).
    /// </summary>
    public SnapPointType type;

    /// <summary>
    /// True if this snap point is currently connected to another snap point.
    /// </summary>
    public bool isConnected;

    /// <summary>
    /// Reference to the other <see cref="LegoSnapPoint"/> this point is connected to, or null if not connected.
    /// </summary>
    public LegoSnapPoint connectedTo;

    /// <summary>
    /// Reference to the parent <see cref="LegoBrick"/> that owns this snap point.
    /// </summary>
    public LegoBrick parentBrick;

    // Distance between stud centers: 0.8m (scaled from real LEGO 8mm for comfortable VR interaction)
    /// <summary>
    /// Spacing between studs in (units used by the project). Default suggested value: 0.8.
    /// </summary>
    public const float STUD_SPACING = 0.8f;

    // Snap detection radius: 0.35m (half-stud distance, also equals edge margin for natural snapping)
    /// <summary>
    /// Radius within which snapping should occur. Default suggested value: 0.35.
    /// </summary>
    public const float SNAP_RADIUS = 0.35f;

    /// <summary>
    /// Draw debug gizmos in the Scene view to visualize snap points and connections.
    /// Draws only for studs to avoid duplicate lines between pairs.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (type != SnapPointType.Stud)
            return;

        // Color: green when connected, yellow when not
        Gizmos.color = isConnected ? Color.green : Color.yellow;

        // Draw wire sphere showing snap radius
        Gizmos.DrawWireSphere(transform.position, SNAP_RADIUS);

        // If connected, draw a line to the connected snap point
        if (isConnected && connectedTo != null)
        {
            Gizmos.DrawLine(transform.position, connectedTo.transform.position);
        }
    }
}
