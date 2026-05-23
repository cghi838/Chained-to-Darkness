// CutsceneTrigger.cs   attach to TriggerZone
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System;

public class CutsceneTrigger : MonoBehaviour
{
    [Header("Drag the VideoPlayer object here")]
    public VideoPlayer videoPlayer;

    [Header("Drag the CutsceneCanvas GameObject here")]
    public GameObject cutsceneCanvas;

    [Header("Drag the player's movement script here")]
    public MonoBehaviour playerController;

    [Header("Drag the player's health script here")]
    public PlayerHealth playerHealth;

    [Header("Drag the CutsceneRoom object here")]
    public GameObject cutsceneRoom;

    [Header("Exact name of the next scene (must be in Build Settings)")]
    public string nextSceneName;

    [Header("End Event")]
    public Action endEvent;

    [Header("Enter Trigger Event")]
    public Action<Collider2D> onEnterTriggerEvent;

    private bool hasPlayed = false;

    private void OnEnable()
    {
        videoPlayer.Stop();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasPlayed)
        {
            onEnterTriggerEvent?.Invoke(other);
            hasPlayed = true;
            StartCutscene();
        }
    }

    void StartCutscene()
    {
        GameObject player = playerController.gameObject;

        // Pause the timer so it doesn't run out during the cutscene
        if (GameManager.Instance != null)
            GameManager.Instance.PauseTimer();

        // Make player invincible during cutscene
        if (playerHealth != null)
        {
            playerHealth.StopAllCoroutines();
            playerHealth.isInvincible = true;
        }

        // Mute all background audio
        AudioListener.pause = true;

        // Teleport player to safe room
        player.transform.position = cutsceneRoom.transform.position;

        // Show canvas
        cutsceneCanvas.SetActive(true);

        // Lock input
        playerController.enabled = false;

        // Kill momentum
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.isKinematic = true; }

        // Freeze animation
        Animator anim = player.GetComponent<Animator>();
        if (anim != null) anim.enabled = false;

        // Subscribe then play
        videoPlayer.loopPointReached += OnVideoEnd;
        videoPlayer.Play();
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        videoPlayer.loopPointReached -= OnVideoEnd;

        // Restore audio
        AudioListener.pause = false;

        // Reset game state including timer and health for next scene
        if (GameManager.Instance != null)
            GameManager.Instance.NewGame();

        // Delegate level ending to LevelExitTrigger
        Debug.Log("Video end. call cutscene end event");
        endEvent?.Invoke();

        // Load next scene directly if endEvent is not wired up
        if (endEvent == null)
        {
            Debug.Log("No end event assigned. Loading next scene directly: " + nextSceneName);
            SceneManager.LoadScene(nextSceneName);
        }
    }
}