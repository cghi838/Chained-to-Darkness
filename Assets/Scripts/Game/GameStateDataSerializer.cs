using System;
using System.IO;
using UnityEngine;

// Windows: C:/Users/[user]/AppData/LocalLow/DefaultCompany/ChainedToDarkness/
// Mac:     ~/Library/Application Support/DefaultCompany/ChainedToDarkness/
public class GameStateDataSerializer : MonoBehaviour
{
    [Header("Settings")]
    public string fileName = "GameStateData.json";
    [Tooltip("If true, always starts fresh from SO defaults — ignores saved JSON.")]
    public bool alwaysFresh = true;   // set false only when save/load is fully implemented

    private string FilePath => Path.Combine(Application.persistentDataPath, fileName);

    private void Start()
    {
        if (alwaysFresh)
        {
            // Delete stale save so SO defaults are always used
            DeleteSave();
            Debug.Log("GameStateDataSerializer: Fresh start — save file ignored.");
            return;
        }

        Load();
    }

    private void OnApplicationQuit()
    {
        if (!alwaysFresh) Save();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && !alwaysFresh) Save();
    }

    public void Save()
    {
        try
        {
            GameStateData state = GameManager.Instance?.State;
            if (state == null)
            {
                Debug.LogWarning("GameStateDataSerializer: No state to save.");
                return;
            }

            // Save a snapshot — exclude timeRemaining (resets every play session)
            var snapshot = new GameStateDataSnapshot
            {
                currentHealth = state.currentHealth,
                maxHealth = state.maxHealth,
                currentSanity = state.currentSanity,
                maxSanity = state.maxSanity,
                maxTime = state.maxTime,
                memoryFragments = state.memoryFragments,
                talkedToDoctor = state.talkedToDoctor,
                openedBasementDoor = state.openedBasementDoor
            };

            string json = JsonUtility.ToJson(snapshot, true);
            File.WriteAllText(FilePath, json);
            Debug.Log($"Game saved to: {FilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"GameStateDataSerializer: Save failed — {e}");
        }
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                Debug.Log("GameStateDataSerializer: No save file found, using default state.");
                return;
            }

            string json = File.ReadAllText(FilePath);
            var snapshot = JsonUtility.FromJson<GameStateDataSnapshot>(json);

            if (snapshot == null)
            {
                Debug.LogWarning("GameStateDataSerializer: Failed to parse save file.");
                return;
            }

            GameStateData state = GameManager.Instance?.State;
            if (state == null) return;

            state.currentHealth = snapshot.currentHealth;
            state.maxHealth = snapshot.maxHealth;
            state.currentSanity = snapshot.currentSanity;
            state.maxSanity = snapshot.maxSanity;
            state.maxTime = snapshot.maxTime;
            state.timeRemaining = snapshot.maxTime; // always reset timer on load
            state.memoryFragments = snapshot.memoryFragments;
            state.talkedToDoctor = snapshot.talkedToDoctor;
            state.openedBasementDoor = snapshot.openedBasementDoor;

            state.ClampValues();
            Debug.Log($"Game loaded from: {FilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"GameStateDataSerializer: Load failed — {e}");
        }
    }

    public void DeleteSave()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
                Debug.Log("Save file deleted.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"GameStateDataSerializer: Delete failed — {e}");
        }
    }

    public bool SaveExists() => File.Exists(FilePath);
}

// Serializable snapshot — excludes timeRemaining so timer always resets on load.
[Serializable]
public class GameStateDataSnapshot
{
    public float currentHealth;
    public float maxHealth;
    public float currentSanity;
    public float maxSanity;
    public float maxTime;
    public int memoryFragments;
    public bool talkedToDoctor;
    public bool openedBasementDoor;
}