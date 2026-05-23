using UnityEngine;

[CreateAssetMenu(menuName = "Game/LevelData", fileName = "LevelData")]

public class LevelData : ScriptableObject
{
    [Header("Player")]
    public Vector3 defaultSpawnPosition; // Default spawn position for the player
    public bool flipSprite = false;

    [Header("Trauma Item Requirements")]
    public int requiredHappyItems; // Number of happy items needed to remove the trauma in a specific level
    public int requiredPainfulItems; // Number of painful items needed to equip the trauma in a specific level
}