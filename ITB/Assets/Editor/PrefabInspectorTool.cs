#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class PrefabInspectorTool : EditorWindow
{
    public GameObject prefabToInspect;
    
    [MenuItem("LEGO/DEBUG - Inspect Prefab Structure")]
    static void ShowWindow()
    {
        GetWindow<PrefabInspectorTool>("Prefab Inspector");
    }

    void OnGUI()
    {
        GUILayout.Label("Prefab Structure Inspector", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        prefabToInspect = (GameObject)EditorGUILayout.ObjectField("Prefab", prefabToInspect, typeof(GameObject), false);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Analyze Prefab", GUILayout.Height(30)))
        {
            AnalyzePrefab();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Test Instantiate in Scene", GUILayout.Height(30)))
        {
            TestInstantiate();
        }
    }

    void AnalyzePrefab()
    {
        if (prefabToInspect == null)
        {
            EditorUtility.DisplayDialog("Error", "Assign a prefab first!", "OK");
            return;
        }

        Debug.Log("╔═══════════════════════════════════════╗");
        Debug.Log($"║ ANALYZING PREFAB: {prefabToInspect.name}");
        Debug.Log("╚═══════════════════════════════════════╝");

        // Check if it's actually a prefab
        PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(prefabToInspect);
        Debug.Log($"Prefab Type: {prefabType}");

        // Analyze root transform
        Transform root = prefabToInspect.transform;
        Debug.Log($"\nROOT TRANSFORM:");
        Debug.Log($"  Position: {root.position}");
        Debug.Log($"  Rotation: {root.rotation.eulerAngles}");
        Debug.Log($"  Scale: {root.localScale}");
        Debug.Log($"  Child Count: {root.childCount}");

        // List all components on root
        Debug.Log($"\nROOT COMPONENTS:");
        Component[] rootComponents = prefabToInspect.GetComponents<Component>();
        foreach (var comp in rootComponents)
        {
            Debug.Log($"  • {comp.GetType().Name}");
            
            // Special handling for specific types
            if (comp is LegoBrick)
            {
                LegoBrick lb = comp as LegoBrick;
                Debug.Log($"    - Width: {lb.Width}, Length: {lb.Length}, Height: {lb.height}");
            }
            else if (comp is BoxCollider)
            {
                BoxCollider bc = comp as BoxCollider;
                Debug.Log($"    - Center: {bc.center}, Size: {bc.size}");
            }
        }

        // Analyze children
        Debug.Log($"\nCHILDREN:");
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            Debug.Log($"  [{i}] {child.name}");
            Debug.Log($"      Local Pos: {child.localPosition}");
            Debug.Log($"      World Pos: {child.position}");
            Debug.Log($"      Local Scale: {child.localScale}");
            
            // Check for mesh
            MeshFilter mf = child.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Debug.Log($"      Mesh: {mf.sharedMesh.name}");
                Debug.Log($"      Mesh Bounds: center={mf.sharedMesh.bounds.center}, size={mf.sharedMesh.bounds.size}");
                Debug.Log($"      Vertices: {mf.sharedMesh.vertexCount}");
            }
            
            MeshRenderer mr = child.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Debug.Log($"      Renderer Bounds: {mr.bounds}");
            }

            // List child components
            Component[] childComps = child.GetComponents<Component>();
            string compList = "Components: ";
            foreach (var comp in childComps)
            {
                if (comp is Transform) continue;
                compList += comp.GetType().Name + ", ";
            }
            if (compList != "Components: ")
            {
                Debug.Log($"      {compList.TrimEnd(',', ' ')}");
            }
        }

        Debug.Log("╚═══════════════════════════════════════╝");
    }

    void TestInstantiate()
    {
        if (prefabToInspect == null)
        {
            EditorUtility.DisplayDialog("Error", "Assign a prefab first!", "OK");
            return;
        }

        // Instantiate in scene
        GameObject instance = PrefabUtility.InstantiatePrefab(prefabToInspect) as GameObject;
        instance.transform.position = Vector3.zero;
        
        // Add debug visualizer
        instance.AddComponent<LegoBrickDebugVisualizer>();
        
        Selection.activeGameObject = instance;
        SceneView.lastActiveSceneView.FrameSelected();
        
        Debug.Log($"✓ Instantiated {prefabToInspect.name} at origin with debug visualizer");
        Debug.Log("Look in Scene view to see the debug visualization!");
    }
}
#endif