using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tracks active LEGO brick-like objects, records their transform history, and exposes count/lookup helpers.
/// Supports both <see cref="LegoBrick"/> and <see cref="LegoSnapPerfect"/> components.
/// Attach this to a manager object in the scene to keep a rolling memory of block positions.
/// </summary>
public class LegoSceneTracker : MonoBehaviour
{
    /// <summary>
    /// Optional singleton-style access. Not enforced via DontDestroyOnLoad to avoid surprises.
    /// </summary>
    public static LegoSceneTracker Instance { get; private set; }

    [Header("Snapshot Settings")]
    [Tooltip("Capture snapshots automatically each interval.")]
    [SerializeField] private bool autoCaptureSnapshots = true;

    [Tooltip("Seconds between automatic snapshots when enabled.")]
    [SerializeField] private float snapshotInterval = 0.5f;

    [Tooltip("Maximum snapshots remembered per brick (oldest entries are pruned).")]
    [SerializeField] private int maxHistoryEntries = 32;

    [Tooltip("Ignore positional changes smaller than this distance (meters).")]
    [SerializeField] private float minimumMovementThreshold = 0.002f;

    [Tooltip("Ignore angular changes smaller than this angle (degrees).")]
    [SerializeField] private float minimumAngleThreshold = 0.5f;

    [Tooltip("Emit a log each time CaptureSnapshot is called with a reason string.")]
    [SerializeField] private bool logSnapshots = false;

    private readonly Dictionary<int, TrackedBrickState> trackedBricks = new Dictionary<int, TrackedBrickState>();
    private readonly Dictionary<int, TrackedBrickState> trackedByTransform = new Dictionary<int, TrackedBrickState>();
    private readonly List<int> removalBuffer = new List<int>();
    private readonly List<int> transformRemovalBuffer = new List<int>();
    private readonly List<ConnectedGroupInfo> connectedGroups = new List<ConnectedGroupInfo>();
    private readonly Queue<TrackedBrickState> groupTraversalQueue = new Queue<TrackedBrickState>();
    private readonly HashSet<int> visitedGroupIds = new HashSet<int>();
    private readonly HashSet<int> neighborIdScratch = new HashSet<int>();
    private readonly List<TrackedBrickState> tempGroupStates = new List<TrackedBrickState>();
    private readonly List<BrickConnectionInfo> groupConnectionsScratch = new List<BrickConnectionInfo>();
    private readonly HashSet<int> groupMemberIdScratch = new HashSet<int>();
    private readonly Dictionary<(int, int), BrickConnectionInfo> connectionMapScratch = new Dictionary<(int, int), BrickConnectionInfo>();
    private float snapshotTimer;

    /// <summary>
    /// Current count of tracked bricks.
    /// </summary>
    public int BrickCount => trackedByTransform.Count;

    /// <summary>
    /// Number of bricks currently connected to at least one other brick.
    /// </summary>
    public int SnappedBrickCount { get; private set; }

    /// <summary>
    /// Number of distinct connected assemblies containing two or more bricks.
    /// </summary>
    public int SnappedGroupCount { get; private set; }

    /// <summary>
    /// Snapshot of connected brick groupings.
    /// </summary>
    public IReadOnlyList<ConnectedGroupInfo> ConnectedGroups => connectedGroups;

    /// <summary>
    /// Enumerate tracked brick records without exposing the backing collection.
    /// </summary>
    public IEnumerable<TrackedBrickState> TrackedStates => trackedByTransform.Values;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        PruneInvalidEntries();
        RefreshTrackedBricks();
        CaptureSnapshotInternal("OnEnable");
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }
    }

    private void Update()
    {
        if (!autoCaptureSnapshots)
            return;

        snapshotTimer += Time.deltaTime;
        if (snapshotTimer >= Mathf.Max(0.01f, snapshotInterval))
        {
            snapshotTimer = 0f;
            CaptureSnapshotInternal("AutoUpdate");
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        trackedBricks.Clear();
        trackedByTransform.Clear();
        RefreshTrackedBricks();
        CaptureSnapshotInternal($"SceneLoaded:{scene.name}");
    }

    /// <summary>
    /// Scan the scene for bricks and ensure they have tracking records.
    /// </summary>
    public void RefreshTrackedBricks()
    {
        PruneInvalidEntries();

        var bricks = FindObjectsByType<LegoBrick>(FindObjectsSortMode.None);
        foreach (var brick in bricks)
        {
            RegisterBrick(brick);
        }

        var snapBricks = FindObjectsByType<LegoSnapPerfect>(FindObjectsSortMode.None);
        foreach (var snap in snapBricks)
        {
            RegisterSnapBrick(snap);
        }
    }

    /// <summary>
    /// Force tracking for a specific brick. Returns true if a new record was created.
    /// </summary>
    public bool RegisterBrick(LegoBrick brick)
    {
        return RegisterComponent(brick);
    }

    /// <summary>
    /// Force tracking for a <see cref="LegoSnapPerfect"/> brick. Returns true if a new record was created.
    /// </summary>
    public bool RegisterSnapBrick(LegoSnapPerfect snapBrick)
    {
        return RegisterComponent(snapBrick);
    }

    bool RegisterComponent(Component component)
    {
        if (component == null)
            return false;

        int componentId = component.GetInstanceID();
        Transform t = component.transform;
        if (t == null)
            return false;

        int transformId = t.GetInstanceID();

        if (trackedBricks.TryGetValue(componentId, out var existing) && existing != null)
        {
            existing.AttachComponent(component);
            if (existing.Transform != null)
            {
                trackedByTransform[existing.Transform.GetInstanceID()] = existing;
            }
            existing.Update(t.position, t.rotation, Time.realtimeSinceStartup, maxHistoryEntries, minimumMovementThreshold, minimumAngleThreshold);
            return false;
        }

        if (!trackedByTransform.TryGetValue(transformId, out var record) || record == null)
        {
            record = new TrackedBrickState(component);
            trackedByTransform[transformId] = record;
        }
        else
        {
            record.AttachComponent(component);
            trackedByTransform[transformId] = record;
        }

        trackedBricks[componentId] = record;
        record.Update(t.position, t.rotation, Time.realtimeSinceStartup, maxHistoryEntries, minimumMovementThreshold, minimumAngleThreshold);
        return true;
    }

    /// <summary>
    /// Remove a brick from tracking.
    /// </summary>
    public bool UnregisterBrick(LegoBrick brick)
    {
        return UnregisterComponent(brick);
    }

    /// <summary>
    /// Remove a snap brick from tracking.
    /// </summary>
    public bool UnregisterSnapBrick(LegoSnapPerfect snapBrick)
    {
        return UnregisterComponent(snapBrick);
    }

    bool UnregisterComponent(Component component)
    {
        if (component == null)
            return false;

        int componentId = component.GetInstanceID();
        if (!trackedBricks.TryGetValue(componentId, out var state) || state == null)
            return false;

        trackedBricks.Remove(componentId);
        state.DetachComponent(component);

        if (!state.HasAnyComponent || state.Transform == null)
        {
            RemoveStateFromTransformMap(state);
        }

        return true;
    }

    private void RemoveStateFromTransformMap(TrackedBrickState state)
    {
        if (state == null)
            return;

        int transformId = state.TransformId;
        bool removed = false;
        if (transformId != 0 && trackedByTransform.TryGetValue(transformId, out var existing) && ReferenceEquals(existing, state))
        {
            trackedByTransform.Remove(transformId);
            removed = true;
        }

        if (!removed)
        {
            int keyToRemove = -1;
            foreach (var kvp in trackedByTransform)
            {
                if (ReferenceEquals(kvp.Value, state))
                {
                    keyToRemove = kvp.Key;
                    break;
                }
            }

            if (keyToRemove != -1)
            {
                trackedByTransform.Remove(keyToRemove);
            }
        }

        foreach (var compId in state.ComponentIds)
        {
            trackedBricks.Remove(compId);
        }

        state.ClearAllComponents();
    }

    /// <summary>
    /// Try to retrieve the tracking record for a brick.
    /// </summary>
    public bool TryGetState(LegoBrick brick, out TrackedBrickState state)
    {
        return TryGetState((Component)brick, out state);
    }

    /// <summary>
    /// Try to retrieve the tracking record for a snap brick.
    /// </summary>
    public bool TryGetState(LegoSnapPerfect snapBrick, out TrackedBrickState state)
    {
        return TryGetState((Component)snapBrick, out state);
    }

    /// <summary>
    /// Try to retrieve the tracking record for a tracked component.
    /// </summary>
    public bool TryGetState(Component component, out TrackedBrickState state)
    {
        state = null;
        if (component == null)
            return false;

        return trackedBricks.TryGetValue(component.GetInstanceID(), out state);
    }

    /// <summary>
    /// Capture a snapshot of every tracked brick. Optionally include a reason string for debugging.
    /// </summary>
    public void CaptureSnapshot(string reason = null)
    {
        CaptureSnapshotInternal(reason);
    }

    private void CaptureSnapshotInternal(string reason)
    {
        RefreshTrackedBricks();

        double timestamp = Time.realtimeSinceStartup;
        foreach (var record in trackedByTransform.Values)
        {
            if (record == null)
                continue;

            Transform t = record.Transform;
            if (t == null)
                continue;

            record.Update(t.position, t.rotation, timestamp, maxHistoryEntries, minimumMovementThreshold, minimumAngleThreshold);
        }

        if (logSnapshots && !string.IsNullOrEmpty(reason))
        {
            Debug.Log($"LegoSceneTracker: snapshot captured ({reason}) with {BrickCount} bricks tracked.");
        }

        RecalculateConnections();
    }

    private void PruneInvalidEntries()
    {
        removalBuffer.Clear();
        transformRemovalBuffer.Clear();

        foreach (var kvp in trackedByTransform)
        {
            var state = kvp.Value;
            if (state == null || state.Transform == null || !state.HasAnyComponent)
            {
                transformRemovalBuffer.Add(kvp.Key);
                if (state != null)
                {
                    foreach (var compId in state.ComponentIds)
                    {
                        removalBuffer.Add(compId);
                    }
                    state.ClearAllComponents();
                }
            }
        }

        for (int i = 0; i < transformRemovalBuffer.Count; i++)
        {
            trackedByTransform.Remove(transformRemovalBuffer[i]);
        }

        for (int i = 0; i < removalBuffer.Count; i++)
        {
            trackedBricks.Remove(removalBuffer[i]);
        }
    }

    /// <summary>
    /// Rebuild cached connectivity data (snapped groups and counts).
    /// </summary>
    public void RecalculateConnections()
    {
        RebuildConnectedGroups();
    }

    private void RebuildConnectedGroups()
    {
        connectedGroups.Clear();
        SnappedBrickCount = 0;
        SnappedGroupCount = 0;

        visitedGroupIds.Clear();
        groupTraversalQueue.Clear();

        foreach (var kvp in trackedByTransform)
        {
            var state = kvp.Value;
            if (state == null || state.Transform == null)
                continue;

            int id = state.InstanceId;
            if (!visitedGroupIds.Add(id))
                continue;

            tempGroupStates.Clear();

            groupTraversalQueue.Enqueue(state);

            while (groupTraversalQueue.Count > 0)
            {
                var brickState = groupTraversalQueue.Dequeue();
                if (brickState == null || brickState.Transform == null)
                    continue;

                tempGroupStates.Add(brickState);

                CollectConnectedNeighborIds(brickState);
                foreach (var neighborComponentId in neighborIdScratch)
                {
                    if (!trackedBricks.TryGetValue(neighborComponentId, out var neighborState) || neighborState == null || neighborState.Transform == null)
                        continue;

                    int neighborTransformId = neighborState.InstanceId;
                    if (!visitedGroupIds.Add(neighborTransformId))
                        continue;

                    groupTraversalQueue.Enqueue(neighborState);
                }
                neighborIdScratch.Clear();
            }

            if (tempGroupStates.Count <= 1)
                continue;

            var groupInfo = new ConnectedGroupInfo();
            groupInfo.SetBricks(tempGroupStates);
            BuildGroupConnections(tempGroupStates, groupConnectionsScratch);
            groupInfo.SetConnections(groupConnectionsScratch);
            connectedGroups.Add(groupInfo);
            groupConnectionsScratch.Clear();
        }

        for (int i = 0; i < connectedGroups.Count; i++)
        {
            SnappedBrickCount += connectedGroups[i].Size;
        }

        SnappedGroupCount = connectedGroups.Count;
    }

    private void BuildGroupConnections(List<TrackedBrickState> groupStates, List<BrickConnectionInfo> connectionBuffer)
    {
        connectionBuffer.Clear();
        groupMemberIdScratch.Clear();
        connectionMapScratch.Clear();

        for (int i = 0; i < groupStates.Count; i++)
        {
            var state = groupStates[i];
            if (state == null)
                continue;

            int id = state.InstanceId;
            if (id != 0)
                groupMemberIdScratch.Add(id);
        }

        for (int i = 0; i < groupStates.Count; i++)
        {
            var state = groupStates[i];
            if (state == null)
                continue;

            AppendSnapPointConnectionsDetailed(state);
            AppendJointConnectionsDetailed(state);
        }

        foreach (var entry in connectionMapScratch.Values)
        {
            connectionBuffer.Add(entry);
        }

        connectionBuffer.Sort((a, b) =>
        {
            string aLabel = a != null ? a.GetDisplayLabel() : string.Empty;
            string bLabel = b != null ? b.GetDisplayLabel() : string.Empty;
            return string.Compare(aLabel, bLabel, StringComparison.Ordinal);
        });

        connectionMapScratch.Clear();
        groupMemberIdScratch.Clear();
    }

    private void AppendSnapPointConnectionsDetailed(TrackedBrickState state)
    {
        if (state == null)
            return;

        var brick = state.LegoBrick;
        if (brick == null)
            return;

        AppendConnectionList(brick.studSnapPoints);
        AppendConnectionList(brick.socketSnapPoints);

        void AppendConnectionList(List<LegoSnapPoint> points)
        {
            if (points == null)
                return;

            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                if (point == null || !point.isConnected)
                    continue;

                var otherPoint = point.connectedTo;
                if (otherPoint == null)
                    continue;

                var otherBrick = otherPoint.parentBrick;
                if (otherBrick == null)
                    continue;

                var otherState = ResolveStateFromComponent(otherBrick);
                if (otherState == null)
                    continue;

                string detail = null;
                if (!string.IsNullOrEmpty(point.name) || (otherPoint != null && !string.IsNullOrEmpty(otherPoint.name)))
                {
                    if (!string.IsNullOrEmpty(point.name) && otherPoint != null && !string.IsNullOrEmpty(otherPoint.name))
                        detail = point.name + " <-> " + otherPoint.name;
                    else if (!string.IsNullOrEmpty(point.name))
                        detail = point.name;
                    else if (otherPoint != null && !string.IsNullOrEmpty(otherPoint.name))
                        detail = otherPoint.name;
                }

                RecordConnection(state, otherState, BrickConnectionType.SnapPoint, detail);
            }
        }
    }

    private void AppendJointConnectionsDetailed(TrackedBrickState state)
    {
        if (state == null)
            return;

        var snapBrick = state.SnapPerfect;
        if (snapBrick == null)
            return;

        var joints = snapBrick.GetComponents<FixedJoint>();
        for (int i = 0; i < joints.Length; i++)
        {
            var joint = joints[i];
            if (joint == null)
                continue;

            var body = joint.connectedBody;
            if (body == null)
                continue;

            Component candidate = body.GetComponent<LegoBrick>();
            if (candidate == null)
                candidate = body.GetComponent<LegoSnapPerfect>();
            if (candidate == null)
                candidate = body.GetComponentInParent<LegoBrick>();
            if (candidate == null)
                candidate = body.GetComponentInParent<LegoSnapPerfect>();

            var otherState = ResolveStateFromComponent(candidate);
            if (otherState == null)
                continue;

            string detail = !string.IsNullOrEmpty(joint.name) ? joint.name : "FixedJoint";
            RecordConnection(state, otherState, BrickConnectionType.FixedJoint, detail);
        }
    }

    private void RecordConnection(TrackedBrickState stateA, TrackedBrickState stateB, BrickConnectionType type, string detail)
    {
        if (stateA == null || stateB == null)
            return;

        int idA = stateA.InstanceId;
        int idB = stateB.InstanceId;

        if (idA == 0 || idB == 0 || idA == idB)
            return;

        if (!groupMemberIdScratch.Contains(idA) || !groupMemberIdScratch.Contains(idB))
            return;

        var key = CreatePairKey(stateA, stateB, out var first, out var second);

        if (!connectionMapScratch.TryGetValue(key, out var info))
        {
            info = new BrickConnectionInfo();
            info.Initialize(first, second, type, detail);
            connectionMapScratch[key] = info;
        }
        else
        {
            info.Merge(first, second, type, detail);
        }
    }

    private static (int, int) CreatePairKey(TrackedBrickState a, TrackedBrickState b, out TrackedBrickState first, out TrackedBrickState second)
    {
        int idA = a.InstanceId;
        int idB = b.InstanceId;

        if (idA < idB)
        {
            first = a;
            second = b;
            return (idA, idB);
        }

        first = b;
        second = a;
        return (idB, idA);
    }

    private TrackedBrickState ResolveStateFromComponent(Component component)
    {
        if (component == null)
            return null;

        if (trackedBricks.TryGetValue(component.GetInstanceID(), out var byComponent) && byComponent != null)
            return byComponent;

        var transform = component.transform;
        if (transform == null)
            return null;

        trackedByTransform.TryGetValue(transform.GetInstanceID(), out var byTransform);
        return byTransform;
    }

    private void CollectConnectedNeighborIds(TrackedBrickState state)
    {
        neighborIdScratch.Clear();
        if (state == null)
            return;

        if (state.LegoBrick != null)
        {
            AppendSnapPointConnections(state.LegoBrick, state.InstanceId);
        }

        if (state.SnapPerfect != null)
        {
            AppendJointConnections(state.SnapPerfect, state.InstanceId);
        }
    }

    private void AppendSnapPointConnections(LegoBrick brick, int selfId)
    {
        if (brick == null)
            return;

        AppendConnections(brick.studSnapPoints);
        AppendConnections(brick.socketSnapPoints);

        void AppendConnections(List<LegoSnapPoint> points)
        {
            if (points == null)
                return;

            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                if (point == null || !point.isConnected)
                    continue;

                var otherPoint = point.connectedTo;
                if (otherPoint == null)
                    continue;

                var otherBrick = otherPoint.parentBrick;
                if (otherBrick == null)
                    continue;

                int otherId = otherBrick.GetInstanceID();
                var otherTransform = otherBrick.transform;
                if (otherTransform != null && otherTransform.GetInstanceID() == selfId)
                    continue;

                neighborIdScratch.Add(otherId);
            }
        }
    }

    private void AppendJointConnections(LegoSnapPerfect snapBrick, int selfId)
    {
        if (snapBrick == null)
            return;

        var joints = snapBrick.GetComponents<FixedJoint>();
        for (int i = 0; i < joints.Length; i++)
        {
            var joint = joints[i];
            if (joint == null)
                continue;

            var body = joint.connectedBody;
            if (body == null)
                continue;

            Component candidate = body.GetComponent<LegoBrick>();
            if (candidate == null)
                candidate = body.GetComponent<LegoSnapPerfect>();
            if (candidate == null)
                candidate = body.GetComponentInParent<LegoBrick>();
            if (candidate == null)
                candidate = body.GetComponentInParent<LegoSnapPerfect>();

            if (candidate == null)
                continue;

            int otherId = candidate.GetInstanceID();
            Transform otherTransform = candidate.transform;
            if (otherTransform != null && otherTransform.GetInstanceID() == selfId)
                continue;

            neighborIdScratch.Add(otherId);
        }
    }

    [System.Serializable]
    public class TrackedBrickState
    {
        public LegoBrick LegoBrick { get; private set; }
        public LegoSnapPerfect SnapPerfect { get; private set; }
        public Transform Transform { get; private set; }
        public int TransformId { get; private set; }
        public int InstanceId => TransformId;
        public string BrickName => Transform != null ? Transform.name : (cachedName ?? "(missing)");
        public Vector3 LastPosition { get; private set; }
        public Quaternion LastRotation { get; private set; }
        public IReadOnlyList<BrickSnapshot> History => history;
        public bool HasAnyComponent => componentIds.Count > 0;
        public IReadOnlyCollection<int> ComponentIds => componentIds;

        private readonly List<BrickSnapshot> history = new List<BrickSnapshot>();
        private readonly HashSet<int> componentIds = new HashSet<int>();
        private string cachedName;

        public TrackedBrickState(Component component)
        {
            AttachComponent(component);
        }

        internal void AttachComponent(Component component)
        {
            if (component == null)
                return;

            componentIds.Add(component.GetInstanceID());

            if (component is LegoBrick legoBrick)
                LegoBrick = legoBrick;

            if (component is LegoSnapPerfect snapPerfect)
                SnapPerfect = snapPerfect;

            var transform = component.transform;
            if (transform != null)
            {
                Transform = transform;
                TransformId = transform.GetInstanceID();
                cachedName = transform.name;
            }
            else if (cachedName == null)
            {
                cachedName = component.name;
            }
        }

        internal void DetachComponent(Component component)
        {
            if (component == null)
                return;

            componentIds.Remove(component.GetInstanceID());

            if (component == LegoBrick)
                LegoBrick = null;

            if (component == SnapPerfect)
                SnapPerfect = null;

            if (!HasAnyComponent)
            {
                componentIds.Clear();
                Transform = null;
                TransformId = 0;
            }
        }

        internal void ClearAllComponents()
        {
            componentIds.Clear();
            LegoBrick = null;
            SnapPerfect = null;
            Transform = null;
            TransformId = 0;
        }

        internal void Update(Vector3 position, Quaternion rotation, double timestamp, int maxHistoryEntries, float movementThreshold, float angleThreshold)
        {
            if (history.Count > 0)
            {
                float movedSqr = (position - LastPosition).sqrMagnitude;
                float angle = Quaternion.Angle(rotation, LastRotation);
                if (movedSqr < movementThreshold * movementThreshold && angle < angleThreshold)
                    return;
            }

            LastPosition = position;
            LastRotation = rotation;

            if (Transform != null)
                cachedName = Transform.name;

            history.Add(new BrickSnapshot
            {
                Timestamp = timestamp,
                Position = position,
                Rotation = rotation
            });

            if (maxHistoryEntries > 0 && history.Count > maxHistoryEntries)
            {
                int overflow = history.Count - maxHistoryEntries;
                history.RemoveRange(0, overflow);
            }
        }
    }

    [System.Serializable]
    public struct BrickSnapshot
    {
        public double Timestamp;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    [System.Serializable]
    public enum BrickConnectionType
    {
        SnapPoint,
        FixedJoint,
        Mixed
    }

    [System.Serializable]
    public class BrickConnectionInfo
    {
        public TrackedBrickState A { get; private set; }
        public TrackedBrickState B { get; private set; }
        public BrickConnectionType Type { get; private set; }
        public string Detail { get; private set; }

        internal void Initialize(TrackedBrickState a, TrackedBrickState b, BrickConnectionType type, string detail)
        {
            A = a;
            B = b;
            Type = type;
            Detail = string.Empty;
            AddDetail(detail);
        }

        internal void Merge(TrackedBrickState a, TrackedBrickState b, BrickConnectionType type, string detail)
        {
            A = a;
            B = b;

            if (Type != type)
                Type = BrickConnectionType.Mixed;

            AddDetail(detail);
        }

        internal void AddDetail(string detail)
        {
            if (string.IsNullOrWhiteSpace(detail))
                return;

            if (string.IsNullOrEmpty(Detail))
            {
                Detail = detail;
            }
            else if (!Detail.Contains(detail))
            {
                Detail += ", " + detail;
            }
        }

        internal string GetDisplayLabel()
        {
            string left = A != null ? A.BrickName : "?";
            string right = B != null ? B.BrickName : "?";
            return left + "-" + right;
        }
    }

    [System.Serializable]
    public class ConnectedGroupInfo
    {
        private readonly List<TrackedBrickState> bricks = new List<TrackedBrickState>();
        private readonly List<BrickConnectionInfo> connections = new List<BrickConnectionInfo>();

        /// <summary>
        /// Bricks participating in this snapped assembly.
        /// </summary>
        public IReadOnlyList<TrackedBrickState> Bricks => bricks;

        /// <summary>
        /// Number of bricks in the group.
        /// </summary>
        public int Size => bricks.Count;

        /// <summary>
        /// Connections linking bricks within the group.
        /// </summary>
        public IReadOnlyList<BrickConnectionInfo> Connections => connections;

        /// <summary>
        /// True if at least one connection was detected for this group.
        /// </summary>
        public bool HasConnections => connections.Count > 0;

        internal void SetBricks(List<TrackedBrickState> source)
        {
            bricks.Clear();
            bricks.AddRange(source);
        }

        internal void SetConnections(List<BrickConnectionInfo> source)
        {
            connections.Clear();
            connections.AddRange(source);
        }
    }
}
