using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Global Game State (SO)")]
    [SerializeField] private GameState stateTemplate;

    public GameStateData State { get; private set; } // Runtime data — serializable, JSON save/load ready

    [Header("Checkpoint")]
    public bool hasCheckpoint = false;
    public Vector3 checkpointPosition;

    // Static events
    public static event Action<float> OnTimeNormalized;
    public static event Action<float, float> OnHealthChanged;
    public static event Action<float, float> OnSanityChanged;

    private LevelData levelData;

    private bool isPlaying = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (stateTemplate == null)
        {
            Debug.LogError("GameManager: GameState template not assigned!");
            return;
        }

        // Create runtime copy from SO — never modify the original SO
        State = stateTemplate.CreateRuntimeData();
    }

    void Start()
    {
        isPlaying = false; // Start paused until player hits jump
        PushStateToUI();
    }

    void Update()
    {
        if (isPlaying) UpdateTimer();
        if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Debug.Log("Escape key pressed");
                EndGame();
            }
            //            else if (Keyboard.current.pKey.wasPressedThisFrame) PauseTimer();
        }
    }

    private void UpdateTimer()
    {
        if (isPlaying)
        {
            State.timeRemaining = Mathf.Max(State.timeRemaining - Time.deltaTime, 0f);
            OnTimeNormalized?.Invoke(State.timeRemaining / State.maxTime);
            if (State.timeRemaining <= 0f) EndGame();
        }
    }

    public void PauseTimer()
    {
        //Debug.Log("PauseTimer");
        isPlaying = false;
        Time.timeScale = 0f;
    }

    public void ResumeTimer()
    {
        isPlaying = true;
        Time.timeScale = 1f;
    }

    public void EndGame()
    {
        isPlaying = false;
        //Debug.Log("EndGame");
        SceneManager.LoadScene("EndScene");
    }

    public void Resume() => Time.timeScale = 1f;

    private void PushStateToUI()
    {
        if (State == null) return;
        State.ClampValues();
        OnHealthChanged?.Invoke(State.currentHealth, State.maxHealth);
        OnSanityChanged?.Invoke(State.currentSanity, State.maxSanity);
    }

    public void NewGame()
    {
        //        LevelData ld = levelData;        
        State = stateTemplate.CreateRuntimeData(); // resets all values including timer
        ClearCheckpoint();
        isPlaying = true;
        PushStateToUI();
        // if (ld != null)
        // {
        //     levelData = ld;
        //     Debug.Log($"GameManager: NewGame with LevelData - Default Spawn: {ld.defaultSpawnPosition}, Flip Sprite: {ld.flipSprite}");
        // }
    }

    public void SetCheckpoint(Vector3 pos)
    {
        checkpointPosition = pos;
        hasCheckpoint = true;
    }

    public void ClearCheckpoint()
    {
        hasCheckpoint = false;
        checkpointPosition = Vector3.zero;
    }

    // public void TakeDamage(float amount)
    // {
    //     if (State == null) return;
    //     State.currentHealth -= amount;
    //     State.ClampValues();
    //     OnHealthChanged?.Invoke(State.currentHealth, State.maxHealth);
    //     if (State.currentHealth <= 0f) EndGame();
    // }

    // public void Heal(float amount)
    // {
    //     if (State == null) return;
    //     State.currentHealth += amount;
    //     State.ClampValues();
    //     OnHealthChanged?.Invoke(State.currentHealth, State.maxHealth);
    // }

    // public void ChangeSanity(float amount)
    // {
    //     if (State == null) return;
    //     State.currentSanity += amount;
    //     State.ClampValues();
    //     OnSanityChanged?.Invoke(State.currentSanity, State.maxSanity);
    // }

    public void AddMemoryFragment(int amount = 1)
    {
        if (State == null) return;
        State.memoryFragments += amount;
        UIManager.Instance?.ShowPopup($"+{amount} Memory");
    }

    public bool IsPlaying() => isPlaying;

    public void SetLevelData(LevelData data)
    {
        if (data != null)
        {
            Debug.Log($"GameManager: Received LevelData for {SceneManager.GetActiveScene().name} - Default Spawn: {data.defaultSpawnPosition}, Flip Sprite: {data.flipSprite}");
        }
        levelData = data;
    }

    public LevelData GetLevelData() => levelData;
}