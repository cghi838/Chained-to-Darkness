using UnityEngine;

public class Bullet : MonoBehaviour
{
    //[Header("Player Hit Setting")]
    //[SerializeField] private LayerMask playerMask;
    //[SerializeField] private float damageToPlayer = 10f;

    //[Header("Effects")]
    //public GameObject damageVFX;
    //public AudioClip damageSFX;

    private Rigidbody2D rb;
    private Collider2D col;
    private Transform anchorPos;
    private float anchorRadius;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }


    void Update()
    {
        float distance = Vector2.Distance(transform.position, anchorPos.position);
        if (distance > anchorRadius) Destroy(gameObject);
    }

    public void SetAnchorPosAndRadius(Transform anchorPos, float anchorRadius)
    {
        this.anchorPos = anchorPos;
        this.anchorRadius = anchorRadius;
    }

    // private void OnTriggerEnter2D(Collider2D other)
    // {
    //     if ((playerMask.value & (1 << other.gameObject.layer)) == 0) return;

    //     PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
    //     SanitySystem sanitySystem = other.GetComponent<SanitySystem>();
    //     // HP
    //     if (playerHealth != null)
    //     {
    //         playerHealth.TakeDamage(damageToPlayer);
    //     }
    //     // Sanity
    //     if (sanitySystem != null)
    //     {
    //         sanitySystem.DecreaseSanity(damageToPlayer); // Example sanity damage
    //     }

    //     // VFX
    //     if (damageVFX != null)
    //         Instantiate(damageVFX, transform.position, Quaternion.identity);

    //     // SFX
    //     float destroyDelay = 0f;
    //     if (damageSFX != null)
    //     {
    //         destroyDelay = damageSFX.length;
    //         PlaySFXDetached(damageSFX);
    //     }

    //     // Destroy bullet
    //     Destroy(gameObject); // Hit player and bullet is gone
    // }

    // private void PlaySFXDetached(AudioClip clip)
    // {
    //     GameObject sfxObj = new GameObject("BulletSFX_Temp");
    //     sfxObj.transform.position = transform.position;
    //     var src = sfxObj.AddComponent<AudioSource>();
    //     src.clip = clip;
    //     src.Play();
    //     Destroy(sfxObj, clip.length);
    // }
}
