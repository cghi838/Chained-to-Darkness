using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;
    //    [SerializeField] private Transform defaultSpawnPoint;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private CameraFollow cameraFollow;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 0.25f;

    [Header("Level Data")]
    [SerializeField] private LevelData levelData;

    [Header("Title Card")]
    [SerializeField] private GameObject titleCard;

    private GameObject playerInstance;
    private int levelIndex = 1;
    private bool gameStarted = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        GameManager.Instance.PauseTimer(); // Ensure game is unpaused when level starts        
        Debug.Log("LevelManager: Start — getlevelData if exists.");
        LevelData ld = GameManager.Instance.GetLevelData(); // Get level data from GameManager 
        if (ld != null)
        {
            levelData = ld; // Use level data from GameManager if available
                            //            Debug.Log($"LevelManager: Received LevelData for {SceneManager.GetActiveScene().name} - Default Spawn: {ld.defaultSpawnPosition}, Flip Sprite: {ld.flipSprite}");
        }
        // FindOrSpawnPlayer();
        // MovePlayerToSpawn();
    }

    private void Update()
    {
        if (!GameManager.Instance.IsPlaying())
        {
            if (InputSystem.actions["Jump"].WasPressedThisFrame())
            {
                titleCard.SetActive(false);
                RespawnPlayer();
                GameManager.Instance.ResumeTimer();
            }
        }
    }

    // ---------------------------
    // Player Spawn / Respawn
    // ---------------------------

    private void FindOrSpawnPlayer()
    {
        var existing = GameObject.FindGameObjectWithTag(playerTag);

        if (existing != null)
        {
            playerInstance = existing;
            FlipPlayerSprite(levelData != null && levelData.flipSprite);
            return;
        }

        if (playerPrefab != null)
        {
            playerInstance = Instantiate(playerPrefab);
            playerInstance.SetActive(true);
            playerInstance.tag = playerTag;
            FlipPlayerSprite(levelData != null && levelData.flipSprite);
        }
        else
        {
            Debug.LogWarning("LevelManager: No player found and no playerPrefab set.");
        }
    }

    private void MovePlayerToSpawn()
    {
        if (playerInstance == null) return;

        Vector3 spawnPos = GetSpawnPosition();
        Debug.Log($"LevelManager: Moving player to spawn position {spawnPos}");
        playerInstance.transform.position = spawnPos;
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(playerInstance.transform);
        }
    }

    private Vector3 GetSpawnPosition()
    {
        // Use checkpoint from GameManager if available
        if (GameManager.Instance != null && GameManager.Instance.hasCheckpoint)
        {
            return GameManager.Instance.checkpointPosition;
        }

        // Otherwise use scene spawn point
        if (levelData.defaultSpawnPosition != null)
        {
            return levelData.defaultSpawnPosition;
        }

        return Vector3.zero;
    }

    private void FlipPlayerSprite(bool flip)
    {
        if (playerInstance == null) return;

        SpriteRenderer spriteRenderer = playerInstance.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = flip;
        }
    }

    /// <summary>
    /// Stores the active checkpoint position in GameManager.
    /// Respawn uses this through GetSpawnPosition().
    /// </summary>
    public void SetCheckpoint(Vector3 position)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("LevelManager: Cannot set checkpoint because GameManager.Instance is null.");
            return;
        }

        GameManager.Instance.hasCheckpoint = true;
        GameManager.Instance.checkpointPosition = position;
    }

    public void RespawnPlayer()
    {
        Debug.Log("Respawning player...");
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        FindOrSpawnPlayer();
        MovePlayerToSpawn();
        Debug.Log("Player respawned at " + GetSpawnPosition());
    }

    // ---------------------------
    // Scene Controls
    // ---------------------------

    public void ReloadLevel()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Resume();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadNextLevel()
    {
        // Stop timer to prevent it from running during scene load
        // and space bar is pressed to start the next level
        if (GameManager.Instance != null)
            GameManager.Instance.PauseTimer();

        int current = SceneManager.GetActiveScene().buildIndex;
        int next = current + 1;

        if (next >= SceneManager.sceneCountInBuildSettings)
            next = 0;

        SceneManager.LoadScene(next);
        GameManager.Instance.NewGame();
    }

    public void LoadLevelByName(string sceneName)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Resume();

        SceneManager.LoadScene(sceneName);
    }

    public void LoadLevelByIndex(int buildIndex)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Resume();

        SceneManager.LoadScene(buildIndex);
    }

    // ---------------------------
    // Level Completion
    // ---------------------------

    public void LevelComplete()
    {
        LoadNextLevel();
    }

    public int GetRequiredHappyItems() => levelData != null ? levelData.requiredHappyItems : 0;

    public int GetRequiredPainfulItems() => levelData != null ? levelData.requiredPainfulItems : 0;
}