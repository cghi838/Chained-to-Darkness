using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  Manages all in-game overlay UI: sanity bar, health,
//  objective text, pick-up prompts, and horror effects.
//  Attach to a Canvas GameObject named "HUD_Canvas".
// ============================================================
public class HUDManager : MonoBehaviour
{
    // Singleton for easy access from other scripts
    public static HUDManager Instance { get; private set; }

    [Header("Health")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Image healthFill;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Color healthColorFull = new Color(0.8f, 0.15f, 0.15f);
    [SerializeField] private Color healthColorCritical = new Color(0.9f, 0.05f, 0.05f);

    [Header("Sanity")]
    [SerializeField] private Slider sanityBar;
    [SerializeField] private Image sanityFill;
    [SerializeField] private TextMeshProUGUI sanityText;
    [SerializeField] private Color sanityColorHigh = new Color(0.3f, 0.6f, 0.9f);
    [SerializeField] private Color sanityColorLow = new Color(0.6f, 0.1f, 0.7f);

    [Header("Objective")]
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private CanvasGroup objectiveGroup;
    [SerializeField] private float objectiveDisplayTime = 5f;
    [SerializeField] private float objectiveFadeDuration = 1f;

    [Header("Pickup Prompt")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Inventory / Item Collected")]
    [SerializeField] private GameObject itemNotifyPanel;
    [SerializeField] private TextMeshProUGUI itemNotifyText;
    [SerializeField] private Image itemNotifyIcon;
    [SerializeField] private float itemNotifyDuration = 3f;

    [Header("Sanity Vignette")]
    [SerializeField] private Image vignetteImage;
    [SerializeField] private float vignetteMaxAlpha = 0.75f;

    [Header("Sanity Screen Distortion")]
    [SerializeField] private CanvasGroup glitchOverlay;
    [SerializeField] private float glitchThreshold = 0.3f;

    [Header("Pause Reference")]
    [SerializeField] private PauseMenuManager pauseMenu;

    // -------------------------------------------------------
    private float currentHealth = 100f;
    private float maxHealth = 100f;
    private float currentSanity = 100f;
    private float maxSanity = 100f;

    private Coroutine objectiveCoroutine;
    private Coroutine itemNotifyCoroutine;

    // -------------------------------------------------------
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
        HidePrompt();
        HideItemNotify();

        if (objectiveGroup != null)
            objectiveGroup.alpha = 0f;

        UpdateHealthUI(currentHealth, maxHealth);
        UpdateSanityUI(currentSanity, maxSanity);
    }

    private void Update()
    {
        UpdateVignette();
        UpdateGlitch();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseMenu != null)
                pauseMenu.TogglePause();
        }
    }

    // -------------------------------------------------------
    //  Health API
    // -------------------------------------------------------
    public void SetHealth(float current, float max)
    {
        currentHealth = current;
        maxHealth = max;
        UpdateHealthUI(current, max);
    }

    private void UpdateHealthUI(float current, float max)
    {
        if (healthBar != null)
            healthBar.value = (max > 0f) ? current / max : 0f;

        if (healthFill != null)
        {
            float ratio = (max > 0f) ? current / max : 0f;
            healthFill.color = Color.Lerp(healthColorCritical, healthColorFull, ratio);
        }

        if (healthText != null)
            healthText.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(max);
    }

    // -------------------------------------------------------
    //  Sanity API
    // -------------------------------------------------------
    public void SetSanity(float current, float max)
    {
        currentSanity = current;
        maxSanity = max;
        UpdateSanityUI(current, max);
    }

    private void UpdateSanityUI(float current, float max)
    {
        if (sanityBar != null)
            sanityBar.value = (max > 0f) ? current / max : 0f;

        if (sanityFill != null)
        {
            float ratio = (max > 0f) ? current / max : 0f;
            sanityFill.color = Color.Lerp(sanityColorLow, sanityColorHigh, ratio);
        }

        if (sanityText != null)
            sanityText.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(max);
    }

    // -------------------------------------------------------
    //  Vignette (darkens edges as sanity drops)
    // -------------------------------------------------------
    private void UpdateVignette()
    {
        if (vignetteImage == null) return;
        float ratio = (maxSanity > 0f) ? currentSanity / maxSanity : 0f;
        float targetAlpha = Mathf.Lerp(vignetteMaxAlpha, 0f, ratio);
        Color c = vignetteImage.color;
        c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * 2f);
        vignetteImage.color = c;
    }

    // -------------------------------------------------------
    //  Glitch overlay (activates at low sanity)
    // -------------------------------------------------------
    private void UpdateGlitch()
    {
        if (glitchOverlay == null) return;
        float ratio = (maxSanity > 0f) ? currentSanity / maxSanity : 0f;
        float targetAlpha = (ratio < glitchThreshold) ?
            Mathf.Lerp(0.6f, 0f, ratio / glitchThreshold) : 0f;
        glitchOverlay.alpha = Mathf.Lerp(glitchOverlay.alpha, targetAlpha, Time.deltaTime * 3f);
    }

    // -------------------------------------------------------
    //  Objective Text
    // -------------------------------------------------------
    public void ShowObjective(string message)
    {
        if (objectiveCoroutine != null)
            StopCoroutine(objectiveCoroutine);
        objectiveCoroutine = StartCoroutine(ShowObjectiveRoutine(message));
    }

    private IEnumerator ShowObjectiveRoutine(string message)
    {
        if (objectiveText != null)
            objectiveText.text = message;

        // Fade in
        yield return StartCoroutine(FadeCanvasGroup(objectiveGroup, 0f, 1f, objectiveFadeDuration));

        yield return new WaitForSeconds(objectiveDisplayTime);

        // Fade out
        yield return StartCoroutine(FadeCanvasGroup(objectiveGroup, 1f, 0f, objectiveFadeDuration));
    }

    // -------------------------------------------------------
    //  Interaction Prompt
    // -------------------------------------------------------
    public void ShowPrompt(string message)
    {
        if (promptPanel != null) promptPanel.SetActive(true);
        if (promptText != null) promptText.text = message;
    }

    public void HidePrompt()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
    }

    // -------------------------------------------------------
    //  Item Notification
    // -------------------------------------------------------
    public void ShowItemNotification(string itemName, Sprite icon = null)
    {
        if (itemNotifyCoroutine != null)
            StopCoroutine(itemNotifyCoroutine);
        itemNotifyCoroutine = StartCoroutine(ItemNotifyRoutine(itemName, icon));
    }

    private IEnumerator ItemNotifyRoutine(string itemName, Sprite icon)
    {
        if (itemNotifyText != null) itemNotifyText.text = "Picked up: " + itemName;
        if (itemNotifyIcon != null)
        {
            itemNotifyIcon.sprite = icon;
            itemNotifyIcon.enabled = (icon != null);
        }

        if (itemNotifyPanel != null) itemNotifyPanel.SetActive(true);
        yield return new WaitForSeconds(itemNotifyDuration);
        if (itemNotifyPanel != null) itemNotifyPanel.SetActive(false);
    }

    private void HideItemNotify()
    {
        if (itemNotifyPanel != null) itemNotifyPanel.SetActive(false);
    }

    // -------------------------------------------------------
    //  Utility
    // -------------------------------------------------------
    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;
        float elapsed = 0f;
        group.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;
    }
}