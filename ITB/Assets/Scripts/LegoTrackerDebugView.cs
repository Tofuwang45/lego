using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.XR.Templates.MR;

/// <summary>
/// Bridges the <see cref="LegoSceneTracker"/> data into a floating debug panel driven by <see cref="DebugInfoDisplayController"/>.
/// Attach this alongside the controller to surface live block counts and recent positions in-game.
/// </summary>
[DisallowMultipleComponent]
public class LegoTrackerDebugView : MonoBehaviour
{
    [Header("View References")]
    [SerializeField] private DebugInfoDisplayController debugDisplay;

    [Tooltip("Optional explicit tracker reference. Falls back to LegoSceneTracker.Instance if left empty.")]
    [SerializeField] private LegoSceneTracker tracker;

    [Header("Display Settings")]
    [Tooltip("How many tracked bricks to list each refresh (ordered by most recent movement).")]
    [SerializeField] private int maxEntries = 6;

    [Tooltip("How many snapped assemblies to summarize each refresh (ordered by group size).")]
    [SerializeField] private int maxGroupSummaries = 3;

    [Tooltip("Show the age of the last snapshot for each listed brick.")]
    [SerializeField] private bool showSnapshotAge = true;

    [Tooltip("Hide the widget automatically when no bricks are tracked.")]
    [SerializeField] private bool hideWhenEmpty = true;

    [Tooltip("Display the total number of bricks tracked (in addition to snapped counts).")]
    [SerializeField] private bool showTotalBrickCount = true;

    [Tooltip("Display the number of snapped assemblies currently detected.")]
    [SerializeField] private bool showSnappedGroupCount = true;

    [Tooltip("Display the number of bricks that are not snapped to anything.")]
    [SerializeField] private bool showLooseBrickCount = true;

    [Tooltip("Show the latest world position for each listed brick entry.")]
    [SerializeField] private bool showBrickPositions = true;

    [Tooltip("Summarize detected assemblies by listing member brick names.")]
    [SerializeField] private bool listGroupSummaries = true;

    [Tooltip("Maximum number of member names to show per group summary.")]
    [SerializeField] private int maxGroupMemberNames = 3;

    [Tooltip("Display direct connection pairs for each assembly.")]
    [SerializeField] private bool showGroupConnectionPairs = true;

    [Tooltip("Maximum number of connection pairs to show per assembly.")]
    [SerializeField] private int maxGroupConnectionPairs = 4;

    [Tooltip("If enabled, mirror the current panel contents to the Unity console each refresh.")]
    [SerializeField] private bool logToConsole = false;

    private readonly List<LegoSceneTracker.TrackedBrickState> tempStates = new List<LegoSceneTracker.TrackedBrickState>();
    private readonly List<LegoSceneTracker.ConnectedGroupInfo> groupSummaryBuffer = new List<LegoSceneTracker.ConnectedGroupInfo>();
    private readonly StringBuilder consoleBuilder = new StringBuilder(256);
    private readonly StringBuilder groupBuilder = new StringBuilder(128);
    private readonly HashSet<int> snappedIdScratch = new HashSet<int>();

    private void Awake()
    {
        if (debugDisplay == null)
            debugDisplay = GetComponent<DebugInfoDisplayController>();

        if (tracker == null)
            tracker = LegoSceneTracker.Instance;
    }

    private void Update()
    {
        if (tracker == null)
            tracker = LegoSceneTracker.Instance;

        var hasTracker = tracker != null;
        if (debugDisplay != null)
            debugDisplay.Show(!hideWhenEmpty || (hasTracker && tracker.BrickCount > 0));

        if (!hasTracker || debugDisplay == null)
            return;

        tracker.RefreshTrackedBricks();
        tracker.RecalculateConnections();

        consoleBuilder.Clear();

        int totalBricks = tracker.BrickCount;
        int snappedBricks = tracker.SnappedBrickCount;
        int snappedGroups = tracker.SnappedGroupCount;
        int looseBricks = Mathf.Max(0, totalBricks - snappedBricks);

        if (showTotalBrickCount)
        {
            debugDisplay.AppendDebugEntry("Total Bricks", totalBricks.ToString());
            if (logToConsole)
                consoleBuilder.AppendLine($"Total Bricks: {totalBricks}");
        }

        debugDisplay.AppendDebugEntry("Snapped Bricks", snappedBricks.ToString());
        if (logToConsole)
            consoleBuilder.AppendLine($"Snapped Bricks: {snappedBricks}");

        if (showSnappedGroupCount)
        {
            debugDisplay.AppendDebugEntry("Assemblies", snappedGroups.ToString());
            if (logToConsole)
                consoleBuilder.AppendLine($"Assemblies: {snappedGroups}");
        }

        if (showLooseBrickCount)
        {
            debugDisplay.AppendDebugEntry("Loose Bricks", looseBricks.ToString());
            if (logToConsole)
                consoleBuilder.AppendLine($"Loose Bricks: {looseBricks}");
        }

        snappedIdScratch.Clear();
        var groups = tracker.ConnectedGroups;
        for (int g = 0; g < groups.Count; g++)
        {
            var group = groups[g];
            if (group == null)
                continue;

            var bricks = group.Bricks;
            for (int i = 0; i < bricks.Count; i++)
            {
                var state = bricks[i];
                if (state == null)
                    continue;
                int id = state.InstanceId;
                if (id != 0)
                    snappedIdScratch.Add(id);
            }
        }

        double now = Time.realtimeSinceStartup;

        if (listGroupSummaries && groups.Count > 0 && maxGroupSummaries > 0)
        {
            groupSummaryBuffer.Clear();
            groupSummaryBuffer.AddRange(groups);
            groupSummaryBuffer.Sort((a, b) => b.Size.CompareTo(a.Size));

            int groupsToShow = Mathf.Min(maxGroupSummaries, groupSummaryBuffer.Count);

            for (int i = 0; i < groupsToShow; i++)
            {
                var group = groupSummaryBuffer[i];
                if (group == null)
                    continue;

                string label = $"Assembly {i + 1} ({group.Size})";
                string entry = FormatGroupSummary(group, now);

                debugDisplay.AppendDebugEntry(label, entry);
                if (logToConsole)
                    consoleBuilder.AppendLine($"{label}: {entry}");

                if (showGroupConnectionPairs && group.HasConnections)
                {
                    string connectionLabel = label + " Links";
                    string connectionsEntry = FormatGroupConnections(group);

                    debugDisplay.AppendDebugEntry(connectionLabel, connectionsEntry);
                    if (logToConsole)
                        consoleBuilder.AppendLine($"{connectionLabel}: {connectionsEntry}");
                }
            }
        }

        tempStates.Clear();
        foreach (var state in tracker.TrackedStates)
        {
            if (state == null || state.Transform == null)
                continue;
            tempStates.Add(state);
        }

        tempStates.Sort((a, b) =>
        {
            double aTime = GetLatestTimestamp(a);
            double bTime = GetLatestTimestamp(b);
            return bTime.CompareTo(aTime);
        });

        if (maxEntries > 0)
        {
            int removeStart = Mathf.Min(maxEntries, tempStates.Count);
            if (removeStart < tempStates.Count)
                tempStates.RemoveRange(removeStart, tempStates.Count - removeStart);
        }

        for (int i = 0; i < tempStates.Count; i++)
        {
            var state = tempStates[i];
            int stateId = state.InstanceId;
            bool isSnapped = stateId != 0 && snappedIdScratch.Contains(stateId);
            string label = state.BrickName ?? $"Brick {state.InstanceId}";
            string entry = string.Empty;

            if (showBrickPositions)
            {
                entry = FormatVector(state.LastPosition);
            }

            if (showSnapshotAge)
            {
                double age = now - GetLatestTimestamp(state);
                if (age < 0d)
                    age = 0d;
                entry = AppendWithSeparator(entry, $"{age:0.00}s");
            }

            if (string.IsNullOrEmpty(entry) && isSnapped)
            {
                entry = "Snapped";
            }

            debugDisplay.AppendDebugEntry(label, entry);

            if (logToConsole)
                consoleBuilder.AppendLine($"{label}: {entry}");
        }

        debugDisplay.RefreshDisplayInfo();

        if (logToConsole && consoleBuilder.Length > 0)
            Debug.Log($"[LegoTrackerDebugView]\n{consoleBuilder.ToString().TrimEnd()}", this);
    }

    private string FormatGroupSummary(LegoSceneTracker.ConnectedGroupInfo group, double now)
    {
        groupBuilder.Clear();

        var bricks = group.Bricks;
        int namesToShow = maxGroupMemberNames > 0 ? Mathf.Min(maxGroupMemberNames, bricks.Count) : 0;
        double freshest = double.MinValue;

        for (int i = 0; i < bricks.Count; i++)
        {
            var state = bricks[i];
            if (state == null || state.Transform == null)
                continue;

            double stamp = GetLatestTimestamp(state);
            if (stamp > freshest)
                freshest = stamp;

            if (i >= namesToShow)
                continue;

            if (groupBuilder.Length > 0)
                groupBuilder.Append(", ");

            groupBuilder.Append(state.BrickName ?? $"Brick {state.InstanceId}");
        }

        if (bricks.Count > namesToShow)
            groupBuilder.Append(", ...");

        if (showSnapshotAge && freshest > double.MinValue)
        {
            double age = now - freshest;
            if (age < 0d)
                age = 0d;
            groupBuilder.Append($" | {age:0.00}s");
        }

        return groupBuilder.Length > 0 ? groupBuilder.ToString() : "(no data)";
    }

    private string FormatGroupConnections(LegoSceneTracker.ConnectedGroupInfo group)
    {
        groupBuilder.Clear();

        var connections = group.Connections;
        if (connections == null || connections.Count == 0)
            return "(no connections)";

        int pairsToShow = maxGroupConnectionPairs > 0 ? Mathf.Min(maxGroupConnectionPairs, connections.Count) : connections.Count;

        for (int i = 0; i < pairsToShow; i++)
        {
            var connection = connections[i];
            if (connection == null)
                continue;

            if (groupBuilder.Length > 0)
                groupBuilder.Append(" | ");

            string left = connection.A != null ? connection.A.BrickName : "?";
            string right = connection.B != null ? connection.B.BrickName : "?";
            string detail = connection.Detail;

            string descriptor = left + " <-> " + right;

            if (!string.IsNullOrEmpty(detail))
            {
                descriptor += " (" + detail + ")";
            }
            else
            {
                descriptor += " (" + DescribeConnectionType(connection.Type) + ")";
            }

            groupBuilder.Append(descriptor);
        }

        if (connections.Count > pairsToShow)
            groupBuilder.Append(" | ...");

        if (groupBuilder.Length == 0)
            groupBuilder.Append("(no connections)");

        return groupBuilder.ToString();
    }

    private static string FormatVector(Vector3 value)
    {
        return $"({value.x:0.000}, {value.y:0.000}, {value.z:0.000})";
    }

    private static string AppendWithSeparator(string current, string addition)
    {
        if (string.IsNullOrEmpty(addition))
            return current ?? string.Empty;

        if (string.IsNullOrEmpty(current))
            return addition;

        return current + " | " + addition;
    }

    private static string DescribeConnectionType(LegoSceneTracker.BrickConnectionType type)
    {
        switch (type)
        {
            case LegoSceneTracker.BrickConnectionType.SnapPoint:
                return "Snap";
            case LegoSceneTracker.BrickConnectionType.FixedJoint:
                return "Joint";
            case LegoSceneTracker.BrickConnectionType.Mixed:
                return "Mixed";
            default:
                return "Unknown";
        }
    }

    private static double GetLatestTimestamp(LegoSceneTracker.TrackedBrickState state)
    {
        var history = state.History;
        if (history == null || history.Count == 0)
            return double.MinValue;

        return history[history.Count - 1].Timestamp;
    }
}
