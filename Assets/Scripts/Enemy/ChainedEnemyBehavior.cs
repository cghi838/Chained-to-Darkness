using UnityEngine;

public class ChainedEnemyBehavior : MonoBehaviour
{
    //Simple state machine so the enemy knows what behavior to run
    private enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        ReturnToPost
    }

    //All the organization
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform chainAnchor;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("Leash Settings")]
    [SerializeField] private float chainRadius = 4f;
    [SerializeField] private float aggroRadius = 6f;
    [SerializeField] private bool returnToPostWhenIdle = true;

    [Header("Patrol Settings")]
    [SerializeField] private bool usePatrol = false;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolWaitTime = 1f;
    [SerializeField] private float patrolPointReachedDistance = 0.1f;

    [Header("Line of Sight")]
    [SerializeField] private bool useLineOfSight = true;
    [SerializeField] private float eyeHeightOffset = 0f;

    [Header("Camera Settings")]
    [SerializeField] private bool hideWhenOffCamera = false;
    [SerializeField] private bool disableBehaviorWhenOffCamera = false;

    //Make sure to click on the ENEMY object to see the radius of where it can move with the gizmo
    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private bool debugDrawRay = true;

    [Header("Player Hit Detection")]
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private float damageToPlayer = 10f; // both HP and sanity damage for simplicity

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D enemyCollider;

    private Vector2 defaultAnchorPosition;
    private bool isAggro;

    //State machine variables
    private EnemyState currentState;
    private int currentPatrolIndex;
    private float patrolWaitTimer;
    private bool isVisibleToCamera = true;
    private PlayerHealth playerHealth;
    private SanitySystem sanitySystem;
    private EnemySound sound;

    private void Awake()
    {
        //Get the position of the anchor and the rigidbody to allow movement
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyCollider = GetComponent<Collider2D>();

        defaultAnchorPosition = transform.position;
        sound = GetComponent<EnemySound>();
    }

    private void Start()
    {
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            sanitySystem = player.GetComponent<SanitySystem>();
        }
        //Choose a starting state
        if (usePatrol && patrolPoints != null && patrolPoints.Length > 0)
            currentState = EnemyState.Patrol;
        else
            currentState = EnemyState.Idle;
    }

    //Finds where the anchor is
    private Vector2 AnchorPosition
    {
        get
        {
            if (chainAnchor != null)
                return chainAnchor.position;

            return defaultAnchorPosition;
        }
    }

    //Main enemy logic loop
    private void FixedUpdate()
    {
        //Optional camera optimization
        if (disableBehaviorWhenOffCamera && !isVisibleToCamera)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        UpdateState();
        RunState();
    }

    //Decides what state the enemy should be in
    private void UpdateState()
    {
        float distanceToPlayer = Vector2.Distance(player.position, AnchorPosition);

        bool withinAggroRange = distanceToPlayer <= aggroRadius;
        bool hasLineOfSight = !useLineOfSight || CanSeePlayer();

        isAggro = withinAggroRange && hasLineOfSight;

        //Highest priority: chase the player if detected
        if (isAggro)
        {
            currentState = EnemyState.Chase;
            return;
        }

        //If not aggro and far from home, return to post
        float distanceToAnchor = Vector2.Distance(rb.position, AnchorPosition);

        if (returnToPostWhenIdle && distanceToAnchor > 0.1f)
        {
            currentState = EnemyState.ReturnToPost;
            return;
        }

        //If patrol is enabled, patrol
        if (usePatrol && patrolPoints != null && patrolPoints.Length > 0)
        {
            currentState = EnemyState.Patrol;
            return;
        }

        //Otherwise just idle
        currentState = EnemyState.Idle;
    }

    //Runs the current state behavior
    private void RunState()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                rb.linearVelocity = Vector2.zero;
                break;

            case EnemyState.Patrol:
                Patrol();
                break;

            case EnemyState.Chase:
                sound?.PlaySound();
                ChasePlayer();
                break;

            case EnemyState.ReturnToPost:
                sound?.StopSound();
                ReturnToPost();
                break;
        }
    }

    //Raycast that requires line of sight with the player COLLIDER
    private bool CanSeePlayer()
    {
        Vector2 origin = rb.position + Vector2.up * eyeHeightOffset;
        Vector2 direction = ((Vector2)player.position - origin).normalized;
        float distance = Vector2.Distance(origin, player.position);

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance);

        if (debugDrawRay)
            Debug.DrawRay(origin, direction * distance, Color.cyan);

        if (hit.collider == null)
            return false;

        return hit.transform == player;
    }

    //How movement toward the player works
    private void ChasePlayer()
    {
        Vector2 directionToPlayer = ((Vector2)player.position - rb.position).normalized;
        Vector2 nextPosition = rb.position + directionToPlayer * moveSpeed * Time.fixedDeltaTime;

        Vector2 offsetFromAnchor = nextPosition - AnchorPosition;

        //Prevents enemy from moving outside the chain radius
        if (offsetFromAnchor.magnitude > chainRadius)
        {
            offsetFromAnchor = offsetFromAnchor.normalized * chainRadius;
            nextPosition = AnchorPosition + offsetFromAnchor;
        }

        //FaceDirection(directionToPlayer.x);
        FaceDirection(directionToPlayer);
        rb.MovePosition(nextPosition);
    }

    //How returning to the post movement works
    private void ReturnToPost()
    {
        Vector2 directionToAnchor = AnchorPosition - rb.position;

        if (directionToAnchor.magnitude < 0.1f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 moveDir = directionToAnchor.normalized;
        //FaceDirection(moveDir.x);
        FaceDirection(moveDir);
        rb.MovePosition(rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);
    }

    //New patrol behavior
    //Enemy moves between patrol points in order
    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        //Wait a moment before moving to next point
        if (patrolWaitTimer > 0f)
        {
            patrolWaitTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Transform targetPoint = patrolPoints[currentPatrolIndex];

        if (targetPoint == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 directionToPoint = (Vector2)targetPoint.position - rb.position;

        //If enemy reached the patrol point, go to next one
        if (directionToPoint.magnitude <= patrolPointReachedDistance)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            patrolWaitTimer = patrolWaitTime;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 moveDir = directionToPoint.normalized;
        //FaceDirection(moveDir.x);
        FaceDirection(moveDir);
        rb.MovePosition(rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);
    }

    //Small helper to make the sprite face the direction it is moving
    private void FaceDirection(Vector2 moveDirection)
    {
        if (spriteRenderer == null)
            return;

        if (moveDirection != Vector2.zero)
        {
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.rotation = targetRotation;
        }
        // float xDirection = moveDirection.x;
        // if (xDirection > 0.01f)
        //     spriteRenderer.flipX = false;
        // else if (xDirection < -0.01f)
        //     spriteRenderer.flipX = true;
    }

    //Makes the enemy visible again when on screen
    private void OnBecameVisible()
    {
        isVisibleToCamera = true;

        if (hideWhenOffCamera)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = true;

            if (enemyCollider != null)
                enemyCollider.enabled = true;
        }
    }

    //Hides the enemy when off screen if enabled
    private void OnBecameInvisible()
    {
        isVisibleToCamera = false;

        if (hideWhenOffCamera)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = false;

            if (enemyCollider != null)
                enemyCollider.enabled = false;
        }
    }

    //Gizmos to see debug stuff (radius they can move and shows raycast)
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        Vector3 center = chainAnchor != null
            ? chainAnchor.position
            : (Application.isPlaying ? defaultAnchorPosition : (Vector2)transform.position);

        //Chain leash radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, chainRadius);

        //Aggro radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, aggroRadius);

        //Patrol path
        if (usePatrol && patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.green;

            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null)
                    continue;

                Gizmos.DrawSphere(patrolPoints[i].position, 0.12f);

                if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
            }
        }
    }

   /* private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Enemy Trigger hit: {other.gameObject.name} layer: {other.gameObject.layer}");

        if ((playerMask.value & (1 << other.gameObject.layer)) == 0) return;

        // HP
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damageToPlayer);
        }

        // Sanity
        if (sanitySystem != null)
        {
            sanitySystem.DecreaseSanity(damageToPlayer); // Example sanity damage
        }
    } */
}