using System;
using System.Collections;
using UnityEngine;

/// PlayerHealth
///
/// Responsibilities:
/// - Health tracking
/// - Damage
/// - Healing
/// - Invincibility frames (i-frames)
/// - Death handling
/// - Respawn via LevelManager
/// - Checkpoint compatibility via GameManager
///
/// Integrates with:
/// - GameManager (checkpoint state)
/// - LevelManager (respawn logic)
/// - UIManager (game over screen)
///
/// Safe to call from:
/// - hazards
/// - enemies
/// - traps
/// - fall zones
///
/// Designed to be the single health/life authority.
public class PlayerHealth : MonoBehaviour
{
    public event Action<float, float> OnHealthChanged;

    private GameStateData state => GameManager.Instance?.State;

    [Header("Invincibility Frames")]
    [Tooltip("Seconds of invulnerability after taking damage")]
    public float invincibilityDuration = 1f;

    [Header("Knockback")]
    [Tooltip("Optional controller for applying knockback")]
    public PlayerPlatformerController controller;

    [Header("Debug")]
    public bool isInvincible;
    public bool isDead;

    private Coroutine invincibilityRoutine;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<PlayerPlatformerController>();
    }

    private void Start()
    {
        if (state != null)
            OnHealthChanged?.Invoke(state.currentHealth, state.maxHealth);
    }

    /// Standard damage entry point.
    /// Supports optional knockback.

    public void TakeDamage(float amount, Vector2 knockback = default)
    {
        if (state == null)
            return;

        if (isDead)
            return;

        if (isInvincible)
            return;

        if (state.currentHealth <= 0)
            return;

        state.currentHealth -= Mathf.Max(0f, amount);
        state.ClampValues();

        OnHealthChanged?.Invoke(
            state.currentHealth,
            state.maxHealth
        );

        Debug.Log("Hit! HP = " + state.currentHealth);

        // Apply knockback if available
        if (controller != null && knockback != Vector2.zero)
            controller.ApplyKnockback(knockback);

        if (state.currentHealth <= 0)
        {
            Die();
            return;
        }

        StartInvincibility();
    }

    /// Instantly kills the player.
    /// Used for pits, death zones, etc.
    public void Kill()
    {
        if (isDead)
            return;

        if (state == null)
            return;

        state.currentHealth = 0;

        OnHealthChanged?.Invoke(
            state.currentHealth,
            state.maxHealth
        );

        Die();
    }

    /// Restores health.
    public void Heal(float amount)
    {
        if (state == null)
            return;

        if (isDead)
            return;

        state.currentHealth += Mathf.Max(0f, amount);
        state.ClampValues();

        OnHealthChanged?.Invoke(
            state.currentHealth,
            state.maxHealth
        );

        Debug.Log("Heal! HP = " + state.currentHealth);
    }

    /// <summary>
    /// Resets health to max.
    /// Used on level restart or checkpoint restore.
    /// </summary>
    public void ResetHealth()
    {
        if (state == null)
            return;

        state.currentHealth = state.maxHealth;

        OnHealthChanged?.Invoke(
            state.currentHealth,
            state.maxHealth
        );
    }

    /// <summary>
    /// Handles death logic.
    /// Uses existing GameManager / LevelManager flow.
    /// </summary>
    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log("Player died");

        if (GameManager.Instance != null &&
            GameManager.Instance.hasCheckpoint)
        {
            // Restore HP
            state.currentHealth = state.maxHealth;

            OnHealthChanged?.Invoke(
                state.currentHealth,
                state.maxHealth
            );

            if (LevelManager.Instance != null)
                LevelManager.Instance.RespawnPlayer();
        }
        else
        {
            //gameObject.SetActive(false);
            GameManager.Instance.EndGame();
            // if (UIManager.Instance != null)
            //     UIManager.Instance.ShowGameOver();
        }

        isDead = false;
    }

    /// <summary>
    /// Starts invincibility frames.
    /// Prevents rapid repeated damage.
    /// </summary>
    private void StartInvincibility()
    {
        if (invincibilityRoutine != null)
            StopCoroutine(invincibilityRoutine);

        invincibilityRoutine =
            StartCoroutine(InvincibilityRoutine());
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        float timer = 0f;

        while (timer < invincibilityDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        isInvincible = false;
    }

    /// Sets the player's checkpoint location.
    /// Called by checkpoint trigger volumes.
    public void SetCheckpoint(Vector3 position)
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.hasCheckpoint = true;

        if (LevelManager.Instance != null)
            LevelManager.Instance.SetCheckpoint(position);

        Debug.Log("Checkpoint set at: " + position);
    }
}