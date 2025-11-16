#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Extract and center ONE specific LEGO brick at a time from the FBX
/// Much better for debugging and testing!
/// </summary>
public class SingleBrickExtractor : EditorWindow
{
    public GameObject fbxSource;
    public int selectedBrickIndex = 0;
    public string outputFolder = "Assets/Prefabs/LegoBricks_Centered";
    public GameObject snapPointPrefab;
    
    private List<string> brickNames = new List<string>();
    private Vector2 scrollPos;
    
    [MenuItem("LEGO/Extract Single Brick (Debug Mode)")]
    static void ShowWindow()
    {
        GetWindow<SingleBrickExtractor>("Extract Single Brick");
    }

    void OnGUI()
    {
        GUILayout.Label("EXTRACT ONE BRICK AT A TIME", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This extracts ONE brick so you can test and debug the centering.", MessageType.Info);
        
        EditorGUILayout.Space();
        
        // FBX Source
        EditorGUILayout.BeginHorizontal();
        fbxSource = (GameObject)EditorGUILayout.ObjectField("FBX Source (Legos)", fbxSource, typeof(GameObject), false);
        if (GUILayout.Button("Refresh List", GUILayout.Width(100)))
        {
            RefreshBrickList();
        }
        EditorGUILayout.EndHorizontal();
        
        if (fbxSource == null)
        {
            EditorGUILayout.HelpBox("Drag the 'Legos' FBX file here", MessageType.Warning);
            return;
        }
        
        // Refresh brick list if needed
        if (brickNames.Count == 0)
        {
            RefreshBrickList();
        }
        
        EditorGUILayout.Space();
        
        // Brick selection
        if (brickNames.Count > 0)
        {
            EditorGUILayout.LabelField($"Found {brickNames.Count} bricks in FBX:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            
            for (int i = 0; i < brickNames.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                // Radio button style selection
                bool isSelected = (i == selectedBrickIndex);
                bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                if (newSelected && !isSelected)
                {
                    selectedBrickIndex = i;
                }
                
                // Brick name
                EditorGUILayout.LabelField($"[{i}] {brickNames[i]}");
                
                // Quick extract button
                if (GUILayout.Button("Extract This", GUILayout.Width(100)))
                {
                    selectedBrickIndex = i;
                    ExtractSingleBrick();
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // Show selected brick
            EditorGUILayout.LabelField("Currently Selected:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"  [{selectedBrickIndex}] {brickNames[selectedBrickIndex]}");
        }
        else
        {
            EditorGUILayout.HelpBox("No meshes found in FBX. Make sure you selected the parent FBX with children.", MessageType.Warning);
        }
        
        EditorGUILayout.Space();
        
        // Output settings
        EditorGUILayout.LabelField("Output Settings:", EditorStyles.boldLabel);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        snapPointPrefab = (GameObject)EditorGUILayout.ObjectField("Snap Point Prefab", snapPointPrefab, typeof(GameObject), false);
        
        EditorGUILayout.Space();
        
        // Extract button
        GUI.enabled = brickNames.Count > 0;
        
        if (GUILayout.Button($"EXTRACT: {(brickNames.Count > 0 ? brickNames[selectedBrickIndex] : "")}", GUILayout.Height(50)))
        {
            ExtractSingleBrick();
        }
        
        GUI.enabled = true;
        
        EditorGUILayout.Space();
        
        // Additional debug options
        EditorGUILayout.LabelField("Debug Options:", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Preview in Scene (with Debug Viz)"))
        {
            PreviewBrickInScene();
        }
        if (GUILayout.Button("Just Instantiate (No Center)"))
        {
            JustInstantiate();
        }
        EditorGUILayout.EndHorizontal();
    }

    void RefreshBrickList()
    {
        brickNames.Clear();
        
        if (fbxSource == null) return;
        
        Transform root = fbxSource.transform;
        
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            
            // Check if it has a mesh
            MeshFilter mf = child.GetComponent<MeshFilter>();
            if (mf == null)
            {
                mf = child.GetComponentInChildren<MeshFilter>();
            }
            
            if (mf != null && mf.sharedMesh != null)
            {
                brickNames.Add(child.name);
            }
        }
        
        Debug.Log($"Found {brickNames.Count} bricks in {fbxSource.name}");
    }

    void ExtractSingleBrick()
    {
        if (fbxSource == null || brickNames.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No brick selected!", "OK");
            return;
        }
        
        // Ensure output folder exists
        EnsureOutputFolder();
        
        // Get the selected brick
        Transform brickTransform = fbxSource.transform.GetChild(selectedBrickIndex);
        string brickName = brickNames[selectedBrickIndex];
        
        Debug.Log("╔═══════════════════════════════════════╗");
        Debug.Log($"║ EXTRACTING: {brickName}");
        Debug.Log("╚═══════════════════════════════════════╝");
        
        // Find mesh
        MeshFilter meshFilter = brickTransform.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = brickTransform.GetComponent<MeshRenderer>();
        
        if (meshFilter == null)
        {
            meshFilter = brickTransform.GetComponentInChildren<MeshFilter>();
            meshRenderer = brickTransform.GetComponentInChildren<MeshRenderer>();
        }
        
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            EditorUtility.DisplayDialog("Error", $"No mesh found in {brickName}!", "OK");
            return;
        }
        
        // Calculate world bounds
        Bounds worldBounds = meshRenderer.bounds;
        Vector3 worldCenter = worldBounds.center;
        Vector3 size = worldBounds.size;
        
        Debug.Log($"Brick Transform Position: {brickTransform.position}");
        Debug.Log($"Mesh World Bounds Center: {worldCenter}");
        Debug.Log($"Mesh Size: {size}");
        Debug.Log($"Offset: {worldCenter - brickTransform.position}");
        
        // Create centered root
        GameObject centeredRoot = new GameObject(brickName);
        centeredRoot.transform.position = worldCenter;
        centeredRoot.transform.rotation = Quaternion.identity;
        
        // Create mesh holder child
        GameObject meshHolder = new GameObject(brickName + "_Mesh");
        meshHolder.transform.SetParent(centeredRoot.transform);
        
        // Copy mesh components
        MeshFilter newMF = meshHolder.AddComponent<MeshFilter>();
        newMF.sharedMesh = meshFilter.sharedMesh;
        
        MeshRenderer newMR = meshHolder.AddComponent<MeshRenderer>();
        newMR.sharedMaterials = meshRenderer.sharedMaterials;
        
        // Calculate offset to keep mesh visually in same place
        Vector3 meshOffset = brickTransform.position - worldCenter;
        meshHolder.transform.localPosition = meshOffset;
        meshHolder.transform.localRotation = brickTransform.localRotation;
        meshHolder.transform.localScale = brickTransform.localScale;
        
        Debug.Log($"Mesh Holder Local Position: {meshHolder.transform.localPosition}");
        
        // Move root to origin for prefab
        centeredRoot.transform.position = Vector3.zero;
        
        // Auto-detect dimensions
        int width = Mathf.Max(1, Mathf.RoundToInt((size.x - 0.7f) / 0.8f) + 1);
        int length = Mathf.Max(1, Mathf.RoundToInt((size.z - 0.7f) / 0.8f) + 1);
        float height = 0.2f;
        
        Debug.Log($"Auto-detected dimensions: {width}x{length}, height: {height}");
        
        // Add components
        Rigidbody rb = centeredRoot.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        BoxCollider col = centeredRoot.AddComponent<BoxCollider>();
        col.size = size;
        col.center = Vector3.zero;
        
        LegoBrick legoBrick = centeredRoot.AddComponent<LegoBrick>();
        
        // Set fields
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
        
        // Add XR Grab Interactable
        var xrGrabType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
        if (xrGrabType != null)
        {
            centeredRoot.AddComponent(xrGrabType);
        }
        
        // Save as prefab
        string prefabPath = $"{outputFolder}/{brickName}.prefab";
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(centeredRoot, prefabPath);
        
        if (savedPrefab != null)
        {
            Debug.Log($"<color=green>✓ SUCCESSFULLY SAVED: {prefabPath}</color>");
            Debug.Log("╚═══════════════════════════════════════╝");
            
            // Clean up scene instance
            DestroyImmediate(centeredRoot);
            
            // Ping the prefab
            Selection.activeObject = savedPrefab;
            EditorGUIUtility.PingObject(savedPrefab);
            
            EditorUtility.DisplayDialog("Success!", 
                $"Brick extracted: {brickName}\n\n" +
                $"Location: {prefabPath}\n\n" +
                $"Dimensions: {width}x{length}\n" +
                $"Size: {size.ToString("F2")}", 
                "OK");
        }
        else
        {
            Debug.LogError($"Failed to save prefab: {prefabPath}");
            DestroyImmediate(centeredRoot);
        }
    }

    void PreviewBrickInScene()
    {
        if (fbxSource == null || brickNames.Count == 0) return;
        
        Transform brickTransform = fbxSource.transform.GetChild(selectedBrickIndex);
        
        // Instantiate it
        GameObject instance = Instantiate(brickTransform.gameObject);
        instance.name = brickNames[selectedBrickIndex] + "_Preview";
        instance.transform.position = Vector3.zero;
        
        // Add debug visualizer
        instance.AddComponent<LegoBrickDebugVisualizer>();
        
        Selection.activeGameObject = instance;
        SceneView.lastActiveSceneView.FrameSelected();
        
        Debug.Log($"✓ Previewed {brickNames[selectedBrickIndex]} in scene with debug visualizer");
        Debug.Log("Look at Scene view - you should see colored spheres showing pivot vs mesh center!");
    }

    void JustInstantiate()
    {
        if (fbxSource == null || brickNames.Count == 0) return;
        
        Transform brickTransform = fbxSource.transform.GetChild(selectedBrickIndex);
        GameObject instance = Instantiate(brickTransform.gameObject);
        instance.name = brickNames[selectedBrickIndex] + "_NoProcessing";
        instance.transform.position = Vector3.zero;
        
        Selection.activeGameObject = instance;
        
        Debug.Log($"✓ Instantiated {brickNames[selectedBrickIndex]} with NO processing");
    }

    void EnsureOutputFolder()
    {
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
    }
}
#endif