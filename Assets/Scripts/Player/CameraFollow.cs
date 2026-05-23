using System.Collections;
using UnityEngine;

// CameraFollow — 2D platformer camera that looks ahead in the player's movement direction.
// Attach to MainCamera. MainCamera must NOT be a child of Player.
//
// Added systems (all additive — original logic is untouched):
//   • Camera Shake        — trauma-based Perlin noise shake, call AddTrauma(0..1)
//   • Area Transitions    — smooth blend when entering a CameraZone trigger
//   • Mental State        — distortion behaviour driven by MentalStateLevel enum
//   • Cinematic Sequences — coroutine dolly shots with letterbox bars
//   • Vignette            — optional CanvasGroup overlay driven by mental state
//
// Place CameraZone triggers in the scene to define per-area bounds, zoom, and mental state.

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    // -------------------------------------------------------------------------
    //  ORIGINAL FIELDS — untouched
    // -------------------------------------------------------------------------

    [Header("Target")]
    public Transform target;

    [Header("Follow Settings")]
    public float smoothSpeed = 8f;
    public float offsetY = 1f;   // vertical offset above player

    [Header("Look Ahead")]
    public float lookAheadX = 3f;   // how far ahead to look in movement direction
    public float lookAheadSpeed = 4f;  // how fast the lookahead shifts

    [Header("Bounds (set to level width)")]
    public bool useBounds = false;
    public float minX, maxX;
    public float minY, maxY;

    private float currentLookAhead = 0f;
    private float facingDirection = 1f;  // 1 = right, -1 = left

    // -------------------------------------------------------------------------
    //  NEW FIELDS
    // -------------------------------------------------------------------------

    [Header("Camera Shake")]
    public float traumaDecay = 1.2f;  // How fast trauma fades per second
    public float maxShakeTranslate = 0.4f;  // Max positional offset (world units)
    public float maxShakeAngle = 2f;    // Max roll in degrees
    public float shakeFrequency = 25f;   // Perlin noise speed

    [Header("Mental State")]
    public MentalStateLevel currentMentalState = MentalStateLevel.Stable;
    [Range(0f, 1f)]
    public float mentalStateIntensity = 0f;

    [Header("Cinematic Bars (optional)")]
    [Tooltip("Black UI panel anchored to top of screen.")]
    public RectTransform letterboxTop;
    [Tooltip("Black UI panel anchored to bottom of screen.")]
    public RectTransform letterboxBottom;
    public float letterboxHeight = 60f;
    public float letterboxSpeed = 3f;

    [Header("Vignette (optional)")]
    [Tooltip("Full-screen CanvasGroup image, black, driven by mental state.")]
    public CanvasGroup vignetteOverlay;

    // Enum must be public so CameraZone can reference it
    public enum MentalStateLevel { Stable, Uneasy, Distressed, Broken }

    // --- Private: shake ---
    private float _trauma;
    private float _shakeTime;

    // --- Private: bounds override (set by CameraZone, falls back to inspector values) ---
    private bool _boundsOverridden;
    private float _zoneMinX, _zoneMaxX, _zoneMinY, _zoneMaxY;

    // --- Private: zoom ---
    private bool _overrideZoom;
    private float _targetOrthoSize;
    private float _defaultOrthoSize;

    // --- Private: transition ---
    private bool _isTransitioning;
    private float _transitionBlend = 5f;  // lerp speed during transition

    // --- Private: cinematic ---
    private bool _inCinematic;
    private bool _cinematicBarsOpen;

    // --- Private: mental state / vignette ---
    private float _mentalNoise;
    private float _vignetteTarget;
    private float _currentVignette;

    private Camera _cam;

    // =========================================================================
    //  Awake
    // =========================================================================

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        _defaultOrthoSize = _cam.orthographicSize;
    }

    // =========================================================================
    //  LateUpdate — original structure preserved, new systems bolted on after
    // =========================================================================

    private void LateUpdate()
    {
        if (target == null)
        {
            // Debug.Log("CameraFollow: No target set for camera to follow.");
            return;
        }

        // Cinematic coroutine drives position directly — skip normal follow
        if (_inCinematic)
        {
            UpdateLetterbox(_cinematicBarsOpen);
            UpdateVignette();
            return;
        }

        // ---- ORIGINAL: Detect facing direction from PlayerMovementPlatformer ----
        var movement = target.GetComponent<PlayerMovementPlatformer>();
        if (movement != null)
        {
            var sprite = target.GetComponent<SpriteRenderer>();
            if (sprite != null)
                facingDirection = sprite.flipX ? -1f : 1f;
        }

        // ---- ORIGINAL: Smoothly shift lookahead toward facing direction ----
        float targetLookAhead = facingDirection * lookAheadX;
        currentLookAhead = Mathf.Lerp(currentLookAhead, targetLookAhead, lookAheadSpeed * Time.deltaTime);

        Vector3 desired = new Vector3(
            target.position.x + currentLookAhead,
            target.position.y + offsetY,
            transform.position.z
        );

        // ---- ORIGINAL: Framerate-independent smooth follow ----
        float t = 1f - Mathf.Pow(0.01f, smoothSpeed * Time.deltaTime);
        Vector3 smoothed = Vector3.Lerp(transform.position, desired, t);

        // ---- ORIGINAL: Bounds clamping ----
        // NEW: if a CameraZone has overridden bounds, use those instead of inspector values
        bool applyBounds = useBounds || _boundsOverridden;
        float bMinX = _boundsOverridden ? _zoneMinX : minX;
        float bMaxX = _boundsOverridden ? _zoneMaxX : maxX;
        float bMinY = _boundsOverridden ? _zoneMinY : minY;
        float bMaxY = _boundsOverridden ? _zoneMaxY : maxY;

        if (applyBounds)
        {
            smoothed.x = Mathf.Clamp(smoothed.x, bMinX, bMaxX);
            smoothed.y = Mathf.Clamp(smoothed.y, bMinY, bMaxY);
        }

        // ---- NEW: Area-transition soft blend (extra smoothing on zone entry) ----
        if (_isTransitioning)
            smoothed = Vector3.Lerp(transform.position, smoothed, _transitionBlend * Time.deltaTime);

        // ---- NEW: Mental-state distortion (additive on top of smoothed pos) ----
        smoothed = ApplyMentalDistortion(smoothed);

        // ---- NEW: Camera shake offset ----
        smoothed += ComputeShake();

        // ---- Apply final position ----
        transform.position = smoothed;

        // ---- NEW: Orthographic zoom (zone override or default) ----
        float targetZoom = _overrideZoom ? _targetOrthoSize : _defaultOrthoSize;
        _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, targetZoom, 4f * Time.deltaTime);

        // ---- NEW: Decay trauma each frame ----
        _trauma = Mathf.Max(0f, _trauma - traumaDecay * Time.deltaTime);
        _shakeTime += Time.deltaTime;
        _mentalNoise += Time.deltaTime;

        // ---- NEW: Vignette & letterbox ----
        UpdateVignette();
        UpdateLetterbox(_cinematicBarsOpen);
    }

    // =========================================================================
    //  Camera Shake  — trauma-based, Perlin noise
    // =========================================================================

    /// <summary>Add trauma [0..1]. Stacks; decays automatically each frame.</summary>
    public void AddTrauma(float amount) => _trauma = Mathf.Clamp01(_trauma + amount);

    public void ShakeLight() => AddTrauma(0.2f);
    public void ShakeMedium() => AddTrauma(0.45f);
    public void ShakeHeavy() => AddTrauma(0.75f);
    public void ShakeExtreme() => AddTrauma(1.0f);

    private Vector3 ComputeShake()
    {
        if (_trauma <= 0f)
        {
            // Smoothly reset roll when no shake is active
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, 10f * Time.deltaTime);
            return Vector3.zero;
        }

        float shake = _trauma * _trauma;  // Squared — natural falloff feel

        float ox = maxShakeTranslate * shake *
                      (Mathf.PerlinNoise(_shakeTime * shakeFrequency, 0f) * 2f - 1f);
        float oy = maxShakeTranslate * shake *
                      (Mathf.PerlinNoise(0f, _shakeTime * shakeFrequency) * 2f - 1f);
        float angle = maxShakeAngle * shake *
                      (Mathf.PerlinNoise(_shakeTime * shakeFrequency,
                                         _shakeTime * shakeFrequency) * 2f - 1f);

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        return new Vector3(ox, oy, 0f);
    }

    // =========================================================================
    //  Bounds — zone override API  (called by CameraZone)
    // =========================================================================

    /// <summary>Override bounds from a CameraZone. Triggers a smooth transition blend.</summary>
    public void SetBounds(float bMinX, float bMaxX, float bMinY, float bMaxY,
                          float transitionDuration = 0.8f)
    {
        _zoneMinX = bMinX; _zoneMaxX = bMaxX;
        _zoneMinY = bMinY; _zoneMaxY = bMaxY;
        _boundsOverridden = true;
        BeginTransition(transitionDuration);
    }

    /// <summary>Remove zone bounds override — reverts to inspector useBounds values.</summary>
    public void ClearBoundsOverride() => _boundsOverridden = false;

    // =========================================================================
    //  Area Transitions
    // =========================================================================

    public void BeginTransition(float duration = 0.8f)
    {
        _transitionBlend = 1f / Mathf.Max(0.01f, duration) * smoothSpeed;
        _isTransitioning = true;
        StopCoroutine(nameof(EndTransitionDelayed));
        StartCoroutine(EndTransitionDelayed(duration * 1.5f));
    }

    private IEnumerator EndTransitionDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        _isTransitioning = false;
    }

    // =========================================================================
    //  Zoom
    // =========================================================================

    public void SetZoom(float orthoSize)
    {
        _targetOrthoSize = orthoSize;
        _overrideZoom = true;
    }

    public void ResetZoom() => _overrideZoom = false;

    // =========================================================================
    //  Mental State
    // =========================================================================

    /// <summary>Set the camera's mental state. intensity 0..1 scales all effects.</summary>
    public void SetMentalState(MentalStateLevel state, float intensity)
    {
        currentMentalState = state;
        mentalStateIntensity = Mathf.Clamp01(intensity);
    }

    private Vector3 ApplyMentalDistortion(Vector3 pos)
    {
        if (mentalStateIntensity <= 0f) return pos;

        switch (currentMentalState)
        {
            // Subtle drift — slightly unsettled
            case MentalStateLevel.Uneasy:
                {
                    pos.y += Mathf.Sin(_mentalNoise * 0.8f) * 0.08f * mentalStateIntensity;
                    _vignetteTarget = 0.15f * mentalStateIntensity;
                    break;
                }

            // Erratic jitter + tilt
            case MentalStateLevel.Distressed:
                {
                    pos.x += (Mathf.PerlinNoise(_mentalNoise * 3f, 0f) - 0.5f) * 0.25f * mentalStateIntensity;
                    pos.y += (Mathf.PerlinNoise(0f, _mentalNoise * 3f) - 0.5f) * 0.15f * mentalStateIntensity;

                    float tilt = Mathf.Sin(_mentalNoise * 1.5f) * 3f * mentalStateIntensity;
                    transform.rotation = Quaternion.Euler(0f, 0f, tilt);

                    _vignetteTarget = 0.35f * mentalStateIntensity;
                    break;
                }

            // Violent chaos, heavy tilt, zoom pulse
            case MentalStateLevel.Broken:
                {
                    float chaos = mentalStateIntensity;
                    pos.x += (Mathf.PerlinNoise(_mentalNoise * 7f, 42f) - 0.5f) * 0.6f * chaos;
                    pos.y += (Mathf.PerlinNoise(99f, _mentalNoise * 7f) - 0.5f) * 0.4f * chaos;

                    float tilt = (Mathf.PerlinNoise(_mentalNoise * 2f, 0f) - 0.5f) * 10f * chaos;
                    transform.rotation = Quaternion.Euler(0f, 0f, tilt);

                    _cam.orthographicSize = _defaultOrthoSize +
                                            Mathf.Sin(_mentalNoise * 4f) * 0.4f * chaos;

                    _vignetteTarget = 0.6f * chaos;
                    break;
                }

            default: // Stable
                _vignetteTarget = 0f;
                transform.rotation = Quaternion.Lerp(transform.rotation,
                                                     Quaternion.identity, 5f * Time.deltaTime);
                break;
        }

        return pos;
    }

    // =========================================================================
    //  Vignette
    // =========================================================================

    private void UpdateVignette()
    {
        if (vignetteOverlay == null) return;
        _currentVignette = Mathf.Lerp(_currentVignette, _vignetteTarget, 2f * Time.deltaTime);
        vignetteOverlay.alpha = _currentVignette;
    }

    // =========================================================================
    //  Cinematic Sequences
    // =========================================================================


    /// Move camera to a world point, hold, then return to player follow.
    /// Letterbox bars open automatically.

    public void PlayCinematicShot(Vector3 worldTarget,
                                   float moveDuration = 1.5f,
                                   float holdDuration = 2f,
                                   float returnDuration = 1f,
                                   float shotOrthoSize = 0f)
    {
        StopCinematics();
        StartCoroutine(CinematicShotRoutine(
            worldTarget, moveDuration, holdDuration, returnDuration,
            shotOrthoSize > 0f ? shotOrthoSize : _defaultOrthoSize));
    }


    /// Pan through a series of world points then return to player follow
    public void PlayDollySequence(Vector3[] points,
                                   float moveSpeed = 3f,
                                   float pausePerPoint = 0.5f,
                                   float returnDuration = 1.2f)
    {
        StopCinematics();
        StartCoroutine(DollySequenceRoutine(points, moveSpeed, pausePerPoint, returnDuration));
    }

    private IEnumerator CinematicShotRoutine(Vector3 dest, float moveDur,
                                              float holdDur, float returnDur, float shotZoom)
    {
        _inCinematic = true;
        OpenLetterbox();

        Vector3 startPos = transform.position;
        float startZoom = _cam.orthographicSize;
        dest.z = transform.position.z;

        yield return LerpCameraTo(startPos, dest, startZoom, shotZoom, moveDur);
        yield return new WaitForSeconds(holdDur);
        yield return LerpCameraToFollow(shotZoom, _defaultOrthoSize, returnDur);

        CloseLetterbox();
        _inCinematic = false;
    }

    private IEnumerator DollySequenceRoutine(Vector3[] points, float moveSpeed,
                                              float pausePerPoint, float returnDuration)
    {
        _inCinematic = true;
        OpenLetterbox();

        Vector3 currentPos = transform.position;

        foreach (var point in points)
        {
            Vector3 dest = new Vector3(point.x, point.y, transform.position.z);
            float dur = Vector3.Distance(currentPos, dest) / Mathf.Max(0.1f, moveSpeed);

            yield return LerpCameraTo(currentPos, dest, _cam.orthographicSize, _cam.orthographicSize, dur);
            yield return new WaitForSeconds(pausePerPoint);

            currentPos = dest;
        }

        yield return LerpCameraToFollow(_cam.orthographicSize, _defaultOrthoSize, returnDuration);

        CloseLetterbox();
        _inCinematic = false;
    }

    private IEnumerator LerpCameraTo(Vector3 from, Vector3 to,
                                      float zoomFrom, float zoomTo, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(from, to, t);
            _cam.orthographicSize = Mathf.Lerp(zoomFrom, zoomTo, t);
            yield return null;
        }
        transform.position = to;
        _cam.orthographicSize = zoomTo;
    }

    private IEnumerator LerpCameraToFollow(float zoomFrom, float zoomTo, float duration)
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            Vector3 playerPos = (target != null)
                ? new Vector3(target.position.x, target.position.y + offsetY, transform.position.z)
                : transform.position;

            transform.position = Vector3.Lerp(startPos, playerPos, t);
            _cam.orthographicSize = Mathf.Lerp(zoomFrom, zoomTo, t);
            yield return null;
        }
    }

    private void StopCinematics()
    {
        StopAllCoroutines();
        CloseLetterbox();
        _inCinematic = false;
    }

    // =========================================================================
    //  Letterbox
    // =========================================================================

    private void OpenLetterbox() => _cinematicBarsOpen = true;
    private void CloseLetterbox() => _cinematicBarsOpen = false;

    private void UpdateLetterbox(bool open)
    {
        if (letterboxTop == null || letterboxBottom == null) return;

        float targetH = open ? letterboxHeight : 0f;

        Vector2 topSize = letterboxTop.sizeDelta;
        Vector2 botSize = letterboxBottom.sizeDelta;

        topSize.y = Mathf.Lerp(topSize.y, targetH, letterboxSpeed * Time.deltaTime);
        botSize.y = Mathf.Lerp(botSize.y, targetH, letterboxSpeed * Time.deltaTime);

        letterboxTop.sizeDelta = topSize;
        letterboxBottom.sizeDelta = botSize;
    }

    // =========================================================================
    //  Zone API  — called by CameraZone
    // =========================================================================

    public void OnEnterZone(CameraZone zone)
    {
        if (zone.overrideBounds)
            SetBounds(zone.minX, zone.maxX, zone.minY, zone.maxY, zone.transitionDuration);

        if (zone.overrideZoom)
            SetZoom(zone.targetOrthoSize);
        else
            ResetZoom();

        if (zone.overrideMentalState)
            SetMentalState(zone.mentalState, zone.mentalStateIntensity);
    }

    public void OnExitZone(CameraZone zone)
    {
        if (zone.clearBoundsOnExit) ClearBoundsOverride();
        if (zone.resetZoomOnExit) ResetZoom();
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
    }
}