using UnityEngine;

public class JumperBehavior : EnemyBehavior
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float jumpCooldown = 2f;

    [Header("Windup Settings")]
    [SerializeField] private float windupTime = 1f;
    [SerializeField] private float pullbackForce = 2f;

    [Header("Indicator Settings")]
    [SerializeField] private Color warningColor = Color.magenta;

    private float lastJumpTime;
    private float windupTimer;

    private bool isJumping = false;
    private float recoveryTimer;
    [SerializeField] private float recoveryTime = 0.5f;
    [SerializeField] private float recoilForce = 3f;

    private enum JumpState
    {
        Ready,
        Windup,
        Jumping,
        Recovering
    }

    private JumpState currentState = JumpState.Ready;

    public override void HandleBehavior(bool canSeePlayer)
    {
        if (!core.isAggro)
        {
            currentState = JumpState.Ready;
            return;
        }

        switch (currentState)
        {
            case JumpState.Ready:
                HandleReady();
                break;

            case JumpState.Windup:
                HandleWindup();
                break;

            case JumpState.Jumping:
                if (Time.time > lastJumpTime + jumpCooldown)
                {
                    currentState = JumpState.Ready;
                }
                break;

            case JumpState.Recovering:
                HandleRecovery();
                break;
        }
    }

    private void HandleReady()
    {
        core.FaceTarget(core.GetPlayer().position);

        if (Time.time > lastJumpTime + jumpCooldown)
        {
            currentState = JumpState.Windup;
            windupTimer = windupTime;
        }
    }

    private void HandleWindup()
    {
        Rigidbody2D rb = core.GetRB();

        Vector2 playerPos = core.GetPlayer().position;
        core.FaceTarget(playerPos);

        core.SetVisionColor(warningColor, 1.5f);

        Vector2 dirToPlayer = (playerPos - (Vector2)core.transform.position).normalized;

        Vector2 anchor = core.GetAnchorPosition();
        float chainRadius = core.GetChainRadius();

        float distToAnchor = Vector2.Distance(rb.position, anchor);

        if (distToAnchor < chainRadius - 0.2f)
        {
            rb.linearVelocity = -dirToPlayer * pullbackForce;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        windupTimer -= Time.deltaTime;

        if (windupTimer <= 0f)
        {
            JumpAtPlayer();
            lastJumpTime = Time.time;
            currentState = JumpState.Jumping;
        }
    }

    private void JumpAtPlayer()
    {
        Rigidbody2D rb = core.GetRB();

        Vector2 dir = (core.GetPlayer().position - core.transform.position).normalized;

        rb.linearVelocity = Vector2.zero;

        rb.AddForce(dir * jumpForce, ForceMode2D.Impulse);

        isJumping = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isJumping)
            return;

        Rigidbody2D rb = core.GetRB();

        rb.linearVelocity = Vector2.zero;

        Vector2 recoilDir = collision.contacts[0].normal;
        rb.AddForce(recoilDir * recoilForce, ForceMode2D.Impulse);

        isJumping = false;

        recoveryTimer = recoveryTime;
        currentState = JumpState.Recovering;
    }

    private void HandleRecovery()
    {
        Rigidbody2D rb = core.GetRB();

        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 10f * Time.deltaTime);

        recoveryTimer -= Time.deltaTime;

        if (recoveryTimer <= 0f)
        {
            currentState = JumpState.Ready;
        }
    }

}