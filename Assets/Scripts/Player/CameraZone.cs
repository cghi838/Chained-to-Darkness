using UnityEngine;

// CameraZone — Place on an empty GameObject with a BoxCollider2D (Is Trigger).
//
// When the player walks into this zone the CameraFollow system will:
//   - Clamp the camera to the zone's bounds
//   - Optionally change the orthographic zoom
//   - Optionally change the mental-state behaviour
//
// Setup:
//   1. Create an empty GameObject in your scene.
//   2. Add a BoxCollider2D and tick "Is Trigger".
//   3. Add this component.
//   4. Tag the Player GameObject as "Player".
//   5. Leave cameraFollow blank — it auto-finds Camera.main's CameraFollow on Awake.

public class CameraZone : MonoBehaviour
{
    [Header("Camera Reference")]
    [Tooltip("Leave blank to auto-find Camera.main's CameraFollow on Awake.")]
    public CameraFollow cameraFollow;

    [Header("Bounds")]
    public bool overrideBounds = true;
    public float minX = -10f;
    public float maxX = 10f;
    public float minY = -5f;
    public float maxY = 5f;
    public float transitionDuration = 0.8f;

    [Header("Zoom")]
    public bool overrideZoom = false;
    public float targetOrthoSize = 5f;
    public bool resetZoomOnExit = true;

    [Header("Mental State")]
    public bool overrideMentalState = false;
    public CameraFollow.MentalStateLevel mentalState = CameraFollow.MentalStateLevel.Stable;
    [Range(0f, 1f)]
    public float mentalStateIntensity = 0.5f;

    [Header("Exit Behaviour")]
    public bool clearBoundsOnExit = false;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (cameraFollow == null && Camera.main != null)
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (cameraFollow == null) return;

        cameraFollow.OnEnterZone(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (cameraFollow == null) return;

        cameraFollow.OnExitZone(this);
    }

    // -------------------------------------------------------------------------
    //  Editor Gizmo — draws the zone bounds and labels them in the Scene view
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        float w = maxX - minX;
        float h = maxY - minY;
        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);

        // Filled tint
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.15f);
        Gizmos.DrawCube(center, new Vector3(w, h, 0.1f));

        // Outline
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        Gizmos.DrawWireCube(center, new Vector3(w, h, 0.1f));

        // Scene-view label
        UnityEditor.Handles.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        string label = gameObject.name;
        if (overrideMentalState) label += "\n[" + mentalState + " x" + mentalStateIntensity.ToString("0.0") + "]";
        if (overrideZoom)        label += "\n[Zoom " + targetOrthoSize + "]";
        UnityEditor.Handles.Label(center, label);
    }
#endif
}