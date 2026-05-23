// CutsceneSignalReceiver_Cutscene1Upgrade1.cs
// Characters: Protagonist 1 + Mom/Boss
// Attach to the SignalReceiver GameObject for this cutscene.
//
// HOW THE AUTO-ADVANCE WORKS:
//   Call StartDialogue() from a single Signal Emitter at the start of the panel.
//   The script works through every line automatically — no Timeline timing needed.
//   Each line finishes typing, waits for the read delay, then moves to the next.
//
// SIGNAL RECEIVER SETUP:
//   Signal_Start  ->  StartDialogue
//   Signal_Clear  ->  ClearText  (optional, for manual wipe at cutscene end)
//
// TO EDIT DIALOGUE:
//   Find the dialogueLines list below and change the text inside any Line().
//   Add new Line() entries to add more lines.
//   Remove Line() entries to remove lines.

using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CutsceneSignalReceiver_Cutscene1Upgrade1 : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI dialogueText;

    [Header("Character Fonts")]
    public TMP_FontAsset protagonist1Font;
    public TMP_FontAsset momBossFont;

    [Header("Typewriter Settings")]
    [Tooltip("Characters typed per second at normal speed")]
    [Range(5f, 120f)]
    public float typeSpeed = 30f;

    [Tooltip("Seconds the player has to read each line before the next one starts")]
    [Range(0.5f, 5f)]
    public float readDelay = 2f;

    private Coroutine currentDialogue;

    // =========================================================================
    //  CHARACTER STYLES
    //  Each method sets the text box appearance for that character.
    // =========================================================================

    void StyleProtagonist1()
    {
        dialogueText.color = new Color(0.85f, 0.9f, 1f);
        dialogueText.fontStyle = FontStyles.Italic;
        dialogueText.fontSize = 16;
        if (protagonist1Font != null) dialogueText.font = protagonist1Font;
    }

    void StyleMomBoss()
    {
        dialogueText.color = new Color(0.85f, 0.1f, 0.1f);
        dialogueText.fontStyle = FontStyles.Bold;
        dialogueText.fontSize = 22;
        if (momBossFont != null) dialogueText.font = momBossFont;
    }

    // =========================================================================
    //  DIALOGUE LINES
    //  Each Line() takes three arguments:
    //    1. The character style method (StyleProtagonist1 or StyleMomBoss)
    //    2. The dialogue string — use [slow] [fast] [reset] [pause] tags as needed
    //    3. Read delay override in seconds — use 0 to use the default readDelay
    //
    //  Narrator lines describing the protagonist's experience use StyleProtagonist1.
    // =========================================================================

    List<DialogueLine> GetLines()
    {
        return new List<DialogueLine>
        {
            // Protagonist — terrified, pleading
            new DialogueLine(StyleProtagonist1,
                "[slow]Please let me out[pause] Mom!", 0),

            // Mom/Boss — aggressive, no hesitation
            new DialogueLine(StyleMomBoss,
                "Did you learn your lesson!", 0),

            // Protagonist — breaking down, sobbing
            new DialogueLine(StyleProtagonist1,
                "[slow]I won't hold anyone's hand again![pause][pause] - I began to sob.", 0),

            // Narrator — describing the moment
            new DialogueLine(StyleProtagonist1,
                "[slow]As the door opens[pause] I am yanked out.[pause] I begin kicking and screaming.", 0),

            // Protagonist — desperate
            new DialogueLine(StyleProtagonist1,
                "[slow]What are you punishing me for?", 0),

            // Mom/Boss — cruel, delivered fast and hard
            new DialogueLine(StyleMomBoss,
                "I'm punishing you for being alive!", 0),

            // Narrator — the aftermath. Longer read delay so it lingers.
            new DialogueLine(StyleProtagonist1,
                "[slow]As she hits me[pause] I scream loudly.[pause][pause] I feel my pleas fall on deaf ears[pause]...", 3),
        };
    }

    // =========================================================================
    //  PUBLIC METHODS
    //  Wire these to Signal Assets via the Signal Receiver component.
    // =========================================================================

    public void StartDialogue()
    {
        if (currentDialogue != null) StopCoroutine(currentDialogue);
        currentDialogue = StartCoroutine(RunDialogue());
    }

    public void ClearText()
    {
        if (currentDialogue != null) StopCoroutine(currentDialogue);
        dialogueText.text = "";
    }

    // =========================================================================
    //  DIALOGUE RUNNER
    //  Works through each line automatically, clears between them.
    // =========================================================================

    IEnumerator RunDialogue()
    {
        foreach (DialogueLine line in GetLines())
        {
            line.applyStyle();
            dialogueText.text = "";
            yield return StartCoroutine(TypeRoutine(line.text));
            float delay = line.readDelayOverride > 0 ? line.readDelayOverride : readDelay;
            yield return new WaitForSeconds(delay);
        }
    }

    // =========================================================================
    //  TYPEWRITER
    // =========================================================================

    IEnumerator TypeRoutine(string message)
    {
        dialogueText.text = "";
        float delay = 1f / typeSpeed;
        int i = 0;
        while (i < message.Length)
        {
            if (message.Substring(i).StartsWith("[slow]")) { delay = 1f / (typeSpeed * 0.3f); i += 6; continue; }
            if (message.Substring(i).StartsWith("[fast]")) { delay = 1f / (typeSpeed * 2f); i += 6; continue; }
            if (message.Substring(i).StartsWith("[reset]")) { delay = 1f / typeSpeed; i += 7; continue; }
            if (message.Substring(i).StartsWith("[pause]")) { yield return new WaitForSeconds(1.2f); i += 7; continue; }
            dialogueText.text += message[i];
            i++;
            yield return new WaitForSeconds(delay);
        }
    }

    // =========================================================================
    //  DIALOGUE LINE DATA CLASS
    // =========================================================================

    class DialogueLine
    {
        public System.Action applyStyle;
        public string text;
        public float readDelayOverride;

        public DialogueLine(System.Action style, string dialogue, float delayOverride)
        {
            applyStyle = style;
            text = dialogue;
            readDelayOverride = delayOverride;
        }
    }
}