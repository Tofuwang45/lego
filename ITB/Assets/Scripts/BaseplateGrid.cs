using UnityEngine;

public class BaseplateGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 16;   // Number of studs wide
    public int gridDepth = 16;   // Number of studs deep
    public float studSpacing = 0.008f;  // Distance between studs (LEGO standard)
    
    [Header("Visualization")]
    public bool showGrid = true;
    
    public Transform[] studs;  // Will be auto-generated
    
    void Start()
    {
        GenerateGrid();
    }
    
    void GenerateGrid()
    {
        // Create container for studs
        Transform studsContainer = transform.Find("Studs");
        if (studsContainer == null)
        {
            GameObject container = new GameObject("Studs");
            container.transform.parent = transform;
            container.transform.localPosition = Vector3.zero;
            studsContainer = container.transform;
        }
        
        // Clear existing studs
        foreach (Transform child in studsContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Calculate starting position (center the grid)
        float startX = -(gridWidth - 1) * studSpacing / 2f;
        float startZ = -(gridDepth - 1) * studSpacing / 2f;
        
        // Generate grid of studs
        System.Collections.Generic.List<Transform> studList = new System.Collections.Generic.List<Transform>();
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridDepth; z++)
            {
                GameObject stud = new GameObject($"stud_{x}_{z}");
                stud.transform.parent = studsContainer;
                
                // Position on grid
                stud.transform.localPosition = new Vector3(
                    startX + (x * studSpacing),
                    0.001f,  // Slightly above baseplate surface
                    startZ + (z * studSpacing)
                );
                
                studList.Add(stud.transform);
            }
        }
        
        studs = studList.ToArray();
        Debug.Log($"Generated {studs.Length} snap points on baseplate");
    }
    
    void OnDrawGizmos()
    {
        if (!showGrid || studs == null) return;
        
        Gizmos.color = Color.cyan;
        foreach (var stud in studs)
        {
            if (stud != null)
            {
                Gizmos.DrawWireSphere(stud.position, 0.002f);
            }
        }
    }
    
    // Call this from editor to regenerate grid
    [ContextMenu("Regenerate Grid")]
    void RegenerateGrid()
    {
        GenerateGrid();
    }
}