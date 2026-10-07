using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ColorBlockPlayer : NetworkBehaviour
{
    [Header("<< Character >>")]
    [SerializeField] private Transform characterModel;
    [SerializeField] private Renderer characterRenderer;

    [Header("<< Character Database >>")]
    [SerializeField] private CharacterDatabase characterDatabase;

    [Header("<< Animation >>")]
    [SerializeField] private Animator animator;

    [Header("<< Move >>")]
    [SerializeField] private float moveSpeed = 10f;

    [SerializeField] private float rotationSpeed = 3f;

    [Header("<< Jump >>")]
    [SerializeField] private float jumpForce = 8f;

    [SerializeField] private float gravity = -20f;

    [Header("<< Ground Check >>")]
    [SerializeField] private Transform groundCheck;

    [SerializeField] private float groundCheckRadius = 0.2f;

    [SerializeField] private LayerMask groundLayer;

    [Header("<< Camera >>")]
    [SerializeField] private Transform camPos;


    #region < Network >

    [Networked]
    public int PlayerIndex { get; private set; }

    [Networked, OnChangedRender(nameof(OnCharacterIndexChanged))]
    public int CharacterIndex { get; private set; } = -1;

    #endregion


    #region < Player Network >

    private PlayerNetwork playerNetwork;
    public PlayerNetwork PlayerNetwork => playerNetwork;

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

        Debug.Log(
           $"[ColorBlockPlayer SetPlayerIndex] " +
           $"Object={Object.Id}, " +
           $"PlayerIndex={PlayerIndex}"
       );
    }

    public void SetCharacterIndex(int index)
    {
        if (!Object.HasStateAuthority)
            return;

        CharacterIndex = index;

        Debug.Log(
            $"[ColorBlockPlayer SetCharacterIndex] " +
            $"Object={Object.Id}, " +
            $"CharacterIndex={CharacterIndex}"
        );
    }

    #endregion


    #region < Movement >

    private CharacterController controller;

    private Player_InputActions inputActions;

    private Vector2 moveInput;

    private bool jumpInput;

    private float verticalVelocity;

    #endregion


    #region < Network State >

    [Networked, OnChangedRender(nameof(OnJumpingChanged))]
    private NetworkBool IsJumping { get; set; }

    [Networked, OnChangedRender(nameof(OnSpeedChanged))]
    private float NetworkSpeed { get; set; }

    #endregion


    #region < Unity >

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (controller == null)
        {
            Debug.LogError(
                "[ColorBlockPlayer] " +
                "CharacterController가 없습니다."
            );
        }
    }

    #endregion


    #region < Spawn >

    public override void Spawned()
    {
        Debug.Log(
            $"[ColorBlockPlayer Spawned] " +
            $"Object={Object.Id}, " +
            $"InputAuthority={Object.InputAuthority}, " +
            $"HasInputAuthority={Object.HasInputAuthority}, " +
            $"PlayerIndex={PlayerIndex}, " +
            $"CharacterIndex={CharacterIndex}"
        );

        ApplyCharacter();

        // 내 캐릭터만 카메라 연결 + InputActions 생성
        if (Object.HasInputAuthority)
        {
            SetupCamera();

            inputActions = new Player_InputActions();

            inputActions.ColorBlock.Enable();

            inputActions.ColorBlock.Jump.performed += OnJump;

            Debug.Log(
                "[ColorBlockPlayer] " +
                "InputActions 활성화 완료"
            );
        }

        UpdateJumpAnimation();
        UpdateSpeedAnimation();
    }

    private void SetupCamera()
    {
        if (Camera.main == null)
        {
            Debug.LogWarning(
                "[ColorBlockPlayer] " +
                "Main Camera를 찾을 수 없습니다."
            );

            return;
        }

        ColorBlockCamera cameraController = Camera.main.GetComponent<ColorBlockCamera>();

        if (cameraController == null)
        {
            Debug.LogError(
                "[ColorBlockPlayer] " +
                "Main Camera에 ColorBlockCamera가 없습니다."
            );

            return;
        }

        if (camPos == null)
        {
            Debug.LogError(
                $"[ColorBlockPlayer] CamPos가 없습니다. " +
                $"PlayerRef = {Object.InputAuthority}"
            );

            return;
        }

        cameraController.SetTarget(camPos);

        Debug.Log(
            $"[ColorBlockPlayer] " +
            $"내 카메라 연결 완료 : {Object.InputAuthority}"
        );
    }

    #endregion


    #region < Character >

    private void OnCharacterIndexChanged()
    {
        ApplyCharacter();
    }

    private void ApplyCharacter()
    {
        if (characterDatabase == null)
        {
            Debug.LogError(
                "[ColorBlockPlayer] " +
                "CharacterDatabase가 없습니다."
            );

            return;
        }

        if (CharacterIndex < 0 || CharacterIndex >= characterDatabase.characters.Length)       
            return;
       

        CharacterData characterData = characterDatabase.characters[CharacterIndex];

        if (characterData == null)
            return;

        if (characterRenderer == null)
            return;

        Material[] materials = characterRenderer.materials;

        if (materials.Length < 2)
            return;

        materials[0] = characterData.characterMaterial;

        characterRenderer.materials = materials;

        Debug.Log(
            $"[ColorBlockPlayer] " +
            $"캐릭터 적용 완료 | " +
            $"PlayerIndex={PlayerIndex}, " +
            $"CharacterIndex={CharacterIndex}, " +
            $"CharacterName={characterData.characterName}"
        );
    }

    #endregion


    #region < Despawn >

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (inputActions == null)
            return;

        inputActions.ColorBlock.Jump.performed -= OnJump;

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

        moveInput = inputActions.ColorBlock.Move.ReadValue<Vector2>();
    }

    public Vector2 GetMoveInput()
    {
        if (!Object.HasInputAuthority)
            return Vector2.zero;

        return moveInput;
    }

    public bool GetJumpInput()
    {
        if (!Object.HasInputAuthority)
            return false;

        bool value = jumpInput;
        jumpInput = false;

        return value;
    }

    private void OnJump(UnityEngine.InputSystem.InputAction.CallbackContext context)
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
        if (!Object.HasStateAuthority)
            return;

        if (GetInput(out ColorBlockInputData inputData))
        {
            moveInput = inputData.Move;

            if (inputData.Jump)
            {
                jumpInput = true;
            }
        }
        else
        {
            moveInput = Vector2.zero;
        }

        Move();
        HandleJump();
        ApplyGravity();
    }

    #endregion


    #region < Move >

    private void Move()
    {
        if (controller == null) return;

        Vector3 moveDirection =
            transform.forward * moveInput.y +
            transform.right * moveInput.x;

        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        Vector3 horizontalMovement = moveDirection * moveSpeed;

        Vector3 movement = new Vector3(
            horizontalMovement.x,
            verticalVelocity,
            horizontalMovement.z
        );

        controller.Move(movement * Runner.DeltaTime);

        float speed = moveDirection.magnitude;

        NetworkSpeed = speed;

        if (moveDirection.sqrMagnitude > 0.01f && moveInput.y >= 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Runner.DeltaTime
            );
        }
    }

    #endregion


    #region < Jump >

    private void HandleJump()
    {
        if (controller == null)
            return;

        if (!controller.isGrounded)
            return;

        // 바닥에 있을 때 아래로 계속 떨어지는 것을 방지
        if (verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (!jumpInput)
            return;

        if (IsJumping)
            return;

        verticalVelocity = jumpForce;

        IsJumping = true;

        jumpInput = false;

        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }
    }

    private void ApplyGravity()
    {
        if (controller == null)
            return;

        // 점프 중 또는 공중
        verticalVelocity += gravity * Runner.DeltaTime;

        // 착지했으면 점프 상태 종료
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;

            if (IsJumping)
            {
                IsJumping = false;
            }
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
        if (IsJumping && animator != null)
        {
            animator.SetTrigger("Jump");
        }
    }

    private void UpdateSpeedAnimation()
    {
        if (animator == null)
            return;

        animator.SetFloat("Speed", NetworkSpeed);
    }


    private void OnJumpingChanged()
    {
        UpdateJumpAnimation();
    }

    private void OnSpeedChanged()
    {
        UpdateSpeedAnimation();
    }



    #endregion
}