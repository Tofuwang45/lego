using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Bridges XR interaction events to the LegoBrick grab/release lifecycle.
/// Attach this to the same GameObject as a <see cref="LegoBrick"/> and an <see cref="XRGrabInteractable"/>.
/// Compatible with Unity 6.2 and XR Interaction Toolkit 3.0+.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class LegoXRGrabbable : MonoBehaviour
{
    private LegoBrick legoBrick;
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        legoBrick = GetComponent<LegoBrick>();
        if (legoBrick == null)
        {
            Debug.LogWarning(name + ": LegoBrick component not found on the same GameObject.");
        }

        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnSelectEntered);
            grabInteractable.selectExited.AddListener(OnSelectExited);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (legoBrick != null)
            legoBrick.OnGrabbed();
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (legoBrick != null)
            legoBrick.OnReleased();
    }
}
