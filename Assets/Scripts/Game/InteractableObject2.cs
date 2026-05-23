using UnityEngine;
using UnityEngine.SceneManagement;

public class InteractableObject2 : MonoBehaviour
{
    public enum InteractableType
    {
        Switch,
        LevelTransition,
        Key,
        LockedObject,
        DialogueTrigger,
        EnvironmentalTrigger
    }

    [Header("General Settings")]
    [SerializeField] private string objectId;
    [SerializeField] private InteractableType interactableType;
    [SerializeField] private bool interactOnce = false;
    [SerializeField] private bool startDisabledIfAlreadyUsed = true;

    [Header("Switch Settings")]
    [SerializeField] private GameObject switchTarget;
    [SerializeField] private bool toggleTargetActive = true;

    [Header("Level Transition Settings")]
    [SerializeField] private string sceneToLoad;

    [Header("Key / Lock Settings")]
    [SerializeField] private string keyId;
    [SerializeField] private bool destroyOnUnlock = true;

    [Header("Dialogue Settings")]
    [TextArea]
    [SerializeField] private string dialogueText;

    [Header("Environmental Trigger Settings")]
    [SerializeField] private GameObject environmentalTarget;
    [SerializeField] private bool enableEnvironmentalTarget = true;

    private bool hasBeenUsed;

    private void Start()
    {
        if (ObjectStateManager.Instance != null && !string.IsNullOrEmpty(objectId))
        {
            hasBeenUsed = ObjectStateManager.Instance.GetObjectState(objectId);

            if (hasBeenUsed && startDisabledIfAlreadyUsed)
                gameObject.SetActive(false);
        }
    }

    public void Interact()
    {
        if (interactOnce && hasBeenUsed)
            return;

        switch (interactableType)
        {
            case InteractableType.Switch:
                UseSwitch();
                break;

            case InteractableType.LevelTransition:
                UseLevelTransition();
                break;

            case InteractableType.Key:
                PickUpKey();
                break;

            case InteractableType.LockedObject:
                TryUnlockObject();
                break;

            case InteractableType.DialogueTrigger:
                TriggerDialogue();
                break;

            case InteractableType.EnvironmentalTrigger:
                TriggerEnvironment();
                break;
        }

        if (interactOnce)
        {
            hasBeenUsed = true;

            if (ObjectStateManager.Instance != null && !string.IsNullOrEmpty(objectId))
                ObjectStateManager.Instance.SetObjectState(objectId, true);
        }
    }

    private void UseSwitch()
    {
        if (switchTarget == null)
            return;

        if (toggleTargetActive)
            switchTarget.SetActive(!switchTarget.activeSelf);
        else
            switchTarget.SetActive(true);
    }

    private void UseLevelTransition()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
            return;

        SceneManager.LoadScene(sceneToLoad);
    }

    private void PickUpKey()
    {
        if (string.IsNullOrEmpty(keyId))
            return;

        if (ObjectStateManager.Instance != null)
            ObjectStateManager.Instance.AddKey(keyId);

        gameObject.SetActive(false);
    }

    private void TryUnlockObject()
    {
        if (string.IsNullOrEmpty(keyId))
            return;

        if (ObjectStateManager.Instance == null)
            return;

        if (ObjectStateManager.Instance.HasKey(keyId))
        {
            if (destroyOnUnlock)
                gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("Locked. Missing key: " + keyId);
        }
    }

    private void TriggerDialogue()
    {
        Debug.Log("Dialogue: " + dialogueText);
    }

    private void TriggerEnvironment()
    {
        if (environmentalTarget == null)
            return;

        environmentalTarget.SetActive(enableEnvironmentalTarget);
    }
}