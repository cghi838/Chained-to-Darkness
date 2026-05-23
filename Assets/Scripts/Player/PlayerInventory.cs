using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Manages the single trauma item — equip/unequip via e-key.
// Attach to Player alongside SanitySystem and PlayerMovementPlatformer.
public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [Header("Equipped Item")]
    [SerializeField] private TraumaItem equippedItem;
    [SerializeField] private SpriteRenderer itemSprite; // child object's SpriteRenderer

    [Header("Number of Items Collected")]
    public int happyFragmentCount = 0;
    public int painfulFragmentCount = 0;

    public bool HasItemEquipped { get; set; } = true;
    public TraumaItem EquippedItem => HasItemEquipped ? equippedItem : null;

    // Event
    public event Action<bool> OnItemToggled; // true = equipped, false = removed

    // Components
    private SanitySystem sanitySystem;
    private PlayerMovementPlatformer playerMovement; // adjust to your movement script name

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        sanitySystem = GetComponent<SanitySystem>();
        playerMovement = GetComponent<PlayerMovementPlatformer>();
        Debug.Log($"PlayerInventory Start — sanitySystem: {sanitySystem}, playerMovement: {playerMovement}, traumaItem: {equippedItem}");

        // Set initial sprite from Item SO
        // Ensure sprite object is active before setting sprite
        if (itemSprite != null)
        {
            itemSprite.gameObject.SetActive(true);
            if (equippedItem?.icon != null)
                itemSprite.sprite = equippedItem.icon;
        }

        ApplyEffects(HasItemEquipped); // Apply initial state
    }

    /* void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
            ToggleItem();
    } */

    public void ToggleItem()
    {
        HasItemEquipped = !HasItemEquipped;
        ApplyEffects(HasItemEquipped);
        OnItemToggled?.Invoke(HasItemEquipped);
        Debug.Log(HasItemEquipped ? $"Equipped: {equippedItem?.itemName}" : $"{equippedItem?.itemName} Item removed");
    }

    public void EquipItem()
    {
        if (HasItemEquipped) return;
        HasItemEquipped = true;
        ApplyEffects(true);
        OnItemToggled?.Invoke(true);
    }

    public void RemoveItem()
    {
        if (!HasItemEquipped) return;
        HasItemEquipped = false;
        ApplyEffects(false);
        OnItemToggled?.Invoke(false);
    }

    public void OnMemoryFragmentPickedUp(MemoryFragment fragment)
    {
        if (fragment.name == "HappyMemoryFragment") happyFragmentCount++;
        else if (fragment.name == "PainfulMemoryFragment") painfulFragmentCount++;
        //        Debug.Log($"Memory Fragment picked up: {fragment.fragmentName}. Happy count: {happyFragmentCount}, Painful count: {painfulFragmentCount}");
        //        Debug.Log($"Required Happy Items: {LevelManager.Instance.GetRequiredHappyItems()}, Required Painful Items: {LevelManager.Instance.GetRequiredPainfulItems()}");
        if (happyFragmentCount > 0 && happyFragmentCount % LevelManager.Instance.GetRequiredHappyItems() == 0)
        {
            ApplyEffects(false);
        }
        else if (painfulFragmentCount > 0 && painfulFragmentCount % LevelManager.Instance.GetRequiredPainfulItems() == 0)
        {
            ApplyEffects(true);
        }
    }

    private void ApplyEffects(bool equipped)
    {
        Debug.Log($"ApplyEffects — equipped: {equipped}, sanitySystem: {sanitySystem}, playerMovement: {playerMovement}");

        // Sprite visibility
        if (itemSprite != null)
            itemSprite.gameObject.SetActive(equipped);

        // Sanity
        if (sanitySystem != null)
        {
            sanitySystem.SetTraumaItemEquipped(equipped);
            sanitySystem.SetDrainModifier(equipped && equippedItem != null ? -equippedItem.sanityDrainModifier : equippedItem.defaultSanityFillUpModifier);
        }
        else Debug.LogWarning("PlayerInventory: SanitySystem is null.");

        // Speed
        if (playerMovement != null && equippedItem != null)
            playerMovement.speedMultiplier = equipped ? equippedItem.speedModifier : equippedItem.defaultSpeedModifier;
        else Debug.LogWarning($"PlayerInventory: playerMovement={playerMovement}, equippedItem={equippedItem}");
    }
}


