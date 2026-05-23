using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Main 2D platformer controller.
/// 
/// Responsibilities:
/// - Horizontal movement
/// - Air control
/// - Jumping
/// - Coyote time
/// - Jump buffering
/// - Ground detection
/// - Head collision detection
/// - Basic slope handling
/// - Moving platform attachment
/// - One-way platform drop-through
/// - Knockback
/// 
/// Setup:
/// - Attach to the player
/// - Requires Rigidbody2D + Collider2D
/// - Ground and one-way platforms should be on included layers
/// - One-way platforms should usually use PlatformEffector2D
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerPlatformerController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody2D body;
    public Collider2D playerCollider;
    public SpriteRenderer spriteRenderer;

    [Header("Movement")]
    public float moveSpeed = 8f;
    public float acceleration = 80f;
    public float groundDeceleration = 90f;
    public float airAcceleration = 45f;
    public float maxFallSpeed = 20f;

    [Header("Jump")]
    public float jumpPower = 14f;
    public int maxJumpCount = 2;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;
    public float jumpCutMultiplier = 0.5f;

    [Header("Collision Checks")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.18f;
    public Transform headCheck;
    public float headCheckRadius = 0.18f;
    public LayerMask groundMask;
    public LayerMask oneWayMask;

    [Header("Slope Handling")]
    [Tooltip("Maximum walkable slope angle in degrees.")]
    public float maxSlopeAngle = 45f;
    public float slopeCastDistance = 0.25f;

    [Header("One-Way Platform")]
    [Tooltip("Seconds to ignore one-way platforms when dropping down.")]
    public float dropThroughDuration = 0.2f;

    [Header("Knockback")]
    public float knockbackControlLockTime = 0.15f;

    [Header("Debug")]
    public bool isGrounded;
    public bool hitHead;
    public bool isOnOneWayPlatform;
    public bool isKnockedBack;
    public Vector2 moveInput;

    private int jumpCount;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool jumpHeld;
    private bool wasGroundedLastFrame;

    private Transform currentMovingPlatform;
    private Vector3 lastPlatformPosition;

    private Coroutine dropThroughRoutine;
    private Coroutine knockbackRoutine;

    private void Reset()
    {
        body = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (playerCollider == null) playerCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        ReadInput();
        UpdateTimers();
        HandleVariableJump();
        UpdateSpriteFacing();
    }

    private void FixedUpdate()
    {
        CheckGrounded();
        CheckHead();
        UpdateGroundState();
        ApplyMovingPlatformDelta();
        HandleHorizontalMovement();
        HandleJump();
        HandleHeadCollision();
        ClampFallSpeed();
    }

    /// Reads keyboard/controller input from Input System actions.
    /// Required actions:
    /// - Move (Vector2)
    /// - Jump (Button)
    private void ReadInput()
    {
        if (InputSystem.actions == null)
            return;

        InputAction moveAction = InputSystem.actions["Move"];
        InputAction jumpAction = InputSystem.actions["Jump"];

        if (moveAction != null)
            moveInput = moveAction.ReadValue<Vector2>();

        if (jumpAction != null)
        {
            if (jumpAction.WasPressedThisFrame())
                jumpBufferTimer = jumpBufferTime;

            jumpHeld = jumpAction.IsPressed();
        }

        // Optional: drop down through one-way platforms by pressing down + jump.
        if (jumpAction != null && jumpAction.WasPressedThisFrame() && moveInput.y < -0.5f && isOnOneWayPlatform)
        {
            if (dropThroughRoutine != null)
                StopCoroutine(dropThroughRoutine);

            dropThroughRoutine = StartCoroutine(DropDownThroughOneWay());
        }
    }

    private void UpdateTimers()
    {
        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.deltaTime;

        if (isGrounded)
            coyoteTimer = coyoteTime;
        else if (coyoteTimer > 0f)
            coyoteTimer -= Time.deltaTime;
    }

    /// Supports variable jump height.
    /// Releasing jump early cuts upward velocity.
    private void HandleVariableJump()
    {
        if (!jumpHeld && body.linearVelocity.y > 0f)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * jumpCutMultiplier);
        }
    }

    private void HandleHorizontalMovement()
    {
        if (isKnockedBack)
            return;

        float targetSpeed = moveInput.x * moveSpeed;
        float currentSpeed = body.linearVelocity.x;

        float accel = Mathf.Abs(moveInput.x) > 0.01f
            ? (isGrounded ? acceleration : airAcceleration)
            : groundDeceleration;

        // Basic slope handling:
        // if grounded on a slope, move along the slope tangent instead of purely horizontal.
        if (isGrounded && TryGetGroundNormal(out Vector2 groundNormal))
        {
            float slopeAngle = Vector2.Angle(groundNormal, Vector2.up);

            if (slopeAngle <= maxSlopeAngle)
            {
                Vector2 slopeTangent = new Vector2(groundNormal.y, -groundNormal.x).normalized;
                if (Mathf.Sign(slopeTangent.x) != Mathf.Sign(moveInput.x) && Mathf.Abs(moveInput.x) > 0.01f)
                    slopeTangent *= -1f;

                Vector2 slopeVelocity = slopeTangent * (moveInput.x * moveSpeed);
                Vector2 newVelocity = Vector2.Lerp(body.linearVelocity, new Vector2(slopeVelocity.x, body.linearVelocity.y), accel * Time.fixedDeltaTime / moveSpeed);
                body.linearVelocity = new Vector2(newVelocity.x, body.linearVelocity.y);
                return;
            }
        }

        float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.fixedDeltaTime);
        body.linearVelocity = new Vector2(newSpeed, body.linearVelocity.y);
    }

    private void HandleJump()
    {
        if (jumpBufferTimer <= 0f)
            return;

        bool canUseGroundJump = isGrounded || coyoteTimer > 0f;
        bool canUseAirJump = jumpCount < maxJumpCount;

        if (!canUseGroundJump && !canUseAirJump)
            return;

        body.linearVelocity = new Vector2(body.linearVelocity.x, 0f);
        body.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);

        jumpBufferTimer = 0f;

        if (canUseGroundJump)
        {
            coyoteTimer = 0f;
            jumpCount = 1;
        }
        else
        {
            jumpCount++;
        }

        isGrounded = false;
    }

    private void HandleHeadCollision()
    {
        if (hitHead && body.linearVelocity.y > 0f)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, 0f);
        }
    }

    private void ClampFallSpeed()
    {
        if (body.linearVelocity.y < -maxFallSpeed)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, -maxFallSpeed);
        }
    }

    private void CheckGrounded()
    {
        LayerMask combinedMask = groundMask | oneWayMask;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, combinedMask);
        isOnOneWayPlatform = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, oneWayMask);
    }

    private void CheckHead()
    {
        LayerMask combinedMask = groundMask | oneWayMask;
        hitHead = Physics2D.OverlapCircle(headCheck.position, headCheckRadius, combinedMask);
    }

    private void UpdateGroundState()
    {
        if (isGrounded && !wasGroundedLastFrame)
        {
            jumpCount = 0;
        }

        wasGroundedLastFrame = isGrounded;
    }

    /// Moving platform support:
    /// The player follows the platform by adding the platform's frame delta.
    /// Attach the player to platforms by tagging them as MovingPlatform
    /// or by putting a kinematic Rigidbody2D platform under the player.
    private void ApplyMovingPlatformDelta()
    {
        if (currentMovingPlatform == null)
            return;

        Vector3 delta = currentMovingPlatform.position - lastPlatformPosition;
        body.position += new Vector2(delta.x, delta.y);
        lastPlatformPosition = currentMovingPlatform.position;
    }

    private bool TryGetGroundNormal(out Vector2 groundNormal)
    {
        RaycastHit2D hit = Physics2D.CircleCast(
            groundCheck.position,
            groundCheckRadius * 0.9f,
            Vector2.down,
            slopeCastDistance,
            groundMask
        );

        if (hit.collider != null)
        {
            groundNormal = hit.normal;
            return true;
        }

        groundNormal = Vector2.up;
        return false;
    }

    /// Temporarily ignores one-way platforms so the player can drop through.
    /// Requires one-way platforms to be on oneWayMask.
    private IEnumerator DropDownThroughOneWay()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(oneWayMask);
        filter.useLayerMask = true;

        Collider2D[] hits = new Collider2D[16];
        int count = Physics2D.OverlapCollider(playerCollider, filter, hits);

        for (int i = 0; i < count; i++)
        {
            if (hits[i] != null)
                Physics2D.IgnoreCollision(playerCollider, hits[i], true);
        }

        yield return new WaitForSeconds(dropThroughDuration);

        for (int i = 0; i < count; i++)
        {
            if (hits[i] != null)
                Physics2D.IgnoreCollision(playerCollider, hits[i], false);
        }
    }

    /// External systems can call this for knockback.
    public void ApplyKnockback(Vector2 force)
    {
        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        knockbackRoutine = StartCoroutine(KnockbackRoutine(force));
    }

    private IEnumerator KnockbackRoutine(Vector2 force)
    {
        isKnockedBack = true;
        body.linearVelocity = Vector2.zero;
        body.AddForce(force, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockbackControlLockTime);

        isKnockedBack = false;
    }

    private void UpdateSpriteFacing()
    {
        if (spriteRenderer == null)
            return;

        if (moveInput.x < -0.01f) spriteRenderer.flipX = true;
        if (moveInput.x > 0.01f) spriteRenderer.flipX = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.rigidbody != null && collision.rigidbody.bodyType != RigidbodyType2D.Static)
        {
            foreach (ContactPoint2D point in collision.contacts)
            {
                if (point.normal.y > 0.5f)
                {
                    currentMovingPlatform = collision.transform;
                    lastPlatformPosition = currentMovingPlatform.position;
                    return;
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (currentMovingPlatform == collision.transform)
        {
            currentMovingPlatform = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (headCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(headCheck.position, headCheckRadius);
        }
    }
}