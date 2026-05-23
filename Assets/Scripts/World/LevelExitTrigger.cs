using UnityEngine;

// LevelExitTrigger — Calls LevelManager.LevelComplete() on player contact.
// Works for Level 1, 2, 3 without modification.
// Requires: Collider2D with IsTrigger enabled.
// ExitDoor - LevelExitTrigger
public class LevelExitTrigger : MonoBehaviour
{
    [Header("Detection")]
    public LayerMask playerLayer;

    [Header("Flag")]
    public string clearFlagName = "Level1Clear";  // unique per level, saved to PlayerPrefs

    [Header("Level Data")]
    public LevelData levelData = null;  // if not null, will be passed to next scene

    [Header("Cutscene Trigger")]
    private CutsceneTrigger cutsceneTrigger;

    private bool triggered = false;

    private void Start()
    {
        cutsceneTrigger = GetComponent<CutsceneTrigger>();
        if (cutsceneTrigger != null)
        {
            cutsceneTrigger.endEvent += OnCutsceneEnd;
            cutsceneTrigger.onEnterTriggerEvent += OnTrigger;
        }
        // if (cutsceneTrigger != null)
        // {
        //     Debug.Log("cutsceneTrigger event added");
        //     cutsceneTrigger.endEvent += OnCutsceneEnd;
        // }
        // else
        // {
        //     Debug.Log("cutsceneTrigger is null");
        // }
    }

    private void OnTrigger(Collider2D other)
    {
        if (triggered) return;
        triggered = true;

        // Save clear flag
        PlayerPrefs.SetInt(clearFlagName, 1);
        PlayerPrefs.Save();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (cutsceneTrigger != null) return; // cutsceneTrigger will call OnTrigger via event
        if (triggered) return;
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;
        //        Debug.Log($"[LevelExit] Player entered trigger: {clearFlagName}");

        OnTrigger(other);

        // Debug.Log($"[LevelExit] {clearFlagName} — level complete");
        GameManager.Instance.SetLevelData(levelData);

        // Delegate scene transition to LevelManager
        LevelManager.Instance.LevelComplete();
    }

    private void OnCutsceneEnd()
    {
        Debug.Log("OnCutsceneEnd");
        triggered = true;

        // Save clear flag
        PlayerPrefs.SetInt(clearFlagName, 1);
        PlayerPrefs.Save();

        // Debug.Log($"[LevelExit] {clearFlagName} — level complete");
        GameManager.Instance.SetLevelData(levelData);

        // Delegate scene transition to LevelManager
        LevelManager.Instance.LevelComplete();
    }
}
