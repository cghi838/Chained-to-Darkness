using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// ============================================================
//  PauseMenuManager.cs
//  Handles in-game pause menu: resume, settings, and quit.
//  Attach to a Canvas or child GameObject in the HUD scene.
//  The pause panel should be a child of the HUD Canvas and
//  initially disabled in the Inspector.
// ============================================================
public class PauseMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject pauseSettingsPanel;

    [Header("Pause Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseSettingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Settings Inside Pause")]
    [SerializeField] private Slider pauseMasterSlider;
    [SerializeField] private Slider pauseMusicSlider;
    [SerializeField] private Slider pauseSFXSlider;
    [SerializeField] private Button pauseSettingsBackButton;
    [SerializeField] private Button pauseSettingsApplyButton;

    [Header("Overlay Dimmer")]
    [SerializeField] private Image backgroundDimmer;
    [SerializeField] private float dimmerTargetAlpha = 0.6f;
    [SerializeField] private float dimmerFadeSpeed = 6f;

    [Header("Confirm Return to Menu")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    // -------------------------------------------------------
    private bool isPaused = false;

    // -------------------------------------------------------
    private void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);

        SetDimmerAlpha(0f);

        BindButtons();
    }

    // -------------------------------------------------------
    //  Public API called from HUDManager or input handler
    // -------------------------------------------------------
    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeDimmer(0f, dimmerTargetAlpha));
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        StopAllCoroutines();
        StartCoroutine(FadeDimmer(dimmerTargetAlpha, 0f));
    }

    // -------------------------------------------------------
    //  Button Binding
    // -------------------------------------------------------
    private void BindButtons()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (pauseSettingsButton != null) pauseSettingsButton.onClick.AddListener(OpenPauseSettings);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuPressed);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitPressed);

        if (pauseSettingsBackButton != null) pauseSettingsBackButton.onClick.AddListener(ClosePauseSettings);
        if (pauseSettingsApplyButton != null) pauseSettingsApplyButton.onClick.AddListener(ApplyPauseSettings);

        if (confirmYesButton != null) confirmYesButton.onClick.AddListener(ReturnToMainMenu);
        if (confirmNoButton != null) confirmNoButton.onClick.AddListener(CloseConfirmPanel);
    }

    // -------------------------------------------------------
    //  Settings inside Pause
    // -------------------------------------------------------
    private void OpenPauseSettings()
    {
        LoadSettingsToSliders();
        if (pauseSettingsPanel != null) pauseSettingsPanel.SetActive(true);
    }

    private void ClosePauseSettings()
    {
        if (pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);
    }

    private void ApplyPauseSettings()
    {
        float master = (pauseMasterSlider != null) ? pauseMasterSlider.value : 1f;
        float music = (pauseMusicSlider != null) ? pauseMusicSlider.value : 0.8f;
        float sfx = (pauseSFXSlider != null) ? pauseSFXSlider.value : 1f;

        PlayerPrefs.SetFloat("MasterVolume", master);
        PlayerPrefs.SetFloat("MusicVolume", music);
        PlayerPrefs.SetFloat("SFXVolume", sfx);
        PlayerPrefs.Save();

        AudioListener.volume = master;
    }

    private void LoadSettingsToSliders()
    {
        if (pauseMasterSlider != null) pauseMasterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        if (pauseMusicSlider != null) pauseMusicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        if (pauseSFXSlider != null) pauseSFXSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    // -------------------------------------------------------
    //  Main Menu / Quit
    // -------------------------------------------------------
    private void OnMainMenuPressed()
    {
        if (confirmPanel != null) confirmPanel.SetActive(true);
    }

    private void CloseConfirmPanel()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnQuitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // -------------------------------------------------------
    //  Dimmer Fade
    // -------------------------------------------------------
    private void SetDimmerAlpha(float alpha)
    {
        if (backgroundDimmer == null) return;
        Color c = backgroundDimmer.color;
        c.a = alpha;
        backgroundDimmer.color = c;
        backgroundDimmer.gameObject.SetActive(alpha > 0.001f);
    }

    private IEnumerator FadeDimmer(float from, float to)
    {
        if (backgroundDimmer == null) yield break;
        backgroundDimmer.gameObject.SetActive(true);
        float elapsed = 0f;
        float duration = Mathf.Abs(to - from) / dimmerFadeSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetDimmerAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetDimmerAlpha(to);
        if (to <= 0.001f)
            backgroundDimmer.gameObject.SetActive(false);
    }
}