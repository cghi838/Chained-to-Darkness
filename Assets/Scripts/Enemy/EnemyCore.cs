using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EnemyCore : MonoBehaviour
{

    private enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        ReturnToPost
    }

    //All the organization
    [Header("References")]
    [SerializeField] private GameObject player;
    [SerializeField] private Transform chainAnchor;
    [SerializeField] private Light2D visionLight;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D enemyCollider;
    [SerializeField] private EnemyBehavior behavior;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private bool allowBehaviorToControlFacing = false;

    [Header("Leash Settings")]
    [SerializeField] private float chainRadius = 4f;
    [SerializeField] private bool returnToPostWhenIdle = true;

    [Header("Leash Physics")]
    [SerializeField] private bool useSoftLeash = true;
    [SerializeField] private float leashPullStrength = 15f;
    [SerializeField] private float leashDamping = 5f;

    [Header("Detection Settings")]
    [SerializeField] private float aggroRadius = 6f;
    [SerializeField] private float aggroMemoryTime = 2f;

    [Header("Idle Look Settings")]
    [SerializeField] private bool useIdleLook = true;
    [SerializeField] private float idleLookSpeed = 2f;
    [SerializeField] private float idleLookAngle = 45f;

    [Header("Patrol Settings")]
    [SerializeField] private bool usePatrol = false;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolWaitTime = 1f;
    [SerializeField] private float patrolPointReachedDistance = 0.1f;

    [Header("Field of View")]
    [SerializeField] private float visionAngle = 90f;

    [Header("Line of Sight")]
    [SerializeField] private bool useLineOfSight = true;
    [SerializeField] private float eyeHeightOffset = 0f;

    //Controls what blocks vision
    [SerializeField] private LayerMask visionBlockingLayers;

    [Header("Camera Settings")]
    [SerializeField] private bool hideWhenOffCamera = false;
    [SerializeField] private bool disableBehaviorWhenOffCamera = false;

    [Header("Lighting Settings")]
    [SerializeField] private float idleLightIntensity = 0.5f;
    [SerializeField] private float aggroLightIntensity = 1.2f;
    [SerializeField] private Color idleLightColor = Color.yellow;
    [SerializeField] private Color aggroLightColor = Color.red;

    //Make sure to click on the ENEMY object to see the radius of where it can move with the gizmo
    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private bool debugDrawRay = true;

    private Rigidbody2D rb;
    private Vector2 defaultAnchorPosition;

    public bool isAggro { get; private set; }
    private float lastSeenPlayerTime;
    public Vector2 lastKnownPlayerPosition { get; private set; }
    private EnemyState currentState;
    private int currentPatrolIndex;
    private float patrolWaitTimer;
    private bool isVisibleToCamera = true;
    private float idleLookTimer;
    private float baseIdleAngle;
    private Transform playerTransform;
    private EnemySound sound;

    private void Awake()
    {
        playerTransform = player.transform;
        //Get the position of the anchor and the rigidbody to allow movement
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (enemyCollider == null)
            enemyCollider = GetComponent<Collider2D>();

        defaultAnchorPosition = transform.position;

        if (behavior != null)
            behavior.Initialize(this);

        sound = GetComponent<EnemySound>();
    }

    private void Start()
    {
        UpdateVisionLight();

        baseIdleAngle = transform.eulerAngles.z;

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

    private void ApplyLeashTension()
    {
        Vector2 anchor = AnchorPosition;
        float radius = chainRadius;

        Vector2 offset = rb.position - anchor;
        float distance = offset.magnitude;

        if (distance <= radius)
            return;

        Vector2 dir = offset.normalized;
        float stretch = distance - radius;

        Vector2 pullForce = -dir * (stretch * leashPullStrength);
        Vector2 dampingForce = -rb.linearVelocity * leashDamping;

        rb.AddForce(pullForce + dampingForce);
    }

    //Main enemy logic
    private void FixedUpdate()
    {
        bool canSee = CanSeePlayer();

        float distanceToPlayer = Vector2.Distance(rb.position, playerTransform.position);

        if (canSee && distanceToPlayer <= aggroRadius)
        {
            isAggro = true;
            lastSeenPlayerTime = Time.time;
            lastKnownPlayerPosition = playerTransform.position;
        }
        else if (distanceToPlayer > aggroRadius)
        {
            isAggro = false;
        }
        else if (Time.time > lastSeenPlayerTime + aggroMemoryTime)
        {
            isAggro = false;
        }

        UpdateState();
        RunState(canSee);

        if (useSoftLeash)
        {
            ApplyLeashTension();
        }
        else
        {
            ClampToAnchor();
        }
    }

    private void ClampToAnchor()
    {
        if (behavior != null && behavior.OverrideClamp())
            return;

        Vector2 offset = rb.position - AnchorPosition;

        if (offset.magnitude > chainRadius)
        {
            Vector2 clampedPosition = AnchorPosition + offset.normalized * chainRadius;

            rb.position = clampedPosition;
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void Update()
    {
        UpdateVisionLight();
    }

    private void UpdateState()
    {
        if (isAggro)
        {
            currentState = EnemyState.Chase;
            return;
        }

        float distanceToAnchor = Vector2.Distance(rb.position, AnchorPosition);

        if (returnToPostWhenIdle && distanceToAnchor > 0.1f)
        {
            currentState = EnemyState.ReturnToPost;
            return;
        }

        if (usePatrol && patrolPoints != null && patrolPoints.Length > 0)
        {
            currentState = EnemyState.Patrol;
            return;
        }

        currentState = EnemyState.Idle;
    }

    private void RunState(bool canSeePlayer)
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                rb.linearVelocity = Vector2.zero;
                IdleLook();
                sound?.StopSound();
                break;

            case EnemyState.Patrol:
                Patrol();
                break;

            case EnemyState.Chase:

                if (behavior != null)
                {
                    sound?.PlaySound();
                    behavior.HandleBehavior(canSeePlayer);
                }
                else
                {
                    if (isAggro)
                        MoveToward(playerTransform.position);
                    else
                        MoveToward(lastKnownPlayerPosition);
                }

                break;

            case EnemyState.ReturnToPost:
                //Debug.Log("ReturnToPost");
                sound?.StopSound();
                ReturnToPost();
                break;
        }
    }


    private void UpdateVisionLight()
    {
        if (visionLight == null)
            return;

        visionLight.pointLightOuterRadius = aggroRadius;
        visionLight.pointLightOuterAngle = visionAngle;
        visionLight.pointLightInnerAngle = visionAngle * 0.75f;

        if (isAggro)
        {
            visionLight.color = aggroLightColor;
            visionLight.intensity = aggroLightIntensity;
        }
        else
        {
            visionLight.color = idleLightColor;
            visionLight.intensity = idleLightIntensity;
        }
    }

    //How movement toward the target works
    public void MoveToward(Vector2 target)
    {
        Vector2 direction = (target - rb.position).normalized;

        if (!allowBehaviorToControlFacing)
        {
            FaceDirection(direction);
        }

        Vector2 nextPosition = rb.position + direction * moveSpeed * Time.fixedDeltaTime;

        Vector2 offsetFromAnchor = nextPosition - AnchorPosition;

        if (offsetFromAnchor.magnitude > chainRadius)
        {
            offsetFromAnchor = offsetFromAnchor.normalized * chainRadius;
            nextPosition = AnchorPosition + offsetFromAnchor;
        }

        // Somehow rb.MovePosition doesn't update enemy's position when it returns to the anchor.
        //rb.MovePosition(nextPosition);
        rb.position = nextPosition;
    }

    private void FaceDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void MoveAway(Vector2 target)
    {
        Vector2 direction = (rb.position - target).normalized;

        RaycastHit2D hit = Physics2D.Raycast(rb.position, direction, 0.5f, visionBlockingLayers);

        if (hit.collider != null)
        {
            direction = Vector2.Perpendicular(direction).normalized;
        }

        Vector2 nextPosition = rb.position + direction * moveSpeed * Time.fixedDeltaTime;

        Vector2 offsetFromAnchor = nextPosition - AnchorPosition;

        if (offsetFromAnchor.magnitude > chainRadius)
        {
            offsetFromAnchor = offsetFromAnchor.normalized * chainRadius;
            nextPosition = AnchorPosition + offsetFromAnchor;
        }

        rb.MovePosition(nextPosition);
    }

    //How returning to the anchor movement works
    private void ReturnToPost()
    {
        Vector2 toAnchor = AnchorPosition - rb.position;
        float dist = toAnchor.magnitude;

        if (dist < 0.05f)
        {
            rb.position = AnchorPosition;
            rb.linearVelocity = Vector2.zero;

            baseIdleAngle = transform.eulerAngles.z;
            idleLookTimer = 0f;

            return;
        }

        MoveToward(AnchorPosition);
    }

    private void IdleLook()
    {
        if (!useIdleLook)
            return;

        idleLookTimer += Time.deltaTime;

        float angleOffset = Mathf.Sin(idleLookTimer * idleLookSpeed) * idleLookAngle;

        float finalAngle = baseIdleAngle + angleOffset;

        transform.rotation = Quaternion.Euler(0, 0, finalAngle);
    }

    //Simple patrol movement between patrol points
    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

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

        if (directionToPoint.magnitude <= patrolPointReachedDistance)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            patrolWaitTimer = patrolWaitTime;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        MoveToward(targetPoint.position);
    }

    //Allow vision color to be changed
    public void SetVisionColor(Color color, float intensityMultiplier = 1f)
    {
        if (visionLight == null)
            return;

        visionLight.color = color;
        visionLight.intensity = aggroLightIntensity * intensityMultiplier;
    }

    //Uses a separate aggro radius so it can be fine tuned
    private bool CanSeePlayer()
    {
        Vector2 origin = rb.position + Vector2.up * eyeHeightOffset;

        Vector2 directionToPlayer = playerTransform.position - (Vector3)origin;
        float distanceToPlayer = directionToPlayer.magnitude;

        float distanceFromAnchor = Vector2.Distance(playerTransform.position, AnchorPosition);

        if (distanceFromAnchor > aggroRadius)
            return false;

        //Vision cone code
        float angle = Vector2.Angle(transform.up, directionToPlayer);
        if (angle > visionAngle * 0.5f)
            return false;

        if (!useLineOfSight)
            return true;

        //Raycast using LayerMask so only valid objects block vision
        RaycastHit2D hit = Physics2D.Raycast(origin, directionToPlayer.normalized, distanceToPlayer, visionBlockingLayers);

        if (debugDrawRay)
            Debug.DrawRay(origin, directionToPlayer.normalized * distanceToPlayer, Color.cyan);

        if (hit.collider == null)
            return false;

        return hit.transform == playerTransform;
    }

    public void FaceTarget(Vector2 target)
    {
        Vector2 direction = (target - rb.position);

        if (direction.sqrMagnitude < 0.0001f)
            return;

        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        float newAngle = Mathf.LerpAngle(
            transform.eulerAngles.z,
            targetAngle,
            15f * Time.deltaTime
        );

        transform.rotation = Quaternion.Euler(0, 0, newAngle);
    }

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

    //Gizmos to see debug stuff
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        //Anchor position
        Vector3 anchorCenter = chainAnchor != null
            ? chainAnchor.position
            : (Application.isPlaying ? defaultAnchorPosition : transform.position);

        //Leash radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(anchorCenter, chainRadius);

        //Aggro radius
        Gizmos.color = Color.red;
        // Gizmos.DrawWireSphere(transform.position, aggroRadius);
        Gizmos.DrawWireSphere(anchorCenter, aggroRadius);

        //Field of View
        Vector3 origin = transform.position + Vector3.up * eyeHeightOffset;

        Vector3 leftBoundary = Quaternion.Euler(0, 0, visionAngle * 0.5f) * transform.up;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, -visionAngle * 0.5f) * transform.up;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(origin, leftBoundary * aggroRadius);
        Gizmos.DrawRay(origin, rightBoundary * aggroRadius);

        //Center of the vision cone
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(origin, transform.up * aggroRadius);

        //Last known player position
        if (Application.isPlaying && isAggro)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(lastKnownPlayerPosition, 0.15f);
        }

        //Patrol path
        if (usePatrol && patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.magenta;

            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null)
                    continue;

                Gizmos.DrawSphere(patrolPoints[i].position, 0.12f);

                if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                {
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                }
            }
        }
    }

    public Transform GetPlayer() => playerTransform;
    public Rigidbody2D GetRB() => rb;
    public Vector2 GetAnchorPosition() => AnchorPosition;
    public float GetChainRadius() => chainRadius;
    public virtual bool OverrideClamp() => false;
    public float GetAggroRadius() => aggroRadius;
}