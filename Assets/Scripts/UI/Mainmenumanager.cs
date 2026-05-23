using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// ============================================================
//  MainMenuManager.cs
//  Attach to a GameObject named "MainMenuManager" in your
//  Main Menu scene.  Assign all references in the Inspector.
// ============================================================
public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject confirmQuitPanel;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;

    [Header("Settings UI")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Button settingsBackButton;
    [SerializeField] private Button settingsApplyButton;

    [Header("Credits UI")]
    [SerializeField] private Button creditsBackButton;

    [Header("Confirm Quit UI")]
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    [Header("Title Text")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Scene Settings")]
    [SerializeField] private string gameplaySceneName = "Level_01";

    [Header("Horror Flicker Effect")]
    [SerializeField] private float flickerMinInterval = 3f;
    [SerializeField] private float flickerMaxInterval = 10f;
    [SerializeField] private float flickerDuration = 0.08f;

    // -------------------------------------------------------
    private Resolution[] availableResolutions;
    private bool hasSaveData = false;

    // -------------------------------------------------------
    private void Awake()
    {
        // Check for save data so Continue is only enabled when valid
        hasSaveData = PlayerPrefs.HasKey("SaveExists");
        if (continueButton != null)
            continueButton.interactable = hasSaveData;
    }

    private void Start()
    {
        PopulateResolutionDropdown();
        LoadSettingsToUI();
        BindButtons();

        ShowPanel(mainPanel);
        HidePanel(settingsPanel);
        HidePanel(creditsPanel);
        HidePanel(confirmQuitPanel);

        StartCoroutine(RandomFlickerRoutine());
    }

    // -------------------------------------------------------
    //  Button Binding
    // -------------------------------------------------------
    private void BindButtons()
    {
        newGameButton.onClick.AddListener(OnNewGame);
        continueButton.onClick.AddListener(OnContinue);
        settingsButton.onClick.AddListener(OnOpenSettings);
        creditsButton.onClick.AddListener(OnOpenCredits);
        quitButton.onClick.AddListener(OnQuit);

        settingsBackButton.onClick.AddListener(OnSettingsBack);
        settingsApplyButton.onClick.AddListener(OnSettingsApply);

        creditsBackButton.onClick.AddListener(OnCreditsBack);

        confirmYesButton.onClick.AddListener(OnConfirmQuitYes);
        confirmNoButton.onClick.AddListener(OnConfirmQuitNo);
    }

    // -------------------------------------------------------
    //  Navigation
    // -------------------------------------------------------
    private void OnNewGame()
    {
        PlayerPrefs.DeleteAll();
        SceneManager.LoadScene(gameplaySceneName);
    }

    private void OnContinue()
    {
        if (hasSaveData)
            SceneManager.LoadScene(gameplaySceneName);
    }

    private void OnOpenSettings()
    {
        HidePanel(mainPanel);
        ShowPanel(settingsPanel);
    }

    private void OnOpenCredits()
    {
        HidePanel(mainPanel);
        ShowPanel(creditsPanel);
    }

    private void OnQuit()
    {
        HidePanel(mainPanel);
        ShowPanel(confirmQuitPanel);
    }

    private void OnSettingsBack()
    {
        HidePanel(settingsPanel);
        ShowPanel(mainPanel);
    }

    private void OnCreditsBack()
    {
        HidePanel(creditsPanel);
        ShowPanel(mainPanel);
    }

    private void OnConfirmQuitYes()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnConfirmQuitNo()
    {
        HidePanel(confirmQuitPanel);
        ShowPanel(mainPanel);
    }

    // -------------------------------------------------------
    //  Settings Logic
    // -------------------------------------------------------
    private void OnSettingsApply()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
        PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
        PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionDropdown.value);
        PlayerPrefs.Save();

        ApplyResolution(resolutionDropdown.value);
        Screen.fullScreen = fullscreenToggle.isOn;

        AudioListener.volume = masterVolumeSlider.value;
    }

    private void LoadSettingsToUI()
    {
        masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        int savedRes = PlayerPrefs.GetInt("ResolutionIndex", 0);
        if (resolutionDropdown != null && savedRes < resolutionDropdown.options.Count)
            resolutionDropdown.value = savedRes;
    }

    private void PopulateResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        availableResolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        var options = new System.Collections.Generic.List<string>();
        int currentIndex = 0;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            string option = availableResolutions[i].width + " x " + availableResolutions[i].height;
            options.Add(option);

            if (availableResolutions[i].width == Screen.currentResolution.width &&
                availableResolutions[i].height == Screen.currentResolution.height)
            {
                currentIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentIndex;
        resolutionDropdown.RefreshShownValue();
    }

    private void ApplyResolution(int index)
    {
        if (availableResolutions == null || index >= availableResolutions.Length) return;
        Resolution res = availableResolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    }

    // -------------------------------------------------------
    //  Panel Helpers
    // -------------------------------------------------------
    private void ShowPanel(GameObject panel)
    {
        if (panel != null) panel.SetActive(true);
    }

    private void HidePanel(GameObject panel)
    {
        if (panel != null) panel.SetActive(false);
    }

    // -------------------------------------------------------
    //  Horror Flicker (title text glitches randomly)
    // -------------------------------------------------------
    private IEnumerator RandomFlickerRoutine()
    {
        while (true)
        {
            float wait = Random.Range(flickerMinInterval, flickerMaxInterval);
            yield return new WaitForSeconds(wait);
            yield return StartCoroutine(FlickerTitle());
        }
    }

    private IEnumerator FlickerTitle()
    {
        if (titleText == null) yield break;

        int flickers = Random.Range(2, 6);
        for (int i = 0; i < flickers; i++)
        {
            titleText.enabled = false;
            yield return new WaitForSeconds(flickerDuration);
            titleText.enabled = true;
            yield return new WaitForSeconds(flickerDuration * 0.5f);
        }
    }
}