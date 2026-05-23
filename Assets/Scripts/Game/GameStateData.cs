using System;
using UnityEngine;

// Runtime state data. Serializable for JSON save/load.
// Created from GameState SO template at runtime.
[Serializable]
public class GameStateData
{
    public float currentHealth;
    public float maxHealth;
    public float currentSanity;
    public float maxSanity;
    public float maxTime;
    public float timeRemaining;
    public int memoryFragments;

    // Story Flags
    public bool talkedToDoctor;
    public bool openedBasementDoor;

    public GameStateData(GameState template)
    {
        currentHealth = template.maxHealth;
        maxHealth = template.maxHealth;
        currentSanity = template.maxSanity;
        maxSanity = template.maxSanity;
        maxTime = template.maxTime;
        timeRemaining = template.maxTime;
        memoryFragments = 0;
        talkedToDoctor = false;
        openedBasementDoor = false;
    }

    public void ClampValues()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        currentSanity = Mathf.Clamp(currentSanity, 0f, maxSanity);
        timeRemaining = Mathf.Clamp(timeRemaining, 0f, maxTime);
    }
}