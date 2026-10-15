using UnityEngine;

public class DodgeItPlayer : MonoBehaviour
{
    [Header("<< Move >>")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float minX = -8f;
    [SerializeField] private float maxX = 8f;

    [Header("<< Jump >>")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = -20f;

    [Header("<< Rotation >>")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("<< Animation >>")]
    [SerializeField] private Animator animator;

    private CharacterController controller;
    private Player_InputActions inputActions;

    private Vector2 moveInput;
    private float verticalVelocity;
    private bool jumpInput;
    private bool isJumping;


    #region < Unity > 

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputActions = new Player_InputActions();
    }

    private void OnEnable()
    {
        if (inputActions == null)
            return;

        inputActions.DodgeIt.Enable();
        inputActions.DodgeIt.Jump.performed += OnJump;
    }

    private void OnDisable()
    {
        if (inputActions == null)
            return;

        inputActions.DodgeIt.Jump.performed -= OnJump;
        inputActions.DodgeIt.Disable();
    }

    private void OnDestroy()
    {
        inputActions?.Dispose();
    }

    private void Update()
    {
        if (inputActions == null || controller == null)
            return;

        moveInput = inputActions.DodgeIt.Move.ReadValue<Vector2>();

        Move();
        HandleJump();
        UpdateSpeedAnimation();
    }

    #endregion


    #region < Move >


    private void Move()
    {
        Vector3 movement = Vector3.right * moveInput.x * moveSpeed;

        // 중력과 점프 속도 적용
        movement.y = verticalVelocity;

        controller.Move(movement * Time.deltaTime);

        // 좌우 이동 범위 제한
        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, minX, maxX);
        transform.position = position;

        // 이동 방향 바라보기
        if (Mathf.Abs(moveInput.x) > 0.01f)
        {
            Vector3 lookDirection = new Vector3(moveInput.x, 0f, 0f);

            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }


    #endregion


    #region < Jump >

    private void OnJump(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        jumpInput = true;
    }


    private void HandleJump()
    {
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (jumpInput)
            {
                verticalVelocity = jumpForce;
                isJumping = true;

                UpdateJumpAnimation();
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        jumpInput = false;
    }


    public void EndJump()
    {
        isJumping = false;
    }



    #endregion


    #region < Animation >

    private void UpdateJumpAnimation()
    {
        if (isJumping && animator != null)
        {
            animator.SetTrigger("Jump");
        }
    }

    private void UpdateSpeedAnimation()
    {
        if (animator == null)
            return;

        animator.SetFloat("Speed", moveInput.magnitude);
    }

#endregion

}