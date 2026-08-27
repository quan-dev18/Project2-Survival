using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats; // Reference to the PlayerStats component

    [SerializeField] private Rigidbody2D rb; // Reference to the Rigidbody2D component
    [SerializeField] private Animator animator; // Reference to the Animator component
    public Vector2 movementInput; // Stores the player's movement input
    //animation dir
    private int lastDir = 0;
    public int LastDir => lastDir;
    [SerializeField] private Transform mesh;

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
        JoystickController joystick = JoystickController.Instance;
        if (joystick != null && joystick.IsActive)
        {
            movementInput = joystick.Direction;
        }
        else
        {
            movementInput = new Vector2(Input.GetAxisRaw("Horizontal"),
                                        Input.GetAxisRaw("Vertical")); // Get movement input from player
        }

        UpdateAnimation();
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
        bool isMoving = movementInput != Vector2.zero;

        if (isMoving)
        {
            // 1. Kiểm tra hướng di chuyển
            if (movementInput.y > 0)
            {
                lastDir = 1; // Đi lên -> Walk_Back
            }
            else if (movementInput.y < 0)
            {
                lastDir = 0; // Đi xuống -> Walk_Front
            }
            else if (Mathf.Abs(movementInput.x) > 0.01f)
            {
                lastDir = 0; // Đi ngang (Trái / Phải) -> Luôn đặt là Walk_Front
            }

            // Gán hướng di chuyển cho Animator (0: Front, 1: Back)
            animator.SetInteger("Dir", lastDir);

            // Lật mặt nhân vật khi đi trái / phải
            if (Mathf.Abs(movementInput.x) > 0.01f)
            {
                Flip(movementInput.x);
            }
        }
        else
        {
            // Khi dừng lại: chuyển về Idle với hướng nhìn cuối cùng
            animator.SetInteger("Dir", -1);
            animator.SetFloat("idle_blend", lastDir); 
        }
    }
    private void Flip(float x)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * -Mathf.Sign(x);
        if(mesh != null)
        {
            mesh.localScale = scale;
        }
    }

    public void SetMesh(Transform newMesh)
    {
        mesh = newMesh;
    }

    public void SetAnimator(Animator newAnimator)
    {
        animator = newAnimator;
    }
}

