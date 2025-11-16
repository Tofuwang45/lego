using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manager responsible for tracking bricks and connection logs.
/// Provides a simple singleton access via <see cref="Instance"/>.
/// </summary>
public class LegoSnapManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static LegoSnapManager Instance;

    /// <summary>
    /// All registered bricks in the scene.
    /// </summary>
    private List<LegoBrick> allBricks = new List<LegoBrick>();

    /// <summary>
    /// History of assembly steps (connections made between bricks).
    /// </summary>
    private List<ConnectionLog> assemblySteps = new List<ConnectionLog>();

    /// <summary>
    /// Serializable record of a single connection step in the assembly.
    /// </summary>
    [System.Serializable]
    public class ConnectionLog
    {
        /// <summary>
        /// Sequential step number for the log entry.
        /// </summary>
        public int stepNumber;

        /// <summary>
        /// The brick that was moved/snapped in this step.
        /// </summary>
        public LegoBrick movingBrick;

        /// <summary>
        /// The brick that served as the snap target in this step.
        /// </summary>
        public LegoBrick targetBrick;

        /// <summary>
        /// Indices of studs involved in the connection (x,z per stud) as Vector2Int.
        /// </summary>
        public List<Vector2Int> studIndices;

        /// <summary>
        /// Timestamp (seconds) when the connection occurred.
        /// </summary>
        public float timestamp;
    }

    /// <summary>
    /// Awake - enforce singleton pattern. If another instance exists, destroy this GameObject.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Register a brick with the manager so its snap points can be searched.
    /// </summary>
    /// <param name="brick">The brick to register.</param>
    public void RegisterBrick(LegoBrick brick)
    {
        if (brick == null)
            return;

        if (!allBricks.Contains(brick))
            allBricks.Add(brick);
    }

    /// <summary>
    /// Find nearby snap points of the given type around a world position within the given radius.
    /// </summary>
    /// <param name="position">World position to search around.</param>
    /// <param name="radius">Search radius in world units.</param>
    /// <param name="type">Snap point type to search for (Stud or Socket).</param>
    /// <returns>List of matching <see cref="LegoSnapPoint"/> objects.</returns>
    public List<LegoSnapPoint> FindNearbySnapPoints(Vector3 position, float radius, LegoSnapPoint.SnapPointType type)
    {
        var results = new List<LegoSnapPoint>();

        foreach (var brick in allBricks)
        {
            if (brick == null)
                continue;

            List<LegoSnapPoint> points = (type == LegoSnapPoint.SnapPointType.Stud) ? brick.studSnapPoints : brick.socketSnapPoints;
            if (points == null)
                continue;

            foreach (var p in points)
            {
                if (p == null)
                    continue;

                if (p.isConnected)
                    continue;

                if (Vector3.Distance(p.transform.position, position) < radius)
                {
                    results.Add(p);
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Log a connection between a stud and a socket. Currently logs a debug message; later this will record a ConnectionLog entry.
    /// </summary>
    /// <param name="stud">The stud snap point.</param>
    /// <param name="socket">The socket snap point.</param>
    public void LogConnection(LegoSnapPoint stud, LegoSnapPoint socket)
    {
        if (stud == null || socket == null)
            return;

        string moving = stud.parentBrick != null ? stud.parentBrick.name : "(unknown)";
        string target = socket.parentBrick != null ? socket.parentBrick.name : "(unknown)";

        Debug.LogFormat("LegoSnapManager: Connection logged - movingBrick={0}, targetBrick={1}", moving, target);

        // Future: create a ConnectionLog entry and add to assemblySteps
    }
}
