#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Simple tool to manually center a single brick for testing
/// </summary>
public class ManualBrickCenterer : MonoBehaviour
{
    [MenuItem("LEGO/DEBUG - Center Selected Brick Manually")]
    static void CenterSelectedBrick()
    {
        GameObject selected = Selection.activeGameObject;
        
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a LEGO brick in the Hierarchy!", "OK");
            return;
        }

        Debug.Log("═══════════════════════════════════════");
        Debug.Log($"CENTERING: {selected.name}");
        Debug.Log("═══════════════════════════════════════");

        // Find the mesh
        MeshRenderer renderer = selected.GetComponentInChildren<MeshRenderer>();
        
        if (renderer == null)
        {
            EditorUtility.DisplayDialog("Error", "No MeshRenderer found in selected object or children!", "OK");
            return;
        }

        // Get WORLD bounds
        Bounds worldBounds = renderer.bounds;
        Vector3 worldCenter = worldBounds.center;
        Vector3 currentPosition = selected.transform.position;
        
        Debug.Log($"Current GameObject position: {currentPosition}");
        Debug.Log($"Mesh world center: {worldCenter}");
        Debug.Log($"Offset: {worldCenter - currentPosition}");
        Debug.Log($"Mesh size: {worldBounds.size}");

        // Record undo
        Undo.RecordObject(selected.transform, "Center Brick");

        // METHOD 1: Move the GameObject to the mesh center
        Vector3 offset = worldCenter - currentPosition;
        selected.transform.position = worldCenter;
        
        // Move all children back by the offset to keep them visually in place
        foreach (Transform child in selected.transform)
        {
            Undo.RecordObject(child, "Adjust child position");
            child.position -= offset;
        }

        Debug.Log($"✓ GameObject moved to: {selected.transform.position}");
        Debug.Log($"✓ Children adjusted by offset: {-offset}");
        Debug.Log("═══════════════════════════════════════");

        EditorUtility.DisplayDialog("Success", 
            $"Brick centered!\n\n" +
            $"Pivot moved from {currentPosition.ToString("F2")} to {worldCenter.ToString("F2")}\n\n" +
            $"Offset was: {offset.ToString("F2")}", 
            "OK");

        EditorUtility.SetDirty(selected);
    }

    [MenuItem("LEGO/DEBUG - Reset Selected Brick to Origin")]
    static void ResetToOrigin()
    {
        GameObject selected = Selection.activeGameObject;
        
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an object!", "OK");
            return;
        }

        Undo.RecordObject(selected.transform, "Reset to origin");
        selected.transform.position = Vector3.zero;
        selected.transform.rotation = Quaternion.identity;
        
        Debug.Log($"✓ {selected.name} reset to origin");
        EditorUtility.SetDirty(selected);
    }

    [MenuItem("LEGO/DEBUG - Print Hierarchy Structure")]
    static void PrintHierarchy()
    {
        GameObject selected = Selection.activeGameObject;
        
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an object!", "OK");
            return;
        }

        Debug.Log("═══════════════════════════════════════");
        Debug.Log($"HIERARCHY for: {selected.name}");
        Debug.Log("═══════════════════════════════════════");
        PrintTransformHierarchy(selected.transform, 0);
        Debug.Log("═══════════════════════════════════════");
    }

    static void PrintTransformHierarchy(Transform t, int level)
    {
        string indent = new string(' ', level * 2);
        
        string components = "";
        Component[] comps = t.GetComponents<Component>();
        foreach (var comp in comps)
        {
            if (comp is Transform) continue;
            components += comp.GetType().Name + ", ";
        }
        
        Debug.Log($"{indent}├─ {t.name}");
        Debug.Log($"{indent}│  Position: {t.position}, Local: {t.localPosition}");
        Debug.Log($"{indent}│  Scale: {t.localScale}");
        if (!string.IsNullOrEmpty(components))
        {
            Debug.Log($"{indent}│  Components: {components.TrimEnd(',', ' ')}");
        }
        
        for (int i = 0; i < t.childCount; i++)
        {
            PrintTransformHierarchy(t.GetChild(i), level + 1);
        }
    }
}
#endif