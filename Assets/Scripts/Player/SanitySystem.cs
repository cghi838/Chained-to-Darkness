using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class SanitySystem : MonoBehaviour
{
    public enum SanityStage
    {
        Stable,     // 70+
        Unstable,   // 40~69
        Critical,   // 10~39
        Broken      // <= 0
    }

    [Header("Popup Text")]
    [SerializeField] private PlayerPopupText popupText;

    [Header("Rates")]
    public float drainRate = 3f;
    //    public float healRate = 3f;
    public bool traumaItemEquipped = true;

    [Header("Restore Sanity")]
    [SerializeField] private float restoreDelayTime = 3f;

    public event Action<float, float> OnSanityChanged; // current, max
    public event Action<SanityStage> OnSanityStageChanged;

    public UnityEvent OnSanityUnstable; // 40~69
    public UnityEvent OnSanityCritical; // 10~39
    public UnityEvent OnSanityBroken;   // <= 0
    public UnityEvent OnSanityRestored; // >= 70 restore

    private float baseDrainRate;
    private bool wasUnstable = false;
    private bool wasCritical = false;
    private bool wasBroken = false;
    private SanityStage currentStage = SanityStage.Stable;
    private GameStateData state => GameManager.Instance?.State;

    private bool coroutineStarted = false;

    private GlitchEffect glitchEffect;

    void Awake()
    {
        baseDrainRate = drainRate;
    }

    void Start()
    {
        if (state == null) return;

        currentStage = EvaluateStage(state.currentSanity);
        OnSanityChanged?.Invoke(state.currentSanity, state.maxSanity);
        OnSanityStageChanged?.Invoke(currentStage);

        wasUnstable = state.currentSanity < 70f && state.currentSanity >= 40f;
        wasCritical = state.currentSanity < 40f && state.currentSanity > 0f;
        wasBroken = state.currentSanity <= 0f;

        // glitch effect
        glitchEffect = FindFirstObjectByType<GlitchEffect>();
        if (traumaItemEquipped)
            glitchEffect?.StartCoroutine(glitchEffect.ShowGlitchBriefly());
    }

    void Update()
    {
        if (state == null) return;

        float prev = state.currentSanity;

        // if trauma is equipped, drain sanity a little faster; if not equipped, drain slower
        state.currentSanity += (drainRate * Time.deltaTime);
        state.ClampValues();

        if (!Mathf.Approximately(prev, state.currentSanity))
            OnSanityChanged?.Invoke(state.currentSanity, state.maxSanity);

        if (!Mathf.Approximately(prev, state.currentSanity))
        {
            OnSanityChanged?.Invoke(state.currentSanity, state.maxSanity);
            CheckThresholdsAndStage();
        }
    }

    private void CheckThresholdsAndStage()
    {
        if (state == null) return;

        float sanity = state.currentSanity;

        bool isUnstable = sanity < 70f && sanity >= 40f;
        bool isCritical = sanity < 40f && sanity > 0f;
        bool isBroken = sanity <= 0f;
        // Debug.Log($"sanity = {sanity}, isBroken = {isBroken}, wasBroken = {wasBroken}");
        if (isBroken && !wasBroken)
        {
            if (popupText != null && coroutineStarted == false)
            {
                Debug.Log("Sanity just broke! Triggering OnSanityBroken event.");
                popupText.ShowPopup("Your sanity is broken! Take a break for a few seconds.");
                StartCoroutine(WaitForSeconds(3)); // wait for enough time to show the text
            }
            OnSanityBroken?.Invoke();
        }

        if (isCritical && !wasCritical)
            OnSanityCritical?.Invoke();
        else if (isUnstable && !wasUnstable)
            OnSanityUnstable?.Invoke();
        else if (sanity >= 70f && (wasUnstable || wasCritical || wasBroken))
            OnSanityRestored?.Invoke();

        wasUnstable = isUnstable;
        wasCritical = isCritical;
        wasBroken = isBroken;

        SanityStage newStage = EvaluateStage(sanity);
        if (newStage != currentStage)
        {
            currentStage = newStage;
            OnSanityStageChanged?.Invoke(currentStage);
        }
    }

    private SanityStage EvaluateStage(float sanity)
    {
        if (sanity >= 70f) return SanityStage.Stable;
        if (sanity >= 40f) return SanityStage.Unstable;
        if (sanity >= 10f) return SanityStage.Critical;
        if (sanity > 0f) return SanityStage.Critical;
        return SanityStage.Broken;
    }

    public SanityStage GetCurrentStage() => currentStage;

    // glitch effect added to trauma item eqipped
    public void SetTraumaItemEquipped(bool val)
    {
        traumaItemEquipped = val;
        glitchEffect?.StartCoroutine(glitchEffect.ShowGlitchBriefly());
    }

    // SetDrainModifier modifies drainRate based on original baseDrainRate
    public void SetDrainModifier(float modifier)
    {
        drainRate = baseDrainRate * modifier;
    }

    // Called by MemoryFragmentPickup
    public void IncreaseSanity(float amount) => RestoreSanity(amount);
    public void DecreaseSanity(float amount) => TakeDamage(amount);

    // Called by CheckpointController
    public void SetSanityDirect(float value)
    {
        if (state == null) return;
        state.currentSanity = Mathf.Clamp(value, 0f, state.maxSanity);
        OnSanityChanged?.Invoke(state.currentSanity, state.maxSanity);
        CheckThresholdsAndStage();
    }

    public void TakeDamage(float amount)
    {
        if (state == null) return;
        state.currentSanity = Mathf.Clamp(state.currentSanity - amount, 0f, state.maxSanity);
        OnSanityChanged?.Invoke(state.currentSanity, state.maxSanity);
        CheckThresholdsAndStage();
    }

    public void RestoreSanity(float amount)
    {
        if (state == null) return;
        state.currentSanity = Mathf.Clamp(state.currentSanity + amount, 0f, state.maxSanity);
        OnSanityChanged?.Invoke(state.currentSanity, state.maxSanity);
        CheckThresholdsAndStage();
    }

    public float GetRatio() => state != null ? state.currentSanity / state.maxSanity : 1f;

    private IEnumerator WaitForSeconds(int seconds)
    {
        coroutineStarted = true;
        yield return new WaitForSecondsRealtime(seconds);
        coroutineStarted = false;
    }

    public float GetRestoreDelayTime() => restoreDelayTime;
}