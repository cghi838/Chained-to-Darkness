using UnityEngine;
using TMPro;

// ============================================================
//  FontManager.cs
//  Assigns fonts based on which character is speaking and
//  what the player's current sanity level is.
//
//  CHARACTER FONTS:
//  MainCharacter  -> Another Danger
//  MomBoss        -> Help Me
//  MomPostGame    -> Roboto               (post game)
//  Father         -> Who Asked Satan
//  TitleScreen    -> Shadows of the Dead
//
//  SANITY FONTS:
//  Max            -> Ghastly Panic        (fully insane)
//  Zero           -> Roboto               (Sane/Post game and moved on)
//
//  HOW TO USE:
//  1. Attach this to a persistent GameObject (e.g. _Managers).
//  2. Drag your TMP Font Assets into each slot in the Inspector.
//  3. Call FontManager.Instance.ApplyCharacterFont(text, character)
//     to set a speaker font.
//  4. Call FontManager.Instance.ApplySanityFont(text, sanity, maxSanity)
//     to set a sanity-driven font on any text element.
// ============================================================
public class FontManager : MonoBehaviour
{
    public static FontManager Instance { get; private set; }

    // -------------------------------------------------------
    //  Character identifiers
    // -------------------------------------------------------
    public enum Character
    {
        MainCharacter,
        MomBoss,
        MomPostGame,
        Father,
        TitleScreen
    }

    // -------------------------------------------------------
    //  Sanity states - only Max and Zero
    // -------------------------------------------------------
    public enum SanityLevel
    {
        Zero,
        Max
    }

    // -------------------------------------------------------
    //  Character Fonts (assign in Inspector)
    // -------------------------------------------------------
    [Header("Character Fonts")]
    [Tooltip("Another Danger")]
    [SerializeField] private TMP_FontAsset mainCharacterFont;
    [Tooltip("Help Me")]
    [SerializeField] private TMP_FontAsset momBossFont;
    [Tooltip("Roboto")]
    [SerializeField] private TMP_FontAsset momPostGameFont;
    [Tooltip("Who Asked Satan")]
    [SerializeField] private TMP_FontAsset fatherFont;
    [Tooltip("Shadows of the Dead")]
    [SerializeField] private TMP_FontAsset titleScreenFont;

    // -------------------------------------------------------
    //  Sanity Fonts (assign in Inspector)
    // -------------------------------------------------------
    [Header("Sanity Fonts")]
    [Tooltip("Ghastly Panic - used when sanity is full")]
    [SerializeField] private TMP_FontAsset sanityMaxFont;
    [Tooltip("Roboto - used when sanity hits zero")]
    [SerializeField] private TMP_FontAsset sanityZeroFont;

    // -------------------------------------------------------
    //  At or below this normalized ratio the font shifts to Zero
    // -------------------------------------------------------
    [Header("Sanity Threshold (0 to 1)")]
    [SerializeField] [Range(0f, 1f)] private float zeroThreshold = 0.05f;

    // -------------------------------------------------------
    //  Character Font API
    // -------------------------------------------------------

    // Apply a character's font to a single text component
    public void ApplyCharacterFont(TextMeshProUGUI textComponent, Character character)
    {
        if (textComponent == null) return;
        TMP_FontAsset font = GetCharacterFont(character);
        if (font != null) textComponent.font = font;
    }

    // Apply a character's font to every TMP text under a panel
    public void ApplyCharacterFontToChildren(GameObject root, Character character)
    {
        if (root == null) return;
        TMP_FontAsset font = GetCharacterFont(character);
        if (font == null) return;
        foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true))
            t.font = font;
    }

    // Returns the TMP Font Asset for a given character
    public TMP_FontAsset GetCharacterFont(Character character)
    {
        switch (character)
        {
            case Character.MainCharacter: return mainCharacterFont;
            case Character.MomBoss: return momBossFont;
            case Character.MomPostGame: return momPostGameFont;
            case Character.Father: return fatherFont;
            case Character.TitleScreen: return titleScreenFont;
            default: return null;
        }
    }

    // -------------------------------------------------------
    //  Sanity Font API
    // -------------------------------------------------------

    // Pass current and max sanity - picks the correct font automatically
    public void ApplySanityFont(TextMeshProUGUI textComponent, float currentSanity, float maxSanity)
    {
        if (textComponent == null) return;
        TMP_FontAsset font = GetSanityFont(currentSanity, maxSanity);
        if (font != null) textComponent.font = font;
    }

    // Apply sanity font to every TMP text under a panel
    public void ApplySanityFontToChildren(GameObject root, float currentSanity, float maxSanity)
    {
        if (root == null) return;
        TMP_FontAsset font = GetSanityFont(currentSanity, maxSanity);
        if (font == null) return;
        foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true))
            t.font = font;
    }

    // Resolves sanity level then returns the matching font
    public TMP_FontAsset GetSanityFont(float currentSanity, float maxSanity)
    {
        return GetSanityFontByLevel(ResolveSanityLevel(currentSanity, maxSanity));
    }

    // Pass a pre-resolved SanityLevel directly
    public TMP_FontAsset GetSanityFontByLevel(SanityLevel level)
    {
        switch (level)
        {
            case SanityLevel.Zero: return sanityZeroFont;
            case SanityLevel.Max: return sanityMaxFont;
            default: return sanityMaxFont;
        }
    }

    // Converts current/max sanity values into a SanityLevel
    public SanityLevel ResolveSanityLevel(float currentSanity, float maxSanity)
    {
        if (maxSanity <= 0f) return SanityLevel.Zero;
        float ratio = currentSanity / maxSanity;
        return (ratio <= zeroThreshold) ? SanityLevel.Zero : SanityLevel.Max;
    }

    // -------------------------------------------------------
    //  Combined API
    //  Uses the character's own font at max sanity.
    //  Switches to the sanity zero font when sanity bottoms out.
    // -------------------------------------------------------
    public void ApplyCombinedFont(TextMeshProUGUI textComponent,
                                   Character character,
                                   float currentSanity,
                                   float maxSanity)
    {
        if (textComponent == null) return;

        if (ResolveSanityLevel(currentSanity, maxSanity) == SanityLevel.Max)
            ApplyCharacterFont(textComponent, character);
        else
            ApplySanityFont(textComponent, currentSanity, maxSanity);
    }
}