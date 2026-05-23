// CutsceneSignalReceiver.cs
// Attach to the SignalReceiver GameObject on your Canvas.
//
// INSPECTOR SETUP:
//   - Drag your DialogueText (TextMeshProUGUI) into the Dialogue Text slot
//   - Drag TMP Font Assets into the Player Font, Enemy Font, Narrator Font slots
//     (leave any slot empty if you don't need a separate font for that character)
//   - Adjust Type Speed (characters per second) to taste
//
// SIGNAL RECEIVER COMPONENT:
//   On the same GameObject, add a Signal Receiver component (Add Component ->
//   Playables -> Signal Receiver) and wire each Signal Asset to its method:
//     Signal_Panel1  ->  ShowPanel1
//     Signal_Panel2  ->  ShowPanel2
//     Signal_Panel3  ->  ShowPanel3
//     Signal_Panel4  ->  ShowPanel4
//     Signal_Clear   ->  ClearText
//
// ADDING MORE PANELS:
//   Copy any ShowPanelX method, increment the number, change the style
//   and dialogue string, then add a new Signal Asset wired to it.
//
// DIALOGUE TAGS (embed in any string):
//   [slow]   - drops to 30% speed
//   [fast]   - jumps to 200% speed
//   [reset]  - returns to normal speed
//   [pause]  - 1.2 seconds of silence

using UnityEngine;
using TMPro;
using System.Collections;

public class CutsceneSignalReceiver : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag your DialogueText (TextMeshProUGUI) object here")]
    public TextMeshProUGUI dialogueText;

    [Header("Character Fonts (optional — leave empty to keep a single font)")]
    [Tooltip("Font used for the player character's dialogue")]
    public TMP_FontAsset playerFont;

    [Tooltip("Font used for the antagonist / enemy dialogue")]
    public TMP_FontAsset enemyFont;

    [Tooltip("Font used for narrator text or environmental descriptions")]
    public TMP_FontAsset narratorFont;

    [Header("Typewriter Settings")]
    [Tooltip("How many characters are typed per second at normal speed")]
    [Range(5f, 120f)]
    public float typeSpeed = 30f;

    // Tracks the currently running typewriter coroutine so it can be
    // stopped cleanly before starting a new one
    private Coroutine currentTyping;

    public void ShowPanel1()
    {
        // Narrator — white, normal weight, medium size
        SetTextStyle(Color.white, FontStyles.Normal, 28, narratorFont);
        TypeText("Something is wrong[pause] with this place.");
    }

    public void ShowPanel2()
    {
        // Player — pale blue, italic, slightly smaller, hesitant pace
        SetTextStyle(new Color(0.7f, 0.85f, 1f), FontStyles.Italic, 26, playerFont);
        TypeText("[slow]I shouldn't have come back here.");
    }

    public void ShowPanel3()
    {
        // Antagonist — deep red, bold, larger, slow and deliberate
        SetTextStyle(new Color(0.8f, 0.15f, 0.15f), FontStyles.Bold, 34, enemyFont);
        TypeText("[slow]You[pause] never[pause] left.");
    }

    public void ShowPanel4()
    {
        // Narrator again — same style as panel 1, mixed pacing
        SetTextStyle(Color.white, FontStyles.Normal, 28, narratorFont);
        TypeText("[fast]I need to leave I need to leave I need to[reset][slow] leave.");
    }

    // Called by Signal_Clear — wipes the text box between panels.
    // Place a Signal_Clear emitter half a second before each panel switch
    // so the old text clears cleanly before the new image appears.
    public void ClearText()
    {
        if (currentTyping != null) StopCoroutine(currentTyping);
        dialogueText.text = "";
    }

    // =========================================================================
    //  STYLE HELPER
    //  Sets all visual properties on the text box before typing begins.
    //  Because this runs before any characters appear, the player never
    //  sees the style switch — each panel just looks like its own style.
    // =========================================================================

    void SetTextStyle(Color color, FontStyles style, float size, TMP_FontAsset font)
    {
        dialogueText.color = color;
        dialogueText.fontStyle = style;
        dialogueText.fontSize = size;

        // Only swap the font if a non-null asset was passed in.
        // Passing null keeps whatever font is currently assigned.
        if (font != null)
            dialogueText.font = font;
    }

    // =========================================================================
    //  TYPEWRITER CORE
    //  TypeText() is the public entry point — always call this, never call
    //  TypeRoutine() directly. TypeText() stops any in-progress typing first
    //  so you never get two coroutines writing to the same text box.
    // =========================================================================

    void TypeText(string message)
    {
        if (currentTyping != null) StopCoroutine(currentTyping);
        currentTyping = StartCoroutine(TypeRoutine(message));
    }

    IEnumerator TypeRoutine(string message)
    {
        dialogueText.text = "";
        float delay = 1f / typeSpeed;
        int i = 0;

        while (i < message.Length)
        {
            // [slow] tag — crawl to 30% of normal speed
            if (message.Substring(i).StartsWith("[slow]"))
            {
                delay = 1f / (typeSpeed * 0.3f);
                i += 6;
                continue;
            }

            // [fast] tag — rush to 200% of normal speed
            if (message.Substring(i).StartsWith("[fast]"))
            {
                delay = 1f / (typeSpeed * 2f);
                i += 6;
                continue;
            }

            // [reset] tag — return to normal speed
            if (message.Substring(i).StartsWith("[reset]"))
            {
                delay = 1f / typeSpeed;
                i += 7;
                continue;
            }

            // [pause] tag — 1.2 seconds of complete silence
            if (message.Substring(i).StartsWith("[pause]"))
            {
                yield return new WaitForSeconds(1.2f);
                i += 7;
                continue;
            }

            // Normal character — type it at the current speed
            dialogueText.text += message[i];
            i++;
            yield return new WaitForSeconds(delay);
        }
    }

}