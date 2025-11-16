#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Properly extracts LEGO bricks from FBX with centered pivots by creating wrapper GameObjects
/// </summary>
public class ProperLegoBrickExtractor : EditorWindow
{
    public GameObject fbxSource;
    public string outputFolder = "Assets/Prefabs/LegoBricks_Centered";
    public GameObject snapPointPrefab;
    
    [MenuItem("LEGO/Properly Extract and Center Bricks")]
    static void ShowWindow()
    {
        GetWindow<ProperLegoBrickExtractor>("Extract LEGO Bricks (Fixed)");
    }

    void OnGUI()
    {
        GUILayout.Label("PROPER LEGO Brick Extraction", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        fbxSource = (GameObject)EditorGUILayout.ObjectField("FBX Source (Legos)", fbxSource, typeof(GameObject), false);
        EditorGUILayout.HelpBox("Drag the 'Legos' FBX file (with play button)", MessageType.Info);
        
        EditorGUILayout.Space();
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        
        EditorGUILayout.Space();
        snapPointPrefab = (GameObject)EditorGUILayout.ObjectField("Snap Point Prefab", snapPointPrefab, typeof(GameObject), false);
        
        EditorGUILayout.Space();
        
        GUI.enabled = fbxSource != null;
        if (GUILayout.Button("EXTRACT WITH PROPER CENTERING", GUILayout.Height(50)))
        {
            ProperExtraction();
        }
        GUI.enabled = true;
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This will create properly centered bricks in: " + outputFolder, MessageType.Info);
    }

    void ProperExtraction()
    {
        if (fbxSource == null)
        {
            EditorUtility.DisplayDialog("Error", "Assign the FBX source first!", "OK");
            return;
        }

        // Ensure output folder exists
        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            string[] folders = outputFolder.Split('/');
            string currentPath = folders[0];
            
            for (int i = 1; i < folders.Length; i++)
            {
                string newPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = newPath;
            }
        }

        Debug.Log("═══════════════════════════════════════");
        Debug.Log("STARTING PROPER BRICK EXTRACTION");
        Debug.Log("═══════════════════════════════════════");

        int successCount = 0;
        int failCount = 0;

        // Get direct children from the FBX
        Transform fbxRoot = fbxSource.transform;
        
        for (int i = 0; i < fbxRoot.childCount; i++)
        {
            Transform childTransform = fbxRoot.GetChild(i);
            
            // Find mesh in this child
            MeshFilter meshFilter = childTransform.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = childTransform.GetComponent<MeshRenderer>();
            
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                Debug.LogWarning($"Skipping {childTransform.name} - no mesh");
                failCount++;
                continue;
            }

            Debug.Log($"\n>>> Processing: {childTransform.name}");

            // Calculate the ACTUAL world-space bounds
            Bounds worldBounds = meshRenderer.bounds;
            Vector3 worldCenter = worldBounds.center;
            Vector3 size = worldBounds.size;
            
            Debug.Log($"    World bounds center: {worldCenter}");
            Debug.Log($"    Bounds size: {size}");

            // Create the new centered root GameObject at the world center
            GameObject centeredRoot = new GameObject(childTransform.name);
            centeredRoot.transform.position = worldCenter;
            centeredRoot.transform.rotation = Quaternion.identity;

            // Create a mesh holder child
            GameObject meshHolder = new GameObject(childTransform.name + "_Mesh");
            meshHolder.transform.SetParent(centeredRoot.transform);
            
            // Copy the mesh components
            MeshFilter newMeshFilter = meshHolder.AddComponent<MeshFilter>();
            newMeshFilter.sharedMesh = meshFilter.sharedMesh;
            
            MeshRenderer newRenderer = meshHolder.AddComponent<MeshRenderer>();
            newRenderer.sharedMaterials = meshRenderer.sharedMaterials;
            
            // Calculate offset: mesh holder needs to be offset so the mesh visually stays in place
            // but the root is now at the center
            Vector3 meshOffset = childTransform.position - worldCenter;
            meshHolder.transform.localPosition = meshOffset;
            meshHolder.transform.localRotation = childTransform.localRotation;
            meshHolder.transform.localScale = childTransform.localScale;
            
            Debug.Log($"    Mesh offset from center: {meshOffset}");
            Debug.Log($"    Root position: {centeredRoot.transform.position}");

            // Auto-detect dimensions
            int width = Mathf.Max(1, Mathf.RoundToInt((size.x - 0.7f) / 0.8f) + 1);
            int length = Mathf.Max(1, Mathf.RoundToInt((size.z - 0.7f) / 0.8f) + 1);
            float height = Mathf.Approximately(size.y, 0) ? 0.2f : size.y;
            
            Debug.Log($"    Auto-detected: {width}x{length}, height: {height:F3}");

            // Move root to origin for prefab creation
            centeredRoot.transform.position = Vector3.zero;

            // Add components
            Rigidbody rb = centeredRoot.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            BoxCollider boxCol = centeredRoot.AddComponent<BoxCollider>();
            boxCol.size = size;
            boxCol.center = Vector3.zero;

            LegoBrick legoBrick = centeredRoot.AddComponent<LegoBrick>();
            
            // Set private fields using SerializedObject
            SerializedObject so = new SerializedObject(legoBrick);
            so.FindProperty("width").intValue = width;
            so.FindProperty("length").intValue = length;
            so.FindProperty("height").floatValue = height;
            
            if (snapPointPrefab != null)
            {
                so.FindProperty("snapPointPrefab").objectReferenceValue = snapPointPrefab;
            }
            
            so.ApplyModifiedPropertiesWithoutUndo();

            centeredRoot.AddComponent<LegoXRGrabbable>();

            // Add XR Grab Interactable if available
            var xrGrabType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
            if (xrGrabType != null)
            {
                var xrGrab = centeredRoot.AddComponent(xrGrabType);
                
                // Set movement type to Kinematic (value = 1)
                var movementTypeProp = xrGrabType.GetProperty("movementType");
                if (movementTypeProp != null)
                {
                    movementTypeProp.SetValue(xrGrab, 1);
                }
            }

            // Save as prefab
            string prefabPath = $"{outputFolder}/{childTransform.name}.prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(centeredRoot, prefabPath);

            if (savedPrefab != null)
            {
                Debug.Log($"    ✓ SAVED: {prefabPath}");
                successCount++;
            }
            else
            {
                Debug.LogError($"    ✗ FAILED to save: {prefabPath}");
                failCount++;
            }

            // Clean up
            DestroyImmediate(centeredRoot);
        }

        AssetDatabase.Refresh();

        Debug.Log("═══════════════════════════════════════");
        Debug.Log($"<color=green>✓ SUCCESS: {successCount} bricks extracted</color>");
        if (failCount > 0)
        {
            Debug.Log($"<color=red>✗ FAILED: {failCount} bricks</color>");
        }
        Debug.Log($"Output: {outputFolder}");
        Debug.Log("═══════════════════════════════════════");

        EditorUtility.DisplayDialog("Extraction Complete!", 
            $"Successfully extracted {successCount} bricks!\n\n" +
            $"Location: {outputFolder}\n\n" +
            $"Each brick is now properly centered at its pivot point.",
            "OK");

        // Highlight the folder
        Object folder = AssetDatabase.LoadAssetAtPath<Object>(outputFolder);
        if (folder != null)
        {
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }
    }
}
#endif