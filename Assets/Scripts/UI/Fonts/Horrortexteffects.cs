using System.Collections;
using UnityEngine;
using TMPro;

// ============================================================
//  HorrorTextEffects.cs
//  Drop this onto any GameObject that has a TextMeshProUGUI
//  to add horror visual effects at runtime.
//
//  EFFECT MODES:
//  None        - No effect
//  Flicker     - Random alpha flickering
//  Pulsate     - Sine-wave scale pulse
//  Shake       - Position jitter
//  ColorShift  - Slow color drift between two colors
//  RandomGlitch - Periodically scrambles characters
// ============================================================
public class HorrorTextEffects : MonoBehaviour
{
    public enum EffectMode
    {
        None,
        Flicker,
        Pulsate,
        Shake,
        ColorShift,
        RandomGlitch
    }

    [Header("Target (leave null to use self)")]
    [SerializeField] private TextMeshProUGUI targetText;

    [Header("Effect")]
    [SerializeField] private EffectMode mode = EffectMode.Flicker;

    [Header("Flicker")]
    [SerializeField] private float flickerMinAlpha = 0.1f;
    [SerializeField] private float flickerSpeed = 8f;

    [Header("Pulsate")]
    [SerializeField] private float pulseAmplitude = 0.05f;
    [SerializeField] private float pulseSpeed = 2f;

    [Header("Shake")]
    [SerializeField] private float shakeIntensity = 2f;
    [SerializeField] private float shakeSpeed = 20f;

    [Header("Color Shift")]
    [SerializeField] private Color colorA = new Color(0.8f, 0.1f, 0.1f);
    [SerializeField] private Color colorB = new Color(0.5f, 0.0f, 0.5f);
    [SerializeField] private float colorShiftSpeed = 0.8f;

    [Header("Random Glitch")]
    [SerializeField] private float glitchInterval = 2.5f;
    [SerializeField] private float glitchDuration = 0.15f;
    [SerializeField] private string glitchChars = "#@!?~^";

    // -------------------------------------------------------
    private RectTransform rectTransform;
    private Vector2 originalAnchoredPos;
    private Vector3 originalScale;
    private string originalText;
    private float glitchTimer = 0f;

    // -------------------------------------------------------
    private void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TextMeshProUGUI>();

        if (targetText != null)
        {
            rectTransform = targetText.GetComponent<RectTransform>();
            originalAnchoredPos = rectTransform.anchoredPosition;
            originalScale = rectTransform.localScale;
            originalText = targetText.text;
        }
    }

    private void Update()
    {
        if (targetText == null) return;

        switch (mode)
        {
            case EffectMode.Flicker: DoFlicker(); break;
            case EffectMode.Pulsate: DoPulsate(); break;
            case EffectMode.Shake: DoShake(); break;
            case EffectMode.ColorShift: DoColorShift(); break;
            case EffectMode.RandomGlitch: DoRandomGlitch(); break;
        }
    }

    private void OnDisable()
    {
        if (targetText == null || rectTransform == null) return;
        rectTransform.anchoredPosition = originalAnchoredPos;
        rectTransform.localScale = originalScale;
        Color c = targetText.color;
        c.a = 1f;
        targetText.color = c;
        targetText.text = originalText;
    }

    // -------------------------------------------------------
    //  Effects
    // -------------------------------------------------------
    private void DoFlicker()
    {
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        float alpha = Mathf.Lerp(flickerMinAlpha, 1f, noise);
        Color c = targetText.color;
        c.a = alpha;
        targetText.color = c;
    }

    private void DoPulsate()
    {
        float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
        rectTransform.localScale = originalScale * scale;
    }

    private void DoShake()
    {
        float x = (Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) - 0.5f) * 2f * shakeIntensity;
        float y = (Mathf.PerlinNoise(0f, Time.time * shakeSpeed) - 0.5f) * 2f * shakeIntensity;
        rectTransform.anchoredPosition = originalAnchoredPos + new Vector2(x, y);
    }

    private void DoColorShift()
    {
        float t = (Mathf.Sin(Time.time * colorShiftSpeed) + 1f) * 0.5f;
        targetText.color = Color.Lerp(colorA, colorB, t);
    }

    private void DoRandomGlitch()
    {
        glitchTimer += Time.deltaTime;
        if (glitchTimer >= glitchInterval)
        {
            glitchTimer = 0f;
            StopAllCoroutines();
            StartCoroutine(GlitchText());
        }
    }

    private IEnumerator GlitchText()
    {
        char[] chars = originalText.ToCharArray();
        char[] glitch = glitchChars.ToCharArray();

        float elapsed = 0f;
        while (elapsed < glitchDuration)
        {
            char[] display = (char[])chars.Clone();
            int swaps = Random.Range(1, 4);
            for (int i = 0; i < swaps; i++)
            {
                int idx = Random.Range(0, display.Length);
                display[idx] = glitch[Random.Range(0, glitch.Length)];
            }
            targetText.text = new string(display);
            elapsed += Time.deltaTime;
            yield return null;
        }
        targetText.text = originalText;
    }

    // -------------------------------------------------------
    //  Public Control
    // -------------------------------------------------------
    public void SetMode(EffectMode newMode)
    {
        StopAllCoroutines();
        mode = newMode;

        if (newMode == EffectMode.None && rectTransform != null)
        {
            rectTransform.anchoredPosition = originalAnchoredPos;
            rectTransform.localScale = originalScale;
            Color c = targetText.color;
            c.a = 1f;
            targetText.color = c;
            targetText.text = originalText;
        }
    }

    // Refresh the cached original text (call this if you change
    // the text content at runtime before starting an effect)
    public void RefreshOriginalText()
    {
        if (targetText != null)
            originalText = targetText.text;
    }
}