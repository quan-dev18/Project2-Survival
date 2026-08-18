using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats; // Reference to the PlayerStats component

    [SerializeField] private Rigidbody2D rb; // Reference to the Rigidbody2D component
    [SerializeField] private Animator animator; // Reference to the Animator component
    private Vector2 movementInput; // Stores the player's movement input

    void Awake()
    {
        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>(); // Get the PlayerStats component if not assigned
        }
    }
    void Start()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>(); // Get the Rigidbody2D component if not assigned
        }
        if (animator == null)
        {
            animator = GetComponent<Animator>(); // Get the Animator component if not assigned
        }
    }

    void Update()
    {
        movementInput = new Vector2(Input.GetAxisRaw("Horizontal"), 
                                    Input.GetAxisRaw("Vertical")); // Get movement input from player

        //UpdateAnimation(); // Update the animation based on movement input
    }

    void FixedUpdate()
    {
        MovePlayer(); // Move the player based on input
    }
    void MovePlayer()
    {
        rb.velocity = movementInput * playerStats.MoveSpeed; // Move the player using Rigidbody2D
    }

    void UpdateAnimation()
    {
        bool isMoveing = movementInput != Vector2.zero;
        animator.SetBool("isMoving", isMoveing); // Set the "isMoving" parameter in the Animator
        if (Mathf.Abs(movementInput.y) > Mathf.Abs(movementInput.x))
        {
            animator.SetInteger("Dir", movementInput.y > 0 ? 1 : 0); // 0 = Down, 1 = Up
        }
        else if (movementInput.x != 0)
        {
            animator.SetInteger("Dir", 2); // 2 = Side
            Flip(movementInput.x);
        }
    }
    private void Flip(float x)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(x);
        transform.localScale = scale;
    }
}

