using System.Collections.Generic;
using UnityEngine;

public class ObjectStateManager2 : MonoBehaviour
{
    public static ObjectStateManager2 Instance { get; private set; }

    private Dictionary<string, bool> objectStates = new Dictionary<string, bool>();
    private HashSet<string> collectedKeys = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetObjectState(string objectId, bool state)
    {
        if (string.IsNullOrEmpty(objectId))
            return;

        objectStates[objectId] = state;
    }

    public bool GetObjectState(string objectId)
    {
        if (string.IsNullOrEmpty(objectId))
            return false;

        if (objectStates.TryGetValue(objectId, out bool state))
            return state;

        return false;
    }

    public void AddKey(string keyId)
    {
        if (string.IsNullOrEmpty(keyId))
            return;

        collectedKeys.Add(keyId);
    }

    public bool HasKey(string keyId)
    {
        if (string.IsNullOrEmpty(keyId))
            return false;

        return collectedKeys.Contains(keyId);
    }
}