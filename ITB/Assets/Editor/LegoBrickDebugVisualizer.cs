using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Attaches to any GameObject to visualize mesh bounds, centers, and offsets
/// </summary>
public class LegoBrickDebugVisualizer : MonoBehaviour
{
    public bool showDebugInfo = true;
    public Color boundsColor = Color.cyan;
    public Color centerColor = Color.yellow;
    public Color pivotColor = Color.red;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showDebugInfo) return;

        // Find mesh in this object or children
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        
        if (meshFilter == null)
        {
            meshFilter = GetComponentInChildren<MeshFilter>();
        }
        
        if (meshRenderer == null)
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>();
        }

        if (meshFilter == null || meshFilter.sharedMesh == null) return;

        // Draw local mesh bounds (in mesh space)
        Bounds localBounds = meshFilter.sharedMesh.bounds;
        Vector3 localCenter = localBounds.center;
        
        // Draw world mesh bounds (actual position in scene)
        Bounds worldBounds = meshRenderer.bounds;
        Vector3 worldCenter = worldBounds.center;
        Vector3 worldSize = worldBounds.size;

        // 1. Draw WORLD bounds (where mesh actually is)
        Gizmos.color = boundsColor;
        Gizmos.DrawWireCube(worldCenter, worldSize);
        
        // 2. Draw WORLD center point
        Gizmos.color = centerColor;
        Gizmos.DrawWireSphere(worldCenter, 0.1f);
        Handles.Label(worldCenter + Vector3.up * 0.2f, 
            $"MESH CENTER\n{worldCenter.ToString("F2")}", 
            new GUIStyle() { 
                normal = new GUIStyleState() { textColor = centerColor },
                fontSize = 11,
                fontStyle = FontStyle.Bold
            });

        // 3. Draw GameObject PIVOT (this transform's position)
        Gizmos.color = pivotColor;
        Gizmos.DrawWireSphere(transform.position, 0.15f);
        Handles.Label(transform.position + Vector3.up * 0.4f, 
            $"PIVOT\n{transform.position.ToString("F2")}", 
            new GUIStyle() { 
                normal = new GUIStyleState() { textColor = pivotColor },
                fontSize = 11,
                fontStyle = FontStyle.Bold
            });

        // 4. Draw line showing offset
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, worldCenter);
        
        Vector3 offset = worldCenter - transform.position;
        Handles.Label((transform.position + worldCenter) / 2f, 
            $"OFFSET: {offset.ToString("F2")}\nDistance: {offset.magnitude:F2}m", 
            new GUIStyle() { 
                normal = new GUIStyleState() { textColor = Color.magenta },
                fontSize = 10
            });

        // 5. Draw coordinate axes at pivot
        float axisLength = 0.3f;
        Handles.color = Color.red;
        Handles.DrawLine(transform.position, transform.position + transform.right * axisLength);
        Handles.color = Color.green;
        Handles.DrawLine(transform.position, transform.position + transform.up * axisLength);
        Handles.color = Color.blue;
        Handles.DrawLine(transform.position, transform.position + transform.forward * axisLength);

        // 6. Display info box
        string info = $"LOCAL mesh bounds center: {localCenter.ToString("F3")}\n" +
                     $"WORLD mesh center: {worldCenter.ToString("F3")}\n" +
                     $"GameObject position: {transform.position.ToString("F3")}\n" +
                     $"Offset: {offset.ToString("F3")} ({offset.magnitude:F3}m)\n" +
                     $"Mesh size: {worldSize.ToString("F3")}";
        
        Handles.Label(transform.position + Vector3.down * 0.5f, info,
            new GUIStyle() {
                normal = new GUIStyleState() { 
                    textColor = Color.white,
                    background = MakeTex(2, 2, new Color(0, 0, 0, 0.7f))
                },
                fontSize = 9,
                padding = new RectOffset(5, 5, 5, 5)
            });
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    [ContextMenu("Print Debug Info to Console")]
    void PrintDebugInfo()
    {
        MeshFilter meshFilter = GetComponentInChildren<MeshFilter>();
        MeshRenderer meshRenderer = GetComponentInChildren<MeshRenderer>();
        
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("No mesh found!");
            return;
        }

        Bounds localBounds = meshFilter.sharedMesh.bounds;
        Bounds worldBounds = meshRenderer.bounds;
        
        Debug.Log("═══════════════════════════════════════");
        Debug.Log($"DEBUG INFO for: {gameObject.name}");
        Debug.Log("═══════════════════════════════════════");
        Debug.Log($"GameObject Position: {transform.position}");
        Debug.Log($"Mesh Local Bounds Center: {localBounds.center}");
        Debug.Log($"Mesh Local Bounds Size: {localBounds.size}");
        Debug.Log($"Mesh World Bounds Center: {worldBounds.center}");
        Debug.Log($"Mesh World Bounds Size: {worldBounds.size}");
        Debug.Log($"Offset (World Center - Pivot): {worldBounds.center - transform.position}");
        Debug.Log($"Offset Magnitude: {(worldBounds.center - transform.position).magnitude}m");
        
        Transform meshTransform = meshFilter.transform;
        Debug.Log($"\nMesh Holder Transform:");
        Debug.Log($"  Local Position: {meshTransform.localPosition}");
        Debug.Log($"  World Position: {meshTransform.position}");
        Debug.Log($"  Local Scale: {meshTransform.localScale}");
        Debug.Log("═══════════════════════════════════════");
    }
#endif
}