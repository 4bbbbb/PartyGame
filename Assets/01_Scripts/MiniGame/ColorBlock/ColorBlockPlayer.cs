using Fusion;
using UnityEngine;

public class ColorBlockPlayer : NetworkBehaviour
{
    [Header("<< Character >>")]
    [SerializeField] private Transform characterModel;

    [Header("<< Animation >>")]
    [SerializeField] private Animator animator;

    [Header("<< Move >>")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("<< Jump >>")]
    [SerializeField] private float jumpForce = 7f;

    [Header("<< Rotation >>")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("<< Ground Check >>")]
    [SerializeField] private Transform groundCheck;

    [SerializeField] private float groundCheckRadius = 0.2f;

    [SerializeField] private LayerMask groundLayer;

    [Header("<< Camera >>")]
    [SerializeField] private Transform camPos;

    #region < Network >

    [Networked]
    public int PlayerIndex { get; private set; }

    [Networked]
    private int CharacterIndex { get; set; }

    #endregion


    #region < Player Network >

    private PlayerNetwork playerNetwork;

    public void SetPlayerNetwork(PlayerNetwork player)
    {
        if (!Object.HasStateAuthority)
            return;

        playerNetwork = player;
    }

    public void SetPlayerIndex(int index)
    {
        if (!Object.HasStateAuthority)
            return;

        PlayerIndex = index;
    }

    public void SetCharacterIndex(int index)
    {
        if (!Object.HasStateAuthority)
            return;

        CharacterIndex = index;
    }

    #endregion


    #region < Movement >

    private Rigidbody rb;

    private Player_InputActions inputActions;

    private Vector2 moveInput;

    private bool jumpInput;

    #endregion


    #region < Network State >

    [Networked, OnChangedRender(nameof(OnJumpingChanged))]
    private NetworkBool IsJumping { get; set; }

    #endregion


    #region < Unity >

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.freezeRotation = true;
        }
    }

    #endregion


    #region < Spawn >

    public override void Spawned()
    {
        Debug.Log(
            $"[ColorBlockPlayer Spawned] " +
            $"PlayerRef = {Object.InputAuthority}"
        );

        // 내 캐릭터만 카메라 연결 + InputActions 생성
        if (Object.HasInputAuthority)
        {
            SetupCamera();

            inputActions =
                new Player_InputActions();

            inputActions.ColorBlock.Enable();

            inputActions.ColorBlock.Jump.performed +=
                OnJump;
        }

        UpdateJumpAnimation();
    }

    private void SetupCamera()
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("Main Camera를 찾을 수 없습니다.");
            return;
        }

        ColorBlockCamera cameraController =
            Camera.main.GetComponent<ColorBlockCamera>();

        if (cameraController == null)
        {
            Debug.LogError(
                "Main Camera에 ColorBlockCamera가 없습니다."
            );

            return;
        }

        if (camPos == null)
        {
            Debug.LogError(
                $"CameraTarget이 없습니다. " +
                $"PlayerRef = {Object.InputAuthority}"
            );

            return;
        }

        cameraController.SetTarget(camPos);

        Debug.Log(
            $"[ColorBlockCamera] 내 카메라 연결 완료 : " +
            $"{Object.InputAuthority}"
        );
    }

    #endregion


    #region < Despawn >

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        if (inputActions == null)
            return;

        inputActions.ColorBlock.Jump.performed -=
            OnJump;

        inputActions.ColorBlock.Disable();

        inputActions.Dispose();

        inputActions = null;
    }

    #endregion


    #region < Input >

    private void Update()
    {
        // 아직 Spawn되지 않은 상태
        if (!Object)
            return;

        // 내 플레이어가 아니면 입력하지 않음
        if (!Object.HasInputAuthority)
            return;

        if (inputActions == null)
            return;

        moveInput =
            inputActions.ColorBlock.Move.ReadValue<Vector2>();
    }

    private void OnJump(
        UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (!Object.HasInputAuthority)
            return;

        if (IsJumping)
            return;

        jumpInput = true;
    }

    #endregion


    #region < Network Movement >

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority)
            return;

        Move();

        if (jumpInput)
        {
            Jump();

            jumpInput = false;
        }
    }

    #endregion


    #region < Move >

    private void Move()
    {
        if (rb == null)
            return;

        if (camPos == null)
            return;

        // 카메라가 바라보는 방향
        Vector3 forward = camPos.forward;
        Vector3 right = camPos.right;

        // 수평 이동만 사용
        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        // 카메라 기준 WASD
        Vector3 moveDirection =
            forward * moveInput.y +
            right * moveInput.x;

        moveDirection =
            Vector3.ClampMagnitude(
                moveDirection,
                1f
            );

        Vector3 velocity =
            moveDirection * moveSpeed;

        rb.linearVelocity =
            new Vector3(
                velocity.x,
                rb.linearVelocity.y,
                velocity.z
            );

        // 애니메이션
        float speed =
            moveDirection.magnitude;

        if (animator != null)
        {
            animator.SetFloat(
                "Speed",
                speed
            );
        }

        // 이동 방향을 바라봄
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(moveDirection);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed *
                    Runner.DeltaTime
                );
        }
    }

    #endregion


    #region < Jump >

    private void Jump()
    {
        if (IsJumping)
            return;

        if (!IsGrounded())
            return;

        IsJumping = true;

        rb.linearVelocity =
            new Vector3(
                rb.linearVelocity.x,
                0f,
                rb.linearVelocity.z
            );

        rb.AddForce(
            Vector3.up * jumpForce,
            ForceMode.Impulse
        );

        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }
    }

    public void EndJump()
    {
        if (!Object.HasInputAuthority)
            return;

        IsJumping = false;
    }

    #endregion


    #region < Animation >

    private void UpdateJumpAnimation()
    {
        if (IsJumping &&
            animator != null)
        {
            animator.SetTrigger("Jump");
        }
    }

    private void OnJumpingChanged()
    {
        UpdateJumpAnimation();
    }

    #endregion


    #region < Ground >

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

    #endregion
}