using System.Collections;
using UnityEngine;

// DamageZone — Applies HP and Sanity penalty when player enters.
// Place a trigger collider at the bottom of each pit.
// Set playerLayer in Inspector to the "Player" layer.
[RequireComponent(typeof(Collider2D))]
public class DamageZone : MonoBehaviour
{
    [Header("Detection")]
    public LayerMask playerLayer;

    [Header("Penalty")]
    public float hpPenalty = 10f;
    public float sanityPenalty = 10f;

    [Header("Cooldown")]
    public float cooldown = 2f;   // seconds before zone can trigger again

    private bool onCooldown = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[DamageZone] Trigger entered by: {other.gameObject.name} (layer: {LayerMask.LayerToName(other.gameObject.layer)})");
        if (onCooldown) return;
        if (((1 << other.gameObject.layer) & playerLayer) == 0)
        {
            Debug.Log("[DamageZone] Layer mismatch — not player layer");
            return;
        }

        Debug.Log($"[DamageZone] Applying penalty — HP -{hpPenalty}, Sanity -{sanityPenalty}");

        other.GetComponent<PlayerHealth>()?.TakeDamage(hpPenalty);
        other.GetComponent<SanitySystem>()?.DecreaseSanity(sanityPenalty);
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        onCooldown = true;
        yield return new WaitForSeconds(cooldown);
        onCooldown = false;
    }
}
