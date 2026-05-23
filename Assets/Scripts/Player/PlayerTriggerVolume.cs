using UnityEngine;

/// Generic trigger volume for hazards, death pits, and checkpoints.
/// 
/// Put this on trigger colliders in the level.
/// 
/// Modes:
/// - Hazard: damages player and optionally applies knockback
/// - KillZone: instantly kills player
/// - Checkpoint: updates respawn position

public class PlayerTriggerVolume : MonoBehaviour
{
    public enum TriggerType
    {
        Hazard,
        KillZone,
        Checkpoint
    }

    [Header("Mode")]
    public TriggerType triggerType = TriggerType.Hazard;

    [Header("Hazard")]
    public int damage = 1;
    public Vector2 knockback = new Vector2(6f, 8f);

    [Tooltip("If true, knockback direction is based on player position relative to the trigger.")]
    public bool autoFlipKnockbackX = true;

    [Tooltip("If true, the hazard can damage repeatedly while the player stays inside.")]
    public bool damageOnStay = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryAffectPlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (damageOnStay)
            TryAffectPlayer(other);
    }

    private void TryAffectPlayer(Collider2D other)
    {
        PlayerHealth life = other.GetComponent<PlayerHealth>();
        if (life == null)
            return;

        switch (triggerType)
        {
            case TriggerType.Hazard:
                {
                    Vector2 finalKnockback = knockback;

                    if (autoFlipKnockbackX)
                    {
                        float direction = Mathf.Sign(other.transform.position.x - transform.position.x);
                        if (Mathf.Abs(direction) < 0.01f)
                            direction = 1f;

                        finalKnockback.x = Mathf.Abs(knockback.x) * direction;
                    }

                    life.TakeDamage(damage, finalKnockback);
                    break;
                }

            case TriggerType.KillZone:
                life.Kill();
                break;

            case TriggerType.Checkpoint:
                life.SetCheckpoint(transform.position);
                break;
        }
    }
}