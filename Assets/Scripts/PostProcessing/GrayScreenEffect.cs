using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrayScreenEffect : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Volume globalVolume;

    [Header("Gray Effect")]
    [SerializeField] private float graySaturation = -100f;   // gray
    [SerializeField] private float holdTime = 0.15f;         // hold time
    [SerializeField] private float recoverTime = 0.5f;       // recover time (to 0)

    private ColorAdjustments colorAdjustments;
    private Coroutine effectCoroutine;

    private void Awake()
    {
        if (globalVolume == null)
        {
            Debug.LogError("[GrayScreenEffect] Global Volume reference is missing.");
            enabled = false;
            return;
        }

        if (!globalVolume.profile.TryGet(out colorAdjustments))
        {
            Debug.LogError("[GrayScreenEffect] Color Adjustments override not found in Volume Profile.");
            enabled = false;
            return;
        }

        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.saturation.value = 0f;
    }

    // Immediately lower it to black and white, hold it for a moment, and then return to 0.
    public void PlayGrayFlash()
    {
        if (effectCoroutine != null)
            StopCoroutine(effectCoroutine);

        effectCoroutine = StartCoroutine(CoGrayFlash());
    }

    // Specify the intensity/time externally
    public void PlayGrayFlash(float targetSaturation, float hold, float recover)
    {
        if (effectCoroutine != null)
            StopCoroutine(effectCoroutine);

        effectCoroutine = StartCoroutine(CoGrayFlash(targetSaturation, hold, recover));
    }

    private IEnumerator CoGrayFlash()
    {
        yield return CoGrayFlash(graySaturation, holdTime, recoverTime);
    }

    private IEnumerator CoGrayFlash(float targetSaturation, float hold, float recover)
    {
        // Apply black and white immediately
        colorAdjustments.saturation.value = targetSaturation;

        // hold
        if (hold > 0f)
            yield return new WaitForSeconds(hold);

        // recover to 0
        float start = colorAdjustments.saturation.value;
        float t = 0f;

        if (recover <= 0f)
        {
            colorAdjustments.saturation.value = 0f;
            effectCoroutine = null;
            yield break;
        }

        while (t < recover)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / recover);
            colorAdjustments.saturation.value = Mathf.Lerp(start, 0f, k);
            yield return null;
        }

        colorAdjustments.saturation.value = 0f;
        effectCoroutine = null;
    }
}
