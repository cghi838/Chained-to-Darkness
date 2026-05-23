using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  DialogueManager.cs
//  Typewriter-effect dialogue system for narrative text,
//  NPC speech, and journal entry display.
//  Attach to a Canvas child named "DialoguePanel".
// ============================================================
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Panel References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueBodyText;
    [SerializeField] private TextMeshProUGUI continueHintText;  // e.g. "[ Press E to continue ]"

    [Header("Speaker Name Background")]
    [SerializeField] private Image nameBackgroundImage;
    [SerializeField] private Color playerNameColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
    [SerializeField] private Color npcNameColor = new Color(0.25f, 0.05f, 0.25f, 0.9f);
    [SerializeField] private Color narratorNameColor = new Color(0.05f, 0.05f, 0.05f, 0.0f);

    [Header("Typewriter Settings")]
    [SerializeField] private float normalTypeSpeed = 0.04f;
    [SerializeField] private float fastTypeSpeed = 0.01f;
    [SerializeField] private float horrorTypeSpeed = 0.12f;
    [SerializeField] private float punctuationPause = 0.18f;

    [Header("Horror Glitch on Text")]
    [SerializeField] private bool enableTextGlitch = true;
    [SerializeField] private float glitchChance = 0.04f;
    [SerializeField] private string glitchCharacters = "#@!?%*^~";

    [Header("Fade")]
    [SerializeField] private float panelFadeDuration = 0.4f;

    [Header("Audio")]
    [SerializeField] private AudioSource dialogueAudioSource;
    [SerializeField] private AudioClip defaultTypeClip;
    [SerializeField] private AudioClip horrorTypeClip;

    // -------------------------------------------------------
    public enum SpeakerType { Player, NPC, Narrator }

    [System.Serializable]
    public class DialogueLine
    {
        public string speakerName;
        public SpeakerType speakerType;
        [TextArea(2, 6)]
        public string text;
        public bool useHorrorEffect;
    }

    // -------------------------------------------------------
    private DialogueLine[] currentLines;
    private int currentIndex = 0;
    private bool isTyping = false;
    private bool isOpen = false;
    private Coroutine typeCoroutine;

    // -------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (continueHintText != null) continueHintText.enabled = false;
    }

    private void Update()
    {
        if (!isOpen) return;

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) ||
            Input.GetMouseButtonDown(0))
        {
            if (isTyping)
                SkipTypewriter();
            else
                AdvanceLine();
        }
    }

    // -------------------------------------------------------
    //  Public API
    // -------------------------------------------------------
    public void StartDialogue(DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0) return;
        currentLines = lines;
        currentIndex = 0;
        isOpen = true;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        StartCoroutine(FadePanel(0f, 1f));

        DisplayLine(currentLines[currentIndex]);
    }

    public void CloseDialogue()
    {
        isOpen = false;
        if (typeCoroutine != null) StopCoroutine(typeCoroutine);
        StartCoroutine(CloseRoutine());
    }

    // -------------------------------------------------------
    //  Line Display
    // -------------------------------------------------------
    private void DisplayLine(DialogueLine line)
    {
        SetSpeakerUI(line.speakerName, line.speakerType);

        if (typeCoroutine != null) StopCoroutine(typeCoroutine);
        if (continueHintText != null) continueHintText.enabled = false;

        float speed = line.useHorrorEffect ? horrorTypeSpeed : normalTypeSpeed;
        AudioClip clip = line.useHorrorEffect ? horrorTypeClip : defaultTypeClip;

        typeCoroutine = StartCoroutine(TypeLine(line.text, speed, clip));
    }

    private void SetSpeakerUI(string name, SpeakerType type)
    {
        if (speakerNameText != null)
        {
            speakerNameText.text = (type == SpeakerType.Narrator) ? "" : name;
        }

        if (nameBackgroundImage != null)
        {
            switch (type)
            {
                case SpeakerType.Player: nameBackgroundImage.color = playerNameColor; break;
                case SpeakerType.NPC: nameBackgroundImage.color = npcNameColor; break;
                case SpeakerType.Narrator: nameBackgroundImage.color = narratorNameColor; break;
            }
        }
    }

    // -------------------------------------------------------
    //  Typewriter Coroutine
    // -------------------------------------------------------
    private IEnumerator TypeLine(string line, float speed, AudioClip clip)
    {
        isTyping = true;
        dialogueBodyText.text = "";

        char[] glitchChars = glitchCharacters.ToCharArray();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            // Random glitch character flash
            if (enableTextGlitch && Random.value < glitchChance && i > 0)
            {
                char fake = glitchChars[Random.Range(0, glitchChars.Length)];
                dialogueBodyText.text = line.Substring(0, i) + fake;
                yield return new WaitForSeconds(speed * 0.5f);
            }

            dialogueBodyText.text = line.Substring(0, i + 1);

            // Play typing sound
            if (dialogueAudioSource != null && clip != null && !dialogueAudioSource.isPlaying)
                dialogueAudioSource.PlayOneShot(clip, 0.4f);

            // Pause longer after punctuation
            if (c == '.' || c == ',' || c == '!' || c == '?' || c == ';')
                yield return new WaitForSeconds(punctuationPause);
            else
                yield return new WaitForSeconds(speed);
        }

        isTyping = false;
        if (continueHintText != null) continueHintText.enabled = true;
    }

    private void SkipTypewriter()
    {
        if (typeCoroutine != null) StopCoroutine(typeCoroutine);
        isTyping = false;
        dialogueBodyText.text = currentLines[currentIndex].text;
        if (continueHintText != null) continueHintText.enabled = true;
    }

    // -------------------------------------------------------
    //  Navigation
    // -------------------------------------------------------
    private void AdvanceLine()
    {
        currentIndex++;
        if (currentIndex < currentLines.Length)
            DisplayLine(currentLines[currentIndex]);
        else
            CloseDialogue();
    }

    // -------------------------------------------------------
    //  Close Routine
    // -------------------------------------------------------
    private IEnumerator CloseRoutine()
    {
        yield return StartCoroutine(FadePanel(1f, 0f));
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    // -------------------------------------------------------
    //  Fade Utility
    // -------------------------------------------------------
    private IEnumerator FadePanel(float from, float to)
    {
        if (panelCanvasGroup == null) yield break;
        float elapsed = 0f;
        panelCanvasGroup.alpha = from;
        while (elapsed < panelFadeDuration)
        {
            elapsed += Time.deltaTime;
            panelCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / panelFadeDuration);
            yield return null;
        }
        panelCanvasGroup.alpha = to;
    }
}