using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Bars")]
    [SerializeField] private Slider timeSlider;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider sanitySlider;

    [Header("Text")]
    [SerializeField] private TMP_Text popupText;  // optional (requires TextMeshPro)
    //[SerializeField] private TMP_Text healthText; // optional: "HP 75/100"
    //[SerializeField] private TMP_Text sanityText; // optional

    [Header("Fade (Optional)")]
    [SerializeField] private Image fadeOverlay;   // full-screen Image
    [SerializeField] private float fadeSpeed = 2f;

    // Cached player component references for event subscription
    [Header("Player")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private SanitySystem sanitySystem;

    Coroutine popupRoutine;
    Coroutine fadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // If you want the UI to persist between scenes, uncomment:
        // DontDestroyOnLoad(gameObject);

        ShowHUD();
        HidePause();
        HideGameOver();

        if (popupText != null)
            popupText.gameObject.SetActive(false);

        if (fadeOverlay != null)
            SetFadeAlpha(0f);

        if (timeSlider != null)
        {
            timeSlider.minValue = 0f;
            timeSlider.maxValue = 1f;
            timeSlider.value = 1f;
        }
    }

    void Start()
    {
        // Sync initial values from GameManager in case BroadcastState fired before OnEnable
        var state = GameManager.Instance?.State;
        if (state != null)
        {
            SetHealth(state.currentHealth, state.maxHealth);
            SetSanity(state.currentSanity, state.maxSanity);
            SetTime(state.timeRemaining / state.maxTime);
        }
    }

    private void OnEnable()
    {
        // Subscribe to PlayerHealth event
        if (playerHealth != null)
            playerHealth.OnHealthChanged += SetHealth;
        else
            Debug.LogWarning("UIManager: PlayerHealth not found.");

        // Subscribe to SanitySystem event
        if (sanitySystem != null)
            sanitySystem.OnSanityChanged += SetSanity;
        else
            Debug.LogWarning("UIManager: SanitySystem not found.");

        // Subscribe to GameManager static events
        GameManager.OnTimeNormalized += SetTime;
        GameManager.OnHealthChanged += SetHealth;
        GameManager.OnSanityChanged += SetSanity;
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged -= SetHealth;
        if (sanitySystem != null) sanitySystem.OnSanityChanged -= SetSanity;

        GameManager.OnTimeNormalized -= SetTime;
        GameManager.OnHealthChanged -= SetHealth;
        GameManager.OnSanityChanged -= SetSanity;
    }

    // --------------------
    // Panels
    // --------------------
    public void ShowHUD()
    {
        if (hudPanel != null) hudPanel.SetActive(true);
    }

    public void HideHUD()
    {
        if (hudPanel != null) hudPanel.SetActive(false);
    }

    public void ShowPause()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void HidePause()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    public void HideGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    // --------------------
    // Bars / UI Values
    // --------------------
    public void SetTime(float normalized)
    {
        if (timeSlider != null) timeSlider.value = Mathf.Clamp01(normalized);
    }

    public void SetHealth(float current, float max)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }

        //if (healthText != null)
        //    healthText.text = $"HP {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }

    public void SetSanity(float current, float max)
    {
        if (sanitySlider != null)
        {
            sanitySlider.maxValue = max;
            sanitySlider.value = current;
        }

        //if (sanityText != null)
        //    sanityText.text = $"SAN {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }

    // --------------------
    // Popup messages
    // --------------------
    public void ShowPopup(string message, float seconds = 1.25f)
    {
        if (popupText == null) return;

        if (popupRoutine != null) StopCoroutine(popupRoutine);
        popupRoutine = StartCoroutine(PopupRoutine(message, seconds));
    }

    private IEnumerator PopupRoutine(string message, float seconds)
    {
        popupText.text = message;
        popupText.gameObject.SetActive(true);

        yield return new WaitForSeconds(seconds);

        popupText.gameObject.SetActive(false);
    }

    // --------------------
    // Fade
    // --------------------
    public void FadeToBlack()
    {
        if (fadeOverlay == null) return;
        StartFade(1f);
    }

    public void FadeFromBlack()
    {
        if (fadeOverlay == null) return;
        StartFade(0f);
    }

    private void StartFade(float targetAlpha)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float startAlpha = fadeOverlay.color.a;

        while (!Mathf.Approximately(fadeOverlay.color.a, targetAlpha))
        {
            float a = Mathf.MoveTowards(fadeOverlay.color.a, targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
            SetFadeAlpha(a);
            yield return null;
        }

        SetFadeAlpha(targetAlpha);
    }

    private void SetFadeAlpha(float a)
    {
        var c = fadeOverlay.color;
        c.a = a;
        fadeOverlay.color = c;
    }
}