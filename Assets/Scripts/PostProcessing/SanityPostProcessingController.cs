using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Volume))]
public class SanityPostProcessingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SanitySystem sanitySystem;

    [Header("Transition")]
    [SerializeField] private float transitionDuration = 1.8f;

    [Header("Memory Durations (sec)")]
    [SerializeField] private float happyDuration = 3.5f;
    [SerializeField] private float painfulDuration = 2.5f;
    [SerializeField] private float childhoodDuration = 5.2f;

    private const float restoreDelayTime = 3f; // wait time before restoring sanity just in case SanitySystem is null

    //  StageProfile 
    private struct StageProfile
    {
        // Vignette
        public float vignetteIntensity;
        public float vignetteSmoothness;
        // FilmGrain
        public float grainIntensity;
        // ColorAdjustments
        public float saturation;        // -100 ~ 100
        public float postExposure;
        public float contrast;          // -100 ~ 100
        public Color colorFilter;
        // ChromaticAberration
        public float chromaticIntensity;
        // LensDistortion
        public float lensDistortion;    // -1 ~ 1
        // Bloom
        public float bloomIntensity;
        public float bloomThreshold;
        // WhiteBalance
        public float whiteBalanceTemp;  // -100 ~ 100 (positive=warm)
        public float whiteBalanceTint;
        // Lift (color adjust)
        public Vector4 lift;
    }

    // Stable (70+) 
    private static readonly StageProfile ProfileStable = new StageProfile
    {
        vignetteIntensity = 0.12f,
        vignetteSmoothness = 0.35f,
        grainIntensity = 0.0f,
        saturation = 0f,
        postExposure = 0f,
        contrast = 0f,
        colorFilter = Color.white,
        chromaticIntensity = 0f,
        lensDistortion = 0f,
        bloomIntensity = 0.5f,
        bloomThreshold = 0.9f,
        whiteBalanceTemp = 0f,
        whiteBalanceTint = 0f,
        lift = new Vector4(1f, 1f, 1f, 0f)
    };

    // Unstable (40~69) 
    private static readonly StageProfile ProfileUnstable = new StageProfile
    {
        vignetteIntensity = 0.35f,
        vignetteSmoothness = 0.5f,
        grainIntensity = 0.35f,
        saturation = -20f,
        postExposure = -0.25f,
        contrast = 8f,
        colorFilter = new Color(0.92f, 0.92f, 1.0f),
        chromaticIntensity = 0.25f,
        lensDistortion = -0.05f,
        bloomIntensity = 0.3f,
        bloomThreshold = 1.0f,
        whiteBalanceTemp = -10f,
        whiteBalanceTint = 0f,
        lift = new Vector4(0.95f, 0.95f, 1.02f, 0f)
    };

    // Critical (10~39) 
    private static readonly StageProfile ProfileCritical = new StageProfile
    {
        vignetteIntensity = 0.62f,
        vignetteSmoothness = 0.7f,
        grainIntensity = 0.75f,
        saturation = -55f,
        postExposure = -0.6f,
        contrast = 22f,
        colorFilter = new Color(1.0f, 0.78f, 0.78f),
        chromaticIntensity = 0.7f,
        lensDistortion = -0.14f,
        bloomIntensity = 0.15f,
        bloomThreshold = 1.2f,
        whiteBalanceTemp = -20f,
        whiteBalanceTint = 10f,
        lift = new Vector4(1.05f, 0.9f, 0.9f, 0f)
    };

    // Broken (0) 
    private static readonly StageProfile ProfileBroken = new StageProfile
    {
        vignetteIntensity = 0.92f,
        vignetteSmoothness = 1.0f,
        grainIntensity = 1.0f,
        saturation = -100f,
        postExposure = -1.8f,
        contrast = 45f,
        colorFilter = new Color(0.5f, 0.1f, 0.1f),
        chromaticIntensity = 1.0f,
        lensDistortion = -0.3f,
        bloomIntensity = 0f,
        bloomThreshold = 1.5f,
        whiteBalanceTemp = -30f,
        whiteBalanceTint = 20f,
        lift = new Vector4(1.1f, 0.8f, 0.8f, 0f)
    };

    // Happy Memory 
    private static readonly StageProfile ProfileHappy = new StageProfile
    {
        vignetteIntensity = 0.03f,
        vignetteSmoothness = 0.2f,
        grainIntensity = 0f,
        saturation = 15f,
        postExposure = 0.35f,
        contrast = -8f,
        colorFilter = new Color(1.0f, 0.95f, 0.85f), // warm cream
        chromaticIntensity = 0f,
        lensDistortion = 0f,
        bloomIntensity = 2.5f,
        bloomThreshold = 0.5f,
        whiteBalanceTemp = 28f, // warm
        whiteBalanceTint = -5f,
        lift = new Vector4(1.0f, 0.98f, 0.9f, 0f)
    };

    // Painful Memory 
    private static readonly StageProfile ProfilePainful = new StageProfile
    {
        vignetteIntensity = 0.88f,
        vignetteSmoothness = 0.9f,
        grainIntensity = 0.55f,
        saturation = -40f,
        postExposure = -1.0f,
        contrast = 35f,
        colorFilter = new Color(1.0f, 0.55f, 0.55f), // strong red tint
        chromaticIntensity = 0.85f,
        lensDistortion = -0.22f,
        bloomIntensity = 0f,
        bloomThreshold = 1.5f,
        whiteBalanceTemp = -18f,
        whiteBalanceTint = 18f,
        lift = new Vector4(1.1f, 0.78f, 0.78f, 0f)
    };

    // Childhood Memory - low saturation, warm sepia, vignette oval
    private static readonly StageProfile ProfileChildhood = new StageProfile
    {
        vignetteIntensity = 0.28f,
        vignetteSmoothness = 0.65f,
        grainIntensity = 0.18f,
        saturation = -30f,
        postExposure = 0.15f,
        contrast = -5f,
        colorFilter = new Color(1.0f, 0.93f, 0.78f), // sepia cream
        chromaticIntensity = 0.08f,
        lensDistortion = 0.05f, // little bit oval lens
        bloomIntensity = 1.2f,
        bloomThreshold = 0.7f,
        whiteBalanceTemp = 22f,
        whiteBalanceTint = -3f,
        lift = new Vector4(1.0f, 0.96f, 0.85f, 0f)
    };

    //  Volume Effect reference
    private Volume volume;
    private Vignette vignette;
    private FilmGrain filmGrain;
    private ColorAdjustments colorAdjustments;
    private ChromaticAberration chromaticAberration;
    private LensDistortion lensDistortion;
    private Bloom bloom;
    private WhiteBalance whiteBalance;
    private LiftGammaGain liftGammaGain;

    //  Runtime 
    private StageProfile currentProfile;
    private StageProfile targetProfile;     // after Memory, come back to target Stage Profile

    private Coroutine transitionCoroutine;
    private Coroutine memoryCoroutine;
    private Coroutine glitchCoroutine;

    //  Unity Lifecycle
    private void Awake()
    {
        volume = GetComponent<Volume>();
        volume.profile = Instantiate(volume.profile);

        volume.profile.TryGet(out vignette);
        volume.profile.TryGet(out filmGrain);
        volume.profile.TryGet(out colorAdjustments);
        volume.profile.TryGet(out chromaticAberration);
        volume.profile.TryGet(out lensDistortion);
        volume.profile.TryGet(out bloom);
        volume.profile.TryGet(out whiteBalance);
        volume.profile.TryGet(out liftGammaGain);
    }

    private void OnEnable()
    {
        if (sanitySystem == null) return;
        sanitySystem.OnSanityStageChanged += HandleStageChanged;
    }

    private void OnDisable()
    {
        if (sanitySystem != null)
            sanitySystem.OnSanityStageChanged -= HandleStageChanged;
    }

    private void Start()
    {
        if (sanitySystem != null)
            ApplyProfileImmediate(StageToProfile(sanitySystem.GetCurrentStage()));
    }

    // Event Handler

    private void HandleStageChanged(SanitySystem.SanityStage stage)
    {
        StageProfile next = StageToProfile(stage);

        if (memoryCoroutine != null)
        {
            targetProfile = next;
            return;
        }

        TransitionTo(next, transitionDuration);

        if (stage == SanitySystem.SanityStage.Broken)
            StartGlitch();
        else
            StopGlitch();
    }

    public void TriggerMemoryByFragment(MemoryFragment fragment)
    {
        if (fragment == null) return;
        string n = fragment.fragmentName ?? "";

        if (n.IndexOf("Happy", System.StringComparison.OrdinalIgnoreCase) >= 0)
            TriggerHappyMemory();
        else if (n.IndexOf("Painful", System.StringComparison.OrdinalIgnoreCase) >= 0)
            TriggerPainfulMemory();
        else if (n.IndexOf("Childhood", System.StringComparison.OrdinalIgnoreCase) >= 0)
            TriggerChildhoodMemory();
    }

    public void TriggerHappyMemory()
    {
        StopMemoryCoroutine();
        memoryCoroutine = StartCoroutine(HappyMemoryRoutine());
    }

    public void TriggerPainfulMemory()
    {
        StopMemoryCoroutine();
        memoryCoroutine = StartCoroutine(PainfulMemoryRoutine());
    }

    public void TriggerChildhoodMemory()
    {
        StopMemoryCoroutine();
        memoryCoroutine = StartCoroutine(ChildhoodMemoryRoutine());
    }

    //  Memory Coroutine

    private IEnumerator HappyMemoryRoutine()
    {
        StopGlitch();
        CacheReturnProfile();

        float fadeIn = 0.8f;
        float hold = happyDuration * 0.55f;
        float fadeOut = happyDuration - fadeIn - hold;

        yield return StartCoroutine(LerpProfile(currentProfile, ProfileHappy, fadeIn, EaseOutCubic));
        yield return StartCoroutine(HappyBreathHold(hold));
        yield return StartCoroutine(LerpProfile(ProfileHappy, targetProfile, fadeOut, EaseInCubic));

        memoryCoroutine = null;
        RestoreGlitchIfBroken();
    }

    private IEnumerator PainfulMemoryRoutine()
    {
        StopGlitch();
        CacheReturnProfile();

        float flashIn = 0.15f;
        float hold = painfulDuration * 0.25f;
        float shakeOut = painfulDuration * 0.15f;
        float fadeOut = painfulDuration - flashIn - hold - shakeOut;

        yield return StartCoroutine(LerpProfile(currentProfile, ProfilePainful, flashIn, EaseOutCubic));
        yield return StartCoroutine(PainfulShakeHold(hold, dampening: false));
        yield return StartCoroutine(PainfulShakeHold(shakeOut, dampening: true));
        yield return StartCoroutine(LerpProfile(currentProfile, targetProfile, fadeOut, EaseInOutCubic));

        memoryCoroutine = null;
        RestoreGlitchIfBroken();
    }

    private IEnumerator ChildhoodMemoryRoutine()
    {
        StopGlitch();
        CacheReturnProfile();

        float fadeIn = childhoodDuration * 0.27f;
        float hold = childhoodDuration * 0.38f;
        float fadeOut = childhoodDuration - fadeIn - hold;

        yield return StartCoroutine(LerpProfile(currentProfile, ProfileChildhood, fadeIn, EaseInOutCubic));

        float elapsed = 0f;
        while (elapsed < hold)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.Sin(elapsed * Mathf.PI * 0.6f) * 0.5f + 0.5f;
            if (bloom != null && bloom.active)
                bloom.intensity.value = Mathf.Lerp(ProfileChildhood.bloomIntensity,
                                                   ProfileChildhood.bloomIntensity * 1.15f, pulse);
            yield return null;
        }
        ApplyProfileImmediate(ProfileChildhood);

        yield return StartCoroutine(LerpProfile(ProfileChildhood, targetProfile, fadeOut, EaseInCubic));

        memoryCoroutine = null;
        RestoreGlitchIfBroken();
    }

    private IEnumerator HappyBreathHold(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.Sin(elapsed / duration * Mathf.PI * 2.2f) * 0.5f + 0.5f;

            if (bloom != null && bloom.active)
            {
                bloom.intensity.value = Mathf.Lerp(ProfileHappy.bloomIntensity,
                                                   ProfileHappy.bloomIntensity * 1.4f, pulse);
                bloom.threshold.value = Mathf.Lerp(ProfileHappy.bloomThreshold,
                                                   ProfileHappy.bloomThreshold * 0.85f, pulse);
            }
            if (colorAdjustments != null && colorAdjustments.active)
                colorAdjustments.postExposure.value = ProfileHappy.postExposure + pulse * 0.18f;

            yield return null;
        }
        ApplyProfileImmediate(ProfileHappy);
    }

    private IEnumerator PainfulShakeHold(float duration, bool dampening)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float damp = dampening ? (1f - elapsed / duration) : 1f;

            if (chromaticAberration != null && chromaticAberration.active)
                chromaticAberration.intensity.value =
                    ProfilePainful.chromaticIntensity * damp
                    * Mathf.Lerp(0.7f, 1.0f, Mathf.PerlinNoise(elapsed * 18f, 0f));

            if (lensDistortion != null && lensDistortion.active)
                lensDistortion.intensity.value =
                    ProfilePainful.lensDistortion * damp
                    * Mathf.Lerp(0.6f, 1.0f, Mathf.PerlinNoise(elapsed * 14f, 5f));

            if (colorAdjustments != null && colorAdjustments.active)
                colorAdjustments.postExposure.value =
                    ProfilePainful.postExposure
                    + Mathf.Sin(elapsed * Mathf.PI * 6f) * 0.25f * damp;

            yield return null;
        }
    }

    private void CacheReturnProfile()
    {
        targetProfile = StageToProfile(
            sanitySystem != null ? sanitySystem.GetCurrentStage()
                                 : SanitySystem.SanityStage.Stable);
    }

    private void StopMemoryCoroutine()
    {
        if (memoryCoroutine == null) return;
        StopCoroutine(memoryCoroutine);
        memoryCoroutine = null;
    }

    private void RestoreGlitchIfBroken()
    {
        if (sanitySystem != null &&
            sanitySystem.GetCurrentStage() == SanitySystem.SanityStage.Broken)
            StartGlitch();
    }

    //  Transition to Stage
    private void TransitionTo(StageProfile next, float duration)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(LerpProfile(currentProfile, next, duration, EaseInOutCubic));
    }


    //  Broken Glitch
    private void StartGlitch()
    {
        if (glitchCoroutine != null) return;
        glitchCoroutine = StartCoroutine(GlitchRoutine());
    }

    private void StopGlitch()
    {
        if (glitchCoroutine == null) return;
        StopCoroutine(glitchCoroutine);
        glitchCoroutine = null;
    }

    private IEnumerator GlitchRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(0.8f, 3.5f));

            if (Random.value < 0.6f)
                yield return StartCoroutine(GlitchFlash());
            else
                yield return StartCoroutine(GlitchChromatic());
        }
    }

    private IEnumerator GlitchFlash()
    {
        Debug.Log("[Glitch] Flash glitch triggered.");
        if (colorAdjustments != null && colorAdjustments.active)
        {
            colorAdjustments.postExposure.value = Random.Range(0.5f, 1.2f);
            yield return new WaitForSecondsRealtime(sanitySystem?.GetRestoreDelayTime() ?? restoreDelayTime);
            //            yield return new WaitForSeconds(0.05f);
            colorAdjustments.postExposure.value = ProfileBroken.postExposure;
            sanitySystem?.RestoreSanity(70f);
        }
        yield return null;
    }

    private IEnumerator GlitchChromatic()
    {
        Debug.Log("[Glitch] Chromatic glitch triggered.");
        if (chromaticAberration != null && chromaticAberration.active)
        {
            chromaticAberration.intensity.value = 1.0f;
            //yield return new WaitForSeconds(Random.Range(0.03f, 0.1f));
            yield return new WaitForSecondsRealtime(sanitySystem?.GetRestoreDelayTime() ?? restoreDelayTime);
            chromaticAberration.intensity.value = ProfileBroken.chromaticIntensity;
            sanitySystem?.RestoreSanity(70f);
        }
        yield return null;
    }

    // Profile lerp

    private IEnumerator LerpProfile(StageProfile from, StageProfile to, float duration,
                                    System.Func<float, float> easing)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = easing(Mathf.Clamp01(elapsed / duration));
            ApplyProfileImmediate(new StageProfile
            {
                vignetteIntensity = Mathf.Lerp(from.vignetteIntensity, to.vignetteIntensity, t),
                vignetteSmoothness = Mathf.Lerp(from.vignetteSmoothness, to.vignetteSmoothness, t),
                grainIntensity = Mathf.Lerp(from.grainIntensity, to.grainIntensity, t),
                saturation = Mathf.Lerp(from.saturation, to.saturation, t),
                postExposure = Mathf.Lerp(from.postExposure, to.postExposure, t),
                contrast = Mathf.Lerp(from.contrast, to.contrast, t),
                colorFilter = Color.Lerp(from.colorFilter, to.colorFilter, t),
                chromaticIntensity = Mathf.Lerp(from.chromaticIntensity, to.chromaticIntensity, t),
                lensDistortion = Mathf.Lerp(from.lensDistortion, to.lensDistortion, t),
                bloomIntensity = Mathf.Lerp(from.bloomIntensity, to.bloomIntensity, t),
                bloomThreshold = Mathf.Lerp(from.bloomThreshold, to.bloomThreshold, t),
                whiteBalanceTemp = Mathf.Lerp(from.whiteBalanceTemp, to.whiteBalanceTemp, t),
                whiteBalanceTint = Mathf.Lerp(from.whiteBalanceTint, to.whiteBalanceTint, t),
                lift = Vector4.Lerp(from.lift, to.lift, t)
            });
            yield return null;
        }
        ApplyProfileImmediate(to);
        currentProfile = to;
    }

    private void ApplyProfileImmediate(StageProfile p)
    {
        currentProfile = p;

        if (vignette != null && vignette.active)
        {
            vignette.intensity.value = p.vignetteIntensity;
            vignette.smoothness.value = p.vignetteSmoothness;
        }
        if (filmGrain != null && filmGrain.active)
            filmGrain.intensity.value = p.grainIntensity;

        if (colorAdjustments != null && colorAdjustments.active)
        {
            colorAdjustments.saturation.value = p.saturation;
            colorAdjustments.postExposure.value = p.postExposure;
            colorAdjustments.contrast.value = p.contrast;
            colorAdjustments.colorFilter.value = p.colorFilter;
        }
        if (chromaticAberration != null && chromaticAberration.active)
            chromaticAberration.intensity.value = p.chromaticIntensity;

        if (lensDistortion != null && lensDistortion.active)
            lensDistortion.intensity.value = p.lensDistortion;

        if (bloom != null && bloom.active)
        {
            bloom.intensity.value = p.bloomIntensity;
            bloom.threshold.value = p.bloomThreshold;
        }
        if (whiteBalance != null && whiteBalance.active)
        {
            whiteBalance.temperature.value = p.whiteBalanceTemp;
            whiteBalance.tint.value = p.whiteBalanceTint;
        }
        if (liftGammaGain != null && liftGammaGain.active)
            liftGammaGain.lift.value = p.lift;
    }

    // Util

    private static StageProfile StageToProfile(SanitySystem.SanityStage stage) =>
        stage switch
        {
            SanitySystem.SanityStage.Stable => ProfileStable,
            SanitySystem.SanityStage.Unstable => ProfileUnstable,
            SanitySystem.SanityStage.Critical => ProfileCritical,
            SanitySystem.SanityStage.Broken => ProfileBroken,
            _ => ProfileStable
        };

    private static float EaseInOutCubic(float t)
        => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

    private static float EaseOutCubic(float t)
        => 1f - Mathf.Pow(1f - t, 3f);

    private static float EaseInCubic(float t)
        => t * t * t;
}
