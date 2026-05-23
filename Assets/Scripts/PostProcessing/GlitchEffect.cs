using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Modified from https://github.com/staffantan/unityglitch (Built-in) to URP
[ExecuteInEditMode]
public class GlitchEffect : MonoBehaviour
{
    public ScriptableRendererFeature glitchRendererFeature; // set from inspector
    public Material glitchMaterial; // glitch shader material


    [Header("Glitch Intensity")]
    [Range(0, 1)] public float intensity;
    [Range(0, 1)] public float colorIntensity;

    private float flicker;
    private float _flickerTime = 0.5f;

    void Start()
    {
        if (glitchMaterial == null) return;
        glitchMaterial.SetFloat("displace", 0);
        glitchMaterial.SetFloat("scale", 1);
        glitchMaterial.SetFloat("filterRadius", 0);
        glitchRendererFeature?.SetActive(false); // disable Renderer Feature
    }

    void OnEnable()
    {
        if (glitchMaterial == null) return;
        glitchMaterial.SetFloat("displace", 0);
        glitchMaterial.SetFloat("scale", 1);
        glitchMaterial.SetFloat("filterRadius", 0);
        glitchRendererFeature?.SetActive(false); // disable Renderer Feature
    }

    void Update()
    {
        if (glitchMaterial == null) return;

        // Set Shader Properties
        glitchMaterial.SetFloat("_Intensity", intensity);
        glitchMaterial.SetFloat("_ColorIntensity", colorIntensity);

        // Color Bleed (Chromatic Aberration) Logic
        flicker += Time.deltaTime * colorIntensity;
        if (flicker > _flickerTime)
        {
            glitchMaterial.SetFloat("filterRadius", Random.Range(-3f, 3f) * colorIntensity);
            glitchMaterial.SetVector("direction", Quaternion.AngleAxis(Random.Range(0, 360) * colorIntensity, Vector3.forward) * Vector4.one);
            flicker = 0;
            _flickerTime = Random.value;
        }

        if (colorIntensity == 0) glitchMaterial.SetFloat("filterRadius", 0);

        // Displacement (Glitch Distort) Logic
        if (Random.value < 0.05 * intensity)
        {
            glitchMaterial.SetFloat("displace", Random.value * intensity);
            glitchMaterial.SetFloat("scale", 1 - Random.value * intensity);
        }
        else
        {
            glitchMaterial.SetFloat("displace", 0);
            glitchMaterial.SetFloat("scale", 1); // when scale=0, black screen
        }
    }

    // GlitchEffect ON
    public void EnableGlitch(float intensityValue = 0.8f)
    {
        glitchRendererFeature.SetActive(true);
        intensity = intensityValue;
        colorIntensity = 0.15f;
    }

    // GlitchEffect OFF
    public void DisableGlitch()
    {
        glitchRendererFeature.SetActive(false);
        intensity = 0;
        colorIntensity = 0;
        glitchMaterial.SetFloat("scale", 1);
        glitchMaterial.SetFloat("displace", 0);
        glitchMaterial.SetFloat("flip_up", 0);
        glitchMaterial.SetFloat("filterRadius", 0);
    }

    // Automatically turns off after a 2-second glitch
    public IEnumerator ShowGlitchBriefly(float intensityValue = 0.8f, float duration = 2f)
    {
        EnableGlitch(intensityValue);
        yield return new WaitForSeconds(duration);
        DisableGlitch();
    }
}