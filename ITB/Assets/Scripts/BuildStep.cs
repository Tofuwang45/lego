using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single step in the build history.
/// Supports the "bridge scenario" where one brick can connect to multiple bricks below it.
/// </summary>
[System.Serializable]
public class BuildStep
{
    [Header("Step Identification")]
    [Tooltip("Unique ID for this build step")]
    public string stepID;

    [Tooltip("Timestamp when this step was created")]
    public string timestamp;

    [Header("Brick Information")]
    [Tooltip("The unique ID of the brick that was placed")]
    public string brickID;

    [Tooltip("Human-readable name of the brick (e.g., '3x1_Blue')")]
    public string brickName;

    [Header("Connection Information")]
    [Tooltip("List of brick IDs that this brick is connected to (supports multiple connections)")]
    public List<string> connectedParentIDs;

    [Header("Transform Information")]
    [Tooltip("Local position relative to the build space")]
    public Vector3 localPosition;

    [Tooltip("Local rotation relative to the build space")]
    public Quaternion localRotation;

    [Tooltip("World position (for debugging)")]
    public Vector3 worldPosition;

    /// <summary>
    /// Constructor for a new build step
    /// </summary>
    public BuildStep(string brickID, string brickName, List<string> connectedParents, Vector3 localPos, Quaternion localRot, Vector3 worldPos)
    {
        this.stepID = System.Guid.NewGuid().ToString();
        this.timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        this.brickID = brickID;
        this.brickName = brickName;
        this.connectedParentIDs = connectedParents ?? new List<string>();
        this.localPosition = localPos;
        this.localRotation = localRot;
        this.worldPosition = worldPos;
    }

    /// <summary>
    /// Get a human-readable description of this build step for AI prompt generation
    /// </summary>
    public string GetDescription()
    {
        if (connectedParentIDs == null || connectedParentIDs.Count == 0)
        {
            return $"Placed {brickName} as the foundation brick.";
        }
        else if (connectedParentIDs.Count == 1)
        {
            return $"Attached {brickName} on top of brick {connectedParentIDs[0]}.";
        }
        else
        {
            string parentsList = string.Join(", ", connectedParentIDs);
            return $"Bridged {brickName} across bricks: {parentsList}.";
        }
    }

    /// <summary>
    /// Convert this build step to a JSON-friendly format for AI API
    /// </summary>
    public string ToJSON()
    {
        return JsonUtility.ToJson(this, true);
    }
}
