using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Rigidbody2D rb;
    public Animator animator;

    Vector2 movement;
    Vector2 lastMoveDirection;

    void Update()
    {
        // Input
        movement = InputSystem.actions["Move"].ReadValue<Vector2>();
        //Debug.Log("move.magnitude=" + move.magnitude);
        /* if (movement.magnitude > 1e-4f)
        {
            animator.SetBool("Move", true);
        }
        else
        {
            animator.SetBool("Move", false);
        } */
    }

    void FixedUpdate()
    {
        // Physics-based movement
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }
}