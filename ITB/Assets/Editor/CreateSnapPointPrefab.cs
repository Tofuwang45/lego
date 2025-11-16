using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor helper to create a SnapPoint prefab configured for LEGO snap points.
/// </summary>
public static class CreateSnapPointPrefab
{
    private const string PrefabFolder = "Assets/Prefabs";
    private const string PrefabPath = "Assets/Prefabs/SnapPointPrefab.prefab";

    [MenuItem("LEGO/Create Snap Point Prefab")]
    public static void Create()
    {
        // Ensure prefab folder exists
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // Create root GameObject
        GameObject root = new GameObject("SnapPointPrefab");

        // Add LegoSnapPoint component
        root.AddComponent<LegoSnapPoint>();

        // Add SphereCollider configured as trigger with SNAP_RADIUS
        SphereCollider col = root.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = LegoSnapPoint.SNAP_RADIUS;

        // Optionally add a small visual sphere for editor visibility
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Visual";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;
        // scale to approx match 0.35m radius visual; sphere default is 1 unit diameter
        visual.transform.localScale = Vector3.one * 0.3f;

        // Remove the primitive's collider so only the root collider exists
        Object.DestroyImmediate(visual.GetComponent<SphereCollider>());

        // Save as prefab (overwrites if exists)
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);

        if (prefab != null)
        {
            Debug.Log($"<color=green>✓ Snap Point Prefab created at {PrefabPath}</color>");
            
            // Select the prefab in Project window
            Selection.activeObject = prefab;
            
            // Ping it in the project window to highlight it
            EditorGUIUtility.PingObject(prefab);
        }
        else
        {
            Debug.LogError("Failed to create Snap Point prefab.");
        }

        // Clean up the temporary scene object
        Object.DestroyImmediate(root);

        // Refresh AssetDatabase to show the new prefab
        AssetDatabase.Refresh();
    }
}
