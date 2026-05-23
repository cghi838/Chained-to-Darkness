using UnityEngine;

// Initial value template. Read-only at runtime — never modify directly.
// Use CreateRuntimeData() to generate a GameStateData instance.
[CreateAssetMenu(menuName = "Game/Global State", fileName = "GameState")]
public class GameState : ScriptableObject
{
    [Header("Initial Stats")]
    public float maxHealth = 100f;
    public float maxSanity = 100f;
    public float maxTime = 240f;

    [Header("Initial Story Flags")]
    public bool talkedToDoctor = false;
    public bool openedBasementDoor = false;

    // Creates a new runtime data instance from this template.
    public GameStateData CreateRuntimeData() => new GameStateData(this);
}