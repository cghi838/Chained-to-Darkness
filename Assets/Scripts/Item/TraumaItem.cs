using UnityEngine;

[CreateAssetMenu(menuName = "Game/TraumaItem", fileName = "TraumaItem")]

public class TraumaItem : ScriptableObject
{
    [Header("Default Effects When Not Equipped")]
    public float defaultSpeedModifier = 0.7f;  // movement speed multiplier when not equipped
    public float defaultSanityFillUpModifier = 0.1f; // sanity fill-up multiplier when not equipped

    [Header("Info")]
    public string itemName;
    public Sprite icon;

    [Header("Equip Effects")]
    public float speedModifier = 0.6f;  // movement speed multiplier when equipped
    public float sanityDrainModifier = 0.2f;    // sanity drain multiplier when equipped (0 = no drain)
    public bool providesLight = true;  // whether the item emits light
}