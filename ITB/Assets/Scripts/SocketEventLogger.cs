using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

/// <summary>
/// Listens to XR Socket Interactor events and logs build steps to the BuildHistoryManager.
/// Attach this to GameObjects with XRSocketInteractor components.
/// </summary>
[RequireComponent(typeof(XRSocketInteractor))]
public class SocketEventLogger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the socket interactor (auto-assigned if null)")]
    public XRSocketInteractor socketInteractor;

    [Header("Settings")]
    [Tooltip("Delay before scanning connections (allows physics to settle)")]
    [Range(0f, 1f)]
    public float scanDelay = 0.1f;

    [Tooltip("Transform to use as reference for local position (usually the build platform)")]
    public Transform buildSpaceReference;

    [Header("Debug")]
    public bool enableDebugLog = true;

    private void Awake()
    {
        // Auto-assign socket interactor if not set
        if (socketInteractor == null)
        {
            socketInteractor = GetComponent<XRSocketInteractor>();
        }

        if (socketInteractor == null)
        {
            Debug.LogError($"SocketEventLogger on {gameObject.name} requires an XRSocketInteractor component!");
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        // Subscribe to socket events
        if (socketInteractor != null)
        {
            socketInteractor.selectEntered.AddListener(OnSocketSnap);
            socketInteractor.selectExited.AddListener(OnSocketRelease);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from socket events
        if (socketInteractor != null)
        {
            socketInteractor.selectEntered.RemoveListener(OnSocketSnap);
            socketInteractor.selectExited.RemoveListener(OnSocketRelease);
        }
    }

    /// <summary>
    /// Called when a brick snaps into the socket
    /// </summary>
    private void OnSocketSnap(SelectEnterEventArgs args)
    {
        // Get the interactable that just snapped (the brick)
        IXRSelectInteractable interactable = args.interactableObject;
        GameObject brickObject = interactable.transform.gameObject;

        // Get the BrickIdentifier component
        BrickIdentifier brickId = brickObject.GetComponent<BrickIdentifier>();
        if (brickId == null)
        {
            Debug.LogWarning($"Snapped object {brickObject.name} does not have a BrickIdentifier component!");
            return;
        }

        // Get the BrickScanner component
        BrickScanner scanner = brickObject.GetComponent<BrickScanner>();
        if (scanner == null)
        {
            Debug.LogWarning($"Snapped object {brickObject.name} does not have a BrickScanner component!");
            return;
        }

        // Wait a moment for physics to settle, then scan and log
        StartCoroutine(ScanAndLogAfterDelay(brickId, scanner, brickObject.transform));
    }

    /// <summary>
    /// Coroutine to scan connections after a short delay
    /// </summary>
    private System.Collections.IEnumerator ScanAndLogAfterDelay(BrickIdentifier brickId, BrickScanner scanner, Transform brickTransform)
    {
        // Wait for physics to settle
        yield return new WaitForSeconds(scanDelay);

        // Scan for connected bricks
        List<string> connectedBrickIDs = scanner.GetConnectedBricks();

        // Get position information
        Vector3 worldPos = brickTransform.position;
        Vector3 localPos = worldPos;
        Quaternion localRot = brickTransform.rotation;

        // If we have a build space reference, get local coordinates relative to it
        if (buildSpaceReference != null)
        {
            localPos = buildSpaceReference.InverseTransformPoint(worldPos);
            localRot = Quaternion.Inverse(buildSpaceReference.rotation) * brickTransform.rotation;
        }

        // Create a new build step
        BuildStep step = new BuildStep(
            brickID: brickId.uniqueID,
            brickName: brickId.brickName,
            connectedParents: connectedBrickIDs,
            localPos: localPos,
            localRot: localRot,
            worldPos: worldPos
        );

        // Add to history
        BuildHistoryManager.Instance.AddBuildStep(step);

        if (enableDebugLog)
        {
            if (connectedBrickIDs.Count == 0)
            {
                Debug.Log($"[SocketLogger] {brickId.brickName} placed as foundation brick");
            }
            else if (connectedBrickIDs.Count == 1)
            {
                Debug.Log($"[SocketLogger] {brickId.brickName} connected to 1 brick");
            }
            else
            {
                Debug.Log($"[SocketLogger] {brickId.brickName} bridged across {connectedBrickIDs.Count} bricks");
            }
        }
    }

    /// <summary>
    /// Called when a brick is removed from the socket
    /// </summary>
    private void OnSocketRelease(SelectExitEventArgs args)
    {
        // Get the interactable that was released
        IXRSelectInteractable interactable = args.interactableObject;
        GameObject brickObject = interactable.transform.gameObject;

        // Get the BrickIdentifier component
        BrickIdentifier brickId = brickObject.GetComponent<BrickIdentifier>();
        if (brickId == null) return;

        // Remove from history
        BuildHistoryManager.Instance.RemoveBuildStep(brickId.uniqueID);

        if (enableDebugLog)
        {
            Debug.Log($"[SocketLogger] {brickId.brickName} removed from construction");
        }
    }
}
