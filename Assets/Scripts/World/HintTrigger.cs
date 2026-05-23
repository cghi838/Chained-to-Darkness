using UnityEngine;

// HintTrigger — Shows a hint message when player enters the trigger zone.

[RequireComponent(typeof(Collider2D))]
public class HintTrigger : MonoBehaviour
{
    [Header("Detection")]
    public LayerMask playerLayer;

    [Header("Hint")]
    [TextArea(1, 3)]
    public string hintMessage = "Press E to drop the chain";
    public float displayDuration = 3f;

    [Header("Settings")]
    [Tooltip("If true, only shows once. If false, shows every time player enters.")]
    public bool showOnce = false;

    private bool shown = false;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (showOnce && shown) return;
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;

        // when use UIManager popup
        //shown = true;
        //UIManager.Instance?.ShowPopup(hintMessage, displayDuration);
        var popup = other.GetComponent<PlayerPopupText>();
        if (popup == null) return;

        shown = true;
        popup.ShowPopup(hintMessage, displayDuration);
    }
}
