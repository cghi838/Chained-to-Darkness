using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementPlatformer : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 10f;
    public float speed = 5f;
    public float speedMultiplier = 1f; // modified by PlayerInventory

    [Header("Jump")]
    public float jumpPower = 10f;
    public int maxJumpCount = 2;

    [Header("Ground Detection")]
    public LayerMask groundMask;
    public Transform groundCheckPoint; // empty child object at player's feet for more accurate ground detection
    public float groundCheckRadius = 0.2f; // radius for ground check overlap circle    

    [Header("Debug")]
    public bool showGroundCheckGizmo = true; // visualize ground check area in Scene view

    private Rigidbody2D body;
    private Vector2 movement;
    private int jumpCount = 0;
    private bool jumpPressed = false;
    private SpriteRenderer spriteRenderer;
    private Animator animator; // animator
    private bool isGrounded; // animator
    private bool wasGrounded; // animator
    private Vector2 moveInput;
    private float jumpedTime = 0f;
    private float jumpGroundIgnoreTime = 0.1f; // time after jump to ignore ground check for better jump responsiveness

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();    // animator
    }

    private void Start()
    {
        // Debug.Log("PlayerMovementPlatformer Start");
        body.bodyType = RigidbodyType2D.Dynamic; // Ensure Rigidbody is dynamic for physics interactions
    }

    private void Update()
    {
        if (Keyboard.current != null)
        {
            // Stop speed when horizontal keys are released
            // if (Keyboard.current.aKey.wasReleasedThisFrame || Keyboard.current.dKey.wasReleasedThisFrame)
            // {
            //     body.linearVelocity = new Vector2(body.linearVelocity.normalized.x * 0.5f, body.linearVelocity.y);
            // }
            /* // Flip sprite based on heading direction        
            if (Keyboard.current.aKey.isPressed)
            {
                spriteRenderer.flipX = true;
            }
            else if (Keyboard.current.dKey.isPressed)
            {
                spriteRenderer.flipX = false;
            } */
        }
        moveInput = InputSystem.actions["Move"].ReadValue<Vector2>();
        // InputAction moveAction = InputSystem.actions["Move"];
        // if (moveAction.WasPressedThisFrame())
        // {
        //     moveInput = moveAction.ReadValue<Vector2>();
        //     HandleFlip();
        // }

        if (InputSystem.actions["Jump"].WasPressedThisFrame() && !jumpPressed && (/*isGrounded || */jumpCount < maxJumpCount))
        {
            if (jumpCount < maxJumpCount)
            {
                // Debug.Log($"Jump pressed! Jump count: {jumpCount}, isGrounded: {isGrounded}, jumpPressed: {jumpPressed}");
                jumpPressed = true;
            }
            //            jumpCount++;
        }

        HandleFlip();
    }

    private void FixedUpdate()
    {
        //        movement = InputSystem.actions["Move"].ReadValue<Vector2>();
        // wasGrounded = isGrounded;
        // bool rawGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundMask);

        // if (Time.time - jumpedTime < jumpGroundIgnoreTime)
        //     isGrounded = false;
        // else
        //     isGrounded = rawGrounded;

        // if (!wasGrounded && isGrounded)
        // {
        //     jumpCount = 0;
        // }

        // if (jumpPressed)
        // {
        //     if (jumpCount < maxJumpCount)
        //     {
        //         Jump();
        //         jumpedTime = Time.time;
        //         animator.SetTrigger("Jump");
        //     }
        //     jumpPressed = false;
        // }

        wasGrounded = isGrounded;
        bool rawGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundMask);
        if (body.linearVelocity.y > 0.1f) 
        {
            isGrounded = false;
        }
        else
        {
            isGrounded = rawGrounded;
        }
        //        isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundMask);

        if (!wasGrounded && isGrounded)
        {
            jumpCount = 0; // reset jump count when grounded
        }
        // Debug.Log($"WasGrounded: {wasGrounded}, isGrounded: {isGrounded}, jumpCount: {jumpCount}, jumpPressed: {jumpPressed}");

        float vx = body.linearVelocityX;

        // Detecting situations where there is input but velocity does not change (static friction)
        if (Mathf.Abs(moveInput.x) > 0.1f && Mathf.Abs(vx) < 0.01f)
        {
            body.linearVelocity = new Vector2(moveInput.x * 0.1f, body.linearVelocityY);
        }

        body.AddForce(Vector2.right * moveInput.x * speed * speedMultiplier, ForceMode2D.Impulse);

        // Set max speed so player goes not too fast, but only limit when trying to go faster in the same direction
        vx = body.linearVelocityX;
        float effectiveMax = maxSpeed * speedMultiplier;
        if (Mathf.Abs(vx) > effectiveMax && Mathf.Sign(vx) == Mathf.Sign(moveInput.x))
        {
            body.linearVelocity = new Vector2(Mathf.Sign(vx) * effectiveMax, body.linearVelocityY);
        }

        // Jump
        if (jumpPressed)
        {
            if (jumpCount < maxJumpCount)
            {
                Jump();
                animator.SetTrigger("Jump"); // animator
            }
            else
            {
                // Debug.Log("Max jump count reached: " + jumpCount);
            }

            jumpPressed = false;
        }
    }

    private void LateUpdate()
    {
        float speedAbs = Mathf.Abs(body.linearVelocity.x);
        float vy = body.linearVelocity.y;

        animator.SetFloat("Speed", speedAbs);
        animator.SetBool("IsGrounded", isGrounded);
        // animator.SetFloat("VerticalVelocity", vy);

        //        wasGrounded = isGrounded;
        // FlipX
        HandleFlip();
    }

    // Flip the sprite depending on the left/right direction using scale factor
    private void HandleFlip()
    {
        // if (Mathf.Abs(body.linearVelocity.x) > 0.01f)
        // {
        //spriteRenderer.flipX = rb.linearVelocity.x < 0f;

        // new sign
        /*            float sign = body.linearVelocity.x < 0f ? -1f : 1f;
                    // player renderer flip
                    Vector3 scale = transform.localScale;
                    scale.x = Mathf.Abs(scale.x) * sign;
                    transform.localScale = scale;
        */
        if (moveInput.x < -0.01f) spriteRenderer.flipX = true;
        else if (moveInput.x > 0.01f) spriteRenderer.flipX = false;
        //        }
    }

    private void Jump()
    {
        if (jumpCount < maxJumpCount)
        {
            // Debug.Log("Jump1! Jump Count = " + (jumpCount));
            body.linearVelocity = new Vector2(body.linearVelocity.x, 0f); // reset Y before jump for consistent height
            body.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            jumpCount++;
            // Debug.Log("Jump2! Jump Count = " + (jumpCount));
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (IsGroundMask(other.gameObject))
        {
            // Debug.Log("ENTER: Collision with ground mask: " + other.gameObject.name + ", normal: " + other.contacts[0].normal);
            if (other.contacts[0].normal.y > 0.5f) // only reset jump if landing on top of the ground, not hitting from the side  
            {
                // jumpCount = 0;
                // isGrounded = true;
                // jumpPressed = false;

                // grounded
                if (!wasGrounded)
                    animator.SetTrigger("Land");  // animator
            }
        }
    }

    //     private void OnCollisionStay2D(Collision2D other)
    //     {
    //         // only reset jump if landing on top of the ground, not hitting from the side    
    // //         if (IsGroundMask(other.gameObject))
    // //         {
    // //             // Debug.Log("STAY: Collision with ground mask: " + other.gameObject.name + ", normal: " + other.contacts[0].normal);
    // //             if (other.contacts[0].normal.y > 0.5f)
    // //             {
    // //                 Debug.Log($"Jump count = {jumpCount}");
    // //                 if (jumpCount == maxJumpCount) jumpCount = 0;
    // //                 isGrounded = true; // animator
    // //             }
    // //             else
    // //             {
    // //                 jumpCountTimerStarted = true;
    // //                 jumpCountTimer = 0f;
    // // //                Debug.Log($"Not grounded, normal: {other.contacts[0].normal}, jumpCount: {jumpCount}");
    // //             }
    // //         }
    //     }

    // private void OnCollisionExit2D(Collision2D other)
    // {
    //     if (IsGroundMask(other.gameObject))
    //     {
    //         isGrounded = false;  // animator
    //     }
    // }

    private bool IsGroundMask(GameObject obj) => (groundMask.value & (1 << obj.layer)) != 0;

    private void OnDrawGizmos()
    {
        if (!showGroundCheckGizmo) return;
        if (groundCheckPoint == null) return;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
    }
}
