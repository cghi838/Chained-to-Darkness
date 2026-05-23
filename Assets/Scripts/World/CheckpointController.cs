using UnityEngine;

// CheckpointController — Registers respawn position when the player passes through.
// Requires: Collider2D with IsTrigger enabled.
// Set playerLayer in Inspector to the "Player" layer.
// Call CheckpointController.Current.Respawn(player) from PlayerHealth.Die()
[RequireComponent(typeof(Collider2D))]
public class CheckpointController : MonoBehaviour
{
    [Header("Detection")]
    public LayerMask playerLayer;

    [Header("Respawn Settings")]
    public float respawnSanity = 50f;

    [Header("Visual")]
    public GameObject activatedVFX;

    private bool activated = false;

    public static CheckpointController Current { get; private set; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Checkpoint] Trigger entered by: {other.gameObject.name} (layer: {LayerMask.LayerToName(other.gameObject.layer)})");

        if (((1 << other.gameObject.layer) & playerLayer) == 0)
        {
            Debug.Log("[Checkpoint] Layer mismatch — not player layer");
            return;
        }

        if (activated)
        {
            Debug.Log("[Checkpoint] Already activated");
            return;
        }

        activated = true;
        Current = this;

        if (activatedVFX != null)
            activatedVFX.SetActive(true);

        Debug.Log($"[Checkpoint] Activated at {transform.position}");
    }

    // Teleports player to this checkpoint and restores Sanity.
    // Call this from PlayerHealth when HP reaches 0.
    public void Respawn(GameObject player)
    {
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        player.transform.position = transform.position;

        var sanity = player.GetComponent<SanitySystem>();
        if (sanity != null) sanity.SetSanityDirect(respawnSanity);

        Debug.Log($"[Checkpoint] Respawned — Sanity restored to {respawnSanity}");
    }
}
