using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Duplicate : MonoBehaviour
{
    [Tooltip("Offset applied to the duplicated copy relative to this object")]
    public Vector3 offset = new Vector3(1f, 0f, 0f);

    [Tooltip("If true, clones will also have this Duplicate script and be clickable")]
    public bool clonesHaveScript = false;

    // Called by Unity when this object is clicked (requires a Collider)
    void OnMouseDown()
    {
        Vector3 spawnPos = transform.position + offset;
        GameObject clone = Instantiate(gameObject, spawnPos, transform.rotation);

        if (!clonesHaveScript)
        {
            Duplicate dup = clone.GetComponent<Duplicate>();
            if (dup != null)
            {
                Destroy(dup);
            }
        }

        // Give the clone a distinct name
        clone.name = gameObject.name + "_copy";
    }

    void Start()
    {
        // Warn if there is no collider — OnMouseDown requires one
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning(name + ": no Collider found — clicks won't be detected. Add a Collider or enable raycast handling.");
        }
    }
}
