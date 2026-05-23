using UnityEngine;

// Pickup object. Reads heal values from Item SO.
// Place in scene with Collider2D (Is Trigger = true).
// MemoryFragmentPickup — Place in world. Applies effects on player contact and destroys itself.
// Requires: Collider2D with IsTrigger enabled, Player Tag = "Player"
// Post-processing:
//   - SanityPostProcessingController under Player or GameObject Scene Global Volume
[RequireComponent(typeof(Collider2D))]
public class MemoryFragmentPickup : MonoBehaviour
{
    [Header("Level Manager")]
    [SerializeField] private LevelManager levelManager;

    [Header("MemoryFragment Data")]
    [SerializeField] private MemoryFragment item; // assign MemoryFragment.asset in Inspector

    [Header("Player Detection")]
    [SerializeField] private LayerMask playerMask;

    [Header("Effects (Optional)")]
    public GameObject pickupVFX;
    public AudioClip pickupSFX;

    private bool isPickedUp = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Trigger hit: {other.gameObject.name} layer: {other.gameObject.layer}");

        if (isPickedUp) return;
        if ((playerMask.value & (1 << other.gameObject.layer)) == 0) return;
        if (item == null)
        {
            Debug.LogWarning("[MemoryFragmentPickup] MemoryFragment: Item SO not assigned.");
            return;
        }

        isPickedUp = true;

        var playerHealth = other.GetComponent<PlayerHealth>();
        var sanitySystem = other.GetComponent<SanitySystem>();
        var inventory = other.GetComponent<PlayerInventory>();
       
        // HP
        if (playerHealth != null && item.healthHealOrDamage != 0)
        {
            if (item.healthHealOrDamage > 0) playerHealth.Heal(item.healthHealOrDamage);
            else playerHealth.TakeDamage(-item.healthHealOrDamage);
        }

        // Sanity
        if (sanitySystem != null && item.sanityHealOrDamage != 0)
        {
            if (item.sanityHealOrDamage > 0) sanitySystem.IncreaseSanity(item.sanityHealOrDamage);
            else sanitySystem.DecreaseSanity(-item.sanityHealOrDamage);
        }

        // Update inventory counts
        if (inventory != null)
        {
            inventory.OnMemoryFragmentPickedUp(item);
        }

        // Post-processing
        // Find in Player/Scene
        var pp = other.GetComponentInChildren<SanityPostProcessingController>()
                 ?? Object.FindFirstObjectByType<SanityPostProcessingController>();
        pp?.TriggerMemoryByFragment(item);

        // GameState
        GameManager.Instance?.AddMemoryFragment();

        // UI Popup
        string hpText = item.healthHealOrDamage != 0 ? $" hp {item.healthHealOrDamage:+#healthHealOrDamage;-#}" : "";
        string sanityText = item.sanityHealOrDamage != 0 ? $" Sanity {item.sanityHealOrDamage:+#;-#}" : "";
        //UIManager.Instance?.ShowPopup($"{item.fragmentName}{hpText}{sanityText}");
        Debug.Log($"[MemoryFragmentPickup] Picked up: {item.fragmentName} — hp {hpText}, sanity {sanityText}");
        var popup = other.GetComponentInChildren<PlayerPopupText>();
        popup?.ShowPopup($"{item.fragmentName}{hpText}{sanityText}");

        // VFX
        if (pickupVFX != null)
            Instantiate(pickupVFX, transform.position, Quaternion.identity);

        // SFX
        float destroyDelay = 0f;
        if (pickupSFX != null)
        {
            destroyDelay = pickupSFX.length;
            PlaySFXDetached(pickupSFX);
        }

        Debug.Log($"[ItemPickup] Picked up: {item.fragmentName} — HP {item.healthHealOrDamage}, Sanity {item.sanityHealOrDamage}");

        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;
        Destroy(gameObject, destroyDelay);
    }

    private void PlaySFXDetached(AudioClip clip)
    {
        GameObject sfxObj = new GameObject("PickupSFX_Temp");
        sfxObj.transform.position = transform.position;
        var src = sfxObj.AddComponent<AudioSource>();
        src.clip = clip;
        src.Play();
        Destroy(sfxObj, clip.length);
    }
}
