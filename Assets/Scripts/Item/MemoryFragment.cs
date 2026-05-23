using UnityEngine;

[CreateAssetMenu(fileName = "MemoryFragment", menuName = "Game/MemoryFragment")]
// MemoryFragmentData — One-time pickup item. Consumed on contact.
public class MemoryFragment : ScriptableObject
{
    public string fragmentName;
    public Sprite icon;

    [Header("Pickup Effects")]
    public float healthHealOrDamage = 0f;   // instant HP change on pickup (negative = damage)
    public float sanityHealOrDamage = 0f;   // instant Sanity change on pickup (negative = drain)
}
