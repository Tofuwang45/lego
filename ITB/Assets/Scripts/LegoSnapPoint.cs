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
    /// Spacing between studs in meters (units used by the project). Default suggested value: 0.8.
    /// </summary>
    public const float STUD_SPACING = 0.8f;

    // Snap detection radius: 0.8m (detection radius for snapping)
    /// <summary>
    /// Radius within which snapping should occur. Default suggested value: 0.8.
    /// </summary>
    public const float SNAP_RADIUS = 0.05f;

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

    /// <summary>
    /// Render visual indicators during Play mode using GL lines
    /// </summary>
    private void OnRenderObject()
    {
        if (!Application.isPlaying)
            return;

        // Create material for drawing if needed
        if (lineMaterial == null)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);
        }

        lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);
        GL.Begin(GL.LINES);

        // Color based on type and connection status
        if (isConnected)
            GL.Color(Color.green);
        else if (type == SnapPointType.Stud)
            GL.Color(new Color(1f, 0f, 0f, 0.8f)); // Red for studs
        else
            GL.Color(new Color(0f, 0.5f, 1f, 0.8f)); // Blue for sockets

        // Draw a small cross at the snap point position
        float size = 0.05f;
        GL.Vertex3(-size, 0, 0);
        GL.Vertex3(size, 0, 0);
        GL.Vertex3(0, -size, 0);
        GL.Vertex3(0, size, 0);
        GL.Vertex3(0, 0, -size);
        GL.Vertex3(0, 0, size);

        GL.End();
        GL.PopMatrix();

        // Draw connection line if connected
        if (isConnected && connectedTo != null && type == SnapPointType.Stud)
        {
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.Begin(GL.LINES);
            GL.Color(Color.green);
            GL.Vertex3(transform.position.x, transform.position.y, transform.position.z);
            GL.Vertex3(connectedTo.transform.position.x, connectedTo.transform.position.y, connectedTo.transform.position.z);
            GL.End();
            GL.PopMatrix();
        }
    }

    private static Material lineMaterial;
}
