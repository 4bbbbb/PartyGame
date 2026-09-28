using UnityEngine;

public class ColorBlockPlayer : MonoBehaviour
{
    [Header("<< Move >>")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("<< Jump >>")]
    [SerializeField] private float jumpForce = 7f;

    [Header("<< Ground Check >>")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;

    private Player_InputActions inputActions;

    private Vector2 moveInput;
    private bool jumpInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.freezeRotation = true;

        inputActions = new Player_InputActions();
    }

    private void OnEnable()
    {
        inputActions.ColorBlock.Enable();

        inputActions.ColorBlock.Jump.performed += OnJump;
    }

    private void OnDisable()
    {
        inputActions.ColorBlock.Jump.performed -= OnJump;

        inputActions.ColorBlock.Disable();
    }

    private void Update()
    {
        moveInput = inputActions.ColorBlock.Move.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        Move();

        if (jumpInput)
        {
            Jump();
            jumpInput = false;
        }
    }

    private void OnJump(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        jumpInput = true;
    }

    private void Move()
    {
        Vector3 moveDirection = new Vector3(
            moveInput.x,
            0f,
            moveInput.y
        );

        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        Vector3 velocity = moveDirection * moveSpeed;

        rb.linearVelocity = new Vector3(
            velocity.x,
            rb.linearVelocity.y,
            velocity.z
        );
    }

    private void Jump()
    {
        if (!IsGrounded())
            return;

        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z
        );

        rb.AddForce(
            Vector3.up * jumpForce,
            ForceMode.Impulse
        );
    }

    private bool IsGrounded()
    {
        if (groundCheck == null)
            return false;

        return Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }
}