using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Singleton manager that tracks the entire build history.
/// Stores all build steps chronologically and provides methods for querying and exporting.
/// </summary>
public class BuildHistoryManager : MonoBehaviour
{
    // Singleton instance
    private static BuildHistoryManager _instance;
    public static BuildHistoryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<BuildHistoryManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("BuildHistoryManager");
                    _instance = go.AddComponent<BuildHistoryManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Build History")]
    [Tooltip("Chronological list of all build steps")]
    [SerializeField]
    private List<BuildStep> buildHistory = new List<BuildStep>();

    [Header("Settings")]
    [Tooltip("Maximum number of steps to store (0 = unlimited)")]
    public int maxHistorySize = 0;

    [Tooltip("Enable debug logging")]
    public bool enableDebugLog = true;

    private void Awake()
    {
        // Enforce singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Add a new build step to the history
    /// </summary>
    public void AddBuildStep(BuildStep step)
    {
        if (step == null)
        {
            Debug.LogWarning("Attempted to add null BuildStep to history");
            return;
        }

        buildHistory.Add(step);

        if (enableDebugLog)
        {
            Debug.Log($"[BuildHistory] Step {buildHistory.Count}: {step.GetDescription()}");
        }

        // Enforce max history size
        if (maxHistorySize > 0 && buildHistory.Count > maxHistorySize)
        {
            buildHistory.RemoveAt(0);
        }
    }

    /// <summary>
    /// Remove a build step by brick ID (useful for deletion/undo)
    /// </summary>
    public void RemoveBuildStep(string brickID)
    {
        int removed = buildHistory.RemoveAll(step => step.brickID == brickID);

        if (enableDebugLog && removed > 0)
        {
            Debug.Log($"[BuildHistory] Removed {removed} step(s) for brick {brickID}");
        }
    }

    /// <summary>
    /// Find a build step by brick ID
    /// </summary>
    public BuildStep FindStepByBrickID(string brickID)
    {
        return buildHistory.FirstOrDefault(step => step.brickID == brickID);
    }

    /// <summary>
    /// Get all build steps (read-only)
    /// </summary>
    public List<BuildStep> GetAllSteps()
    {
        return new List<BuildStep>(buildHistory);
    }

    /// <summary>
    /// Get the total number of build steps
    /// </summary>
    public int GetStepCount()
    {
        return buildHistory.Count;
    }

    /// <summary>
    /// Clear all build history
    /// </summary>
    public void ClearHistory()
    {
        buildHistory.Clear();
        if (enableDebugLog)
        {
            Debug.Log("[BuildHistory] History cleared");
        }
    }

    /// <summary>
    /// Generate a human-readable summary of the build history
    /// </summary>
    public string GenerateSummary()
    {
        if (buildHistory.Count == 0)
        {
            return "No build steps recorded.";
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"Build History Summary ({buildHistory.Count} steps):");
        sb.AppendLine("==============================================");

        for (int i = 0; i < buildHistory.Count; i++)
        {
            sb.AppendLine($"Step {i + 1}: {buildHistory[i].GetDescription()}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Export build history as JSON for AI API consumption
    /// </summary>
    public string ExportToJSON()
    {
        // Create a wrapper object for JSON serialization
        BuildHistoryData data = new BuildHistoryData
        {
            totalSteps = buildHistory.Count,
            steps = buildHistory
        };

        return JsonUtility.ToJson(data, true);
    }

    /// <summary>
    /// Generate an AI-friendly prompt from the build history
    /// </summary>
    public string GenerateAIPrompt()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine("Generate step-by-step assembly instructions for the following brick construction:");
        sb.AppendLine();
        sb.AppendLine("Build Steps:");

        for (int i = 0; i < buildHistory.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {buildHistory[i].GetDescription()}");
        }

        sb.AppendLine();
        sb.AppendLine("Please create clear, concise assembly instructions that a user could follow to recreate this construction.");

        return sb.ToString();
    }

    /// <summary>
    /// Debug: Print current history to console
    /// </summary>
    [ContextMenu("Print History")]
    public void PrintHistory()
    {
        Debug.Log(GenerateSummary());
    }
}

/// <summary>
/// Wrapper class for JSON serialization of build history
/// </summary>
[System.Serializable]
public class BuildHistoryData
{
    public int totalSteps;
    public List<BuildStep> steps;
}
