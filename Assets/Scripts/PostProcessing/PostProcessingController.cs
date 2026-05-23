using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessingController : MonoBehaviour
{
    [Header("URP Global Volume")]
    [SerializeField] private Volume globalVolume;

    [Header("Gray Effect")]
    [SerializeField] private float graySaturation = -100f;

    [Header("Sanity Effect")]
    [SerializeField] private SanitySystem sanitySystem;
    [SerializeField] private float maxVignette = 0.6f;
    [SerializeField] private float maxAberration = 1f;

    private ColorAdjustments colorAdjustments;
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;
    private bool isGrayOn = false;

    void Start()
    {
        // Check if the Global Volume reference is assigned
        if (globalVolume == null)
        {
            Debug.LogError("Global Volume is not assigned.");
            enabled = false;
            return;
        }

        var profile = globalVolume.profile;
        // Try to get the Color Adjustments override from the Volume Profile
        if (!profile.TryGet(out colorAdjustments))
        {
            Debug.LogError("Color Adjustments not found in Volume Profile.");
            enabled = false;
            return;
        }
        profile.TryGet(out vignette);
        profile.TryGet(out chromaticAberration);

        // Ensure saturation is controlled by script and starts at normal color
        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.saturation.value = 0f;
        Debug.Log("PostProcessingController started");
    }

    void Update()
    {
        // New Input System: press G key to test gray effect
        if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
        {
            ToggleGrayEffect();
        }

        // Update on effects based on mental power
        if (sanitySystem != null) UpdateSanityEffects(sanitySystem.GetRatio());
    }

    public void OnSanityLow()
    {
        Debug.Log("Sanity Low — Enhanced effect");
        maxVignette = 0.5f;
        maxAberration = 0.6f;
    }

    public void OnSanityCritical()
    {
        Debug.Log("Sanity Critical — Maximum effect");
        maxVignette = 0.8f;
        maxAberration = 1f;
        if (colorAdjustments != null)
            colorAdjustments.saturation.value = -50f; // Color fading begins
    }

    public void OnSanityRestored()
    {
        Debug.Log("Sanity Restored — Reset effect");
        maxVignette = 0.6f;
        maxAberration = 1f;
        if (colorAdjustments != null)
            colorAdjustments.saturation.value = 0f;
    }

    public void ToggleGrayEffect()
    {
        if (colorAdjustments == null) return;

        isGrayOn = !isGrayOn; // toggle
        colorAdjustments.saturation.value = isGrayOn ? graySaturation : 0f;
        Debug.Log("ToggleGrayEffect isGrayOn=" + isGrayOn + " colorAdjustments saturation={1}" + colorAdjustments.saturation.value);
    }

    public void UpdateSanityEffects(float sanityRatio)
    {
        // The lower the mental power, the stronger the effect (1 - ratio)
        float intensity = 1f - sanityRatio;

        if (vignette != null)
        {
            vignette.intensity.overrideState = true;
            vignette.intensity.value = Mathf.Lerp(0f, maxVignette, intensity);
        }

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.overrideState = true;
            chromaticAberration.intensity.value = Mathf.Lerp(0f, maxAberration, intensity);
        }
    }
}
