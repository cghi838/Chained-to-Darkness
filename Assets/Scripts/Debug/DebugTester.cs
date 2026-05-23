using UnityEngine;
using UnityEngine.InputSystem;

public class DebugTester : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private SanitySystem sanitySystem;
    [SerializeField] private GameStateDataSerializer gameStateDataSerializer;

    private float currentSanity;

    private void Awake()
    {
        if (sanitySystem == null)
            sanitySystem = GetComponent<SanitySystem>();

        if (sanitySystem == null)
            sanitySystem = FindFirstObjectByType<SanitySystem>();
    }

    private void OnEnable()
    {
        if (sanitySystem != null)
            sanitySystem.OnSanityChanged += OnSanityChanged;
    }

    private void OnDisable()
    {
        if (sanitySystem != null)
            sanitySystem.OnSanityChanged -= OnSanityChanged;
    }

    private void OnSanityChanged(float current, float max)
    {
        currentSanity = current;
        //        Debug.Log($"[SanityDebug] Current = {current}");
    }

    void Update()
    {
        if (playerHealth == null || sanitySystem == null || Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            playerHealth?.TakeDamage(1);       // 1-key — TakeDamage

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            playerHealth?.Heal(1);             // 2-key — Heal

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            sanitySystem?.SetTraumaItemEquipped(false); // 3-key — Item unlock (reduces mental power)

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
            sanitySystem?.SetTraumaItemEquipped(true);  // 4-key — Equip items (restores sanity)

        if (Keyboard.current.digit5Key.wasPressedThisFrame)
            MoveToBoundary(40);       // 5-key — Unstable Sanity

        if (Keyboard.current.digit6Key.wasPressedThisFrame)
            MoveToBoundary(1);             // 6-key — Critical Sanity

        if (Keyboard.current.digit7Key.wasPressedThisFrame)
            MoveToBoundary(0); // 7-key — Broken Sanity

        if (Keyboard.current.digit8Key.wasPressedThisFrame)
            MoveToBoundary(100);  // 8-key — Stable Sanity


        if (Keyboard.current.minusKey.wasPressedThisFrame)
            sanitySystem?.DecreaseSanity(10f);

        if (Keyboard.current.equalsKey.wasPressedThisFrame)
            sanitySystem?.IncreaseSanity(10f);

        if (Keyboard.current.xKey.wasPressedThisFrame)
            gameStateDataSerializer?.Save();    // x-key - save gameState Data

        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            // happy 아이템 효과 호출
        }

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            // painful 효과 호출
        }

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            // childhood 효과 호출
        }

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            // enemy hit 처리 호출
        }

        // H key — set HP to 0 to test death/respawn
        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            Debug.Log("[DebugTester] Force kill — testing checkpoint respawn");
            playerHealth?.TakeDamage(9999f);
        }

        // R key — force respawn at current checkpoint
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (CheckpointController.Current != null)
            {
                Debug.Log($"[DebugTester] Force respawn at {CheckpointController.Current.transform.position}");
                CheckpointController.Current.Respawn(playerHealth?.gameObject);
            }
            else
            {
                Debug.Log("[DebugTester] No checkpoint activated yet");
            }
        }
    }

    private void MoveToBoundary(float targetSanity)
    {
        float delta = targetSanity - currentSanity;

        if (Mathf.Approximately(delta, 0f))
            return;

        if (delta > 0f)
            sanitySystem.IncreaseSanity(delta);
        else
            sanitySystem.DecreaseSanity(-delta);

        Debug.Log($"Moved sanity from {currentSanity} to target {targetSanity}");
    }
}