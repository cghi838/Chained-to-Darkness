using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;

    // Call this whenever health changes
    public void SetHealth(float current, float max)
    {
        healthSlider.maxValue = max;
        healthSlider.value = current;
    }
}