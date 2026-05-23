using UnityEngine;

public class Damage : MonoBehaviour
{
    [Header("Player Hit Setting")]
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private float damageToPlayer = 10f;

    [Header("Effects")]
    public GameObject damageVFX;
    public AudioClip damageSFX;

    [Header("Destory After Hit")]
    public bool destroyAfterHit = false;

    [Header("BossScript")]
    [SerializeField] BossBehavior bossBehavior;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Debug.Log("~~~~~~~~~~damage");
        if ((playerMask.value & (1 << other.gameObject.layer)) == 0) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        SanitySystem sanitySystem = other.GetComponent<SanitySystem>();
        // HP
        if (playerHealth != null)
        {
            // Debug.Log("Damage to Player health");
            playerHealth.TakeDamage(damageToPlayer);
        }
        // Sanity
        if (sanitySystem != null)
        {
            sanitySystem.DecreaseSanity(damageToPlayer); // Example sanity damage
        }

        // VFX
        if (damageVFX != null)
        {
            Debug.Log("Damage VFX");
            Instantiate(damageVFX, transform.position, Quaternion.identity);
        }

        // SFX
        float destroyDelay = 0f;
        if (damageSFX != null)
        {
            destroyDelay = damageSFX.length;
            PlaySFXDetached(damageSFX);
        }

        if (destroyAfterHit)
        {
            Destroy(gameObject, destroyDelay);
        }

        if (bossBehavior != null)
        {
            // if boss is in jumper behavior, change it back to chase behavior, otherwise, 
            // just take damage to change its behavior phase
//            Debug.Log($"Damage to Boss. Current Health: {bossBehavior.CurrentHealth}");
            bossBehavior.TakeDamage((bossBehavior.CurrentHealth == 0) ? -3 : 1);
        }
    }

    private void PlaySFXDetached(AudioClip clip)
    {
        GameObject sfxObj = new GameObject("DamageSFX_Temp");
        sfxObj.transform.position = transform.position;
        var src = sfxObj.AddComponent<AudioSource>();
        src.clip = clip;
        src.Play();
        Destroy(sfxObj, clip.length);
    }
}
