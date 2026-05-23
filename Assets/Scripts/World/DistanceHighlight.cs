using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DistanceHighlight : MonoBehaviour
{
    public Transform player;

    [Header("Distance")]
    public float maxDistance = 5f;

    [Header("Strength")]
    public float outlineMax = 0.5f;
    public float glowMax = 0.8f;

    [Header("Pulse")]
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.3f;

    Material mat;

    void Start()
    {
        mat = GetComponent<SpriteRenderer>().material;
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(player.position, transform.position);

        // when close, then 1 & when far, then 0
        float t = Mathf.Clamp01(1f - dist / maxDistance);

        mat.SetFloat("_OutlineStrength", outlineMax * t);
        mat.SetFloat("_GlowStrength", glowMax * t);
        mat.SetFloat("_PulseSpeed", pulseSpeed * t);
        mat.SetFloat("_PulseAmount", pulseAmount * t);
    }
}