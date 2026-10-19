using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class DodgeItPlayer : NetworkBehaviour
{
    [Header("<< Character >>")]
    [SerializeField] private Renderer characterRenderer;

    [Header("<< Character Database >>")]
    [SerializeField] private CharacterDatabase characterDatabase;

    [Header("<< Name >>")]
    [SerializeField] private TMPro.TextMeshProUGUI nameText;

    [Networked]
    public int PlayerIndex { get; private set; }

    [Networked, OnChangedRender(nameof(OnCharacterIndexChanged))]
    public int CharacterIndex { get; private set; }

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

    [Networked]
    private Vector2 NetworkMoveInput { get; set; }

    [Networked]
    private NetworkBool NetworkJumpRequested { get; set; }

    private float verticalVelocity;
    private bool isJumping;
    private bool jumpInput;

    private PlayerNetwork playerNetwork;

    public PlayerNetwork PlayerNetwork => playerNetwork;

    #region < Unity >

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputActions = new Player_InputActions();
    }

    public override void Spawned()
    {
        Debug.Log(
            $"[DodgeItPlayer] Spawned | " +
            $"InputAuthority={Object.HasInputAuthority} | " +
            $"StateAuthority={Object.HasStateAuthority}"
        );

        // 각 클라이언트에서 해당 캐릭터의 PlayerNetwork를 찾는다.
        FindPlayerNetwork();

        // 스폰 직후에도 외형을 적용한다.
        ApplyCharacter();

        // 내 캐릭터만 입력을 받는다.
        if (Object.HasInputAuthority)
        {
            inputActions.DodgeIt.Enable();
            inputActions.DodgeIt.Jump.performed += OnJump;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        DisableInput();
    }

    private void OnDestroy()
    {
        DisableInput();
        inputActions?.Dispose();
    }

    private void DisableInput()
    {
        if (inputActions == null)
            return;

        inputActions.DodgeIt.Jump.performed -= OnJump;
        inputActions.DodgeIt.Disable();
    }

    private void Update()
    {
        if (Object == null || !Object.HasInputAuthority)
            return;

        Vector2 moveInput =
            inputActions.DodgeIt.Move.ReadValue<Vector2>();

        bool jumpPressed = jumpInput;
        jumpInput = false;

        RPC_SendInput(moveInput, jumpPressed);
    }

    #endregion

    #region < Network Input >

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority,
        Channel = RpcChannel.Unreliable
    )]
    private void RPC_SendInput(
        Vector2 moveInput,
        NetworkBool jumpPressed)
    {
        NetworkMoveInput = Vector2.ClampMagnitude(moveInput, 1f);

        if (jumpPressed)
            NetworkJumpRequested = true;
    }

    #endregion

    #region < Move >

    public override void FixedUpdateNetwork()
    {
        // 실제 이동은 State Authority에서만 처리한다.
        if (!Object.HasStateAuthority)
            return;

        if (controller == null)
            return;

        Vector2 moveInput = NetworkMoveInput;

        HandleJump();
        Move(moveInput);
        UpdateSpeedAnimation(moveInput);
    }

    private void Move(Vector2 moveInput)
    {
        Vector3 movement =
            Vector3.right * moveInput.x * moveSpeed;

        movement.y = verticalVelocity;

        controller.Move(movement * Runner.DeltaTime);

        // 좌우 이동 범위 제한
        Vector3 position = transform.position;

        position.x = Mathf.Clamp(position.x, minX, maxX);

        transform.position = position;

        // 이동 방향 바라보기
        if (Mathf.Abs(moveInput.x) > 0.01f)
        {
            Vector3 lookDirection =
                new Vector3(moveInput.x, 0f, 0f);

            Quaternion targetRotation =
                Quaternion.LookRotation(lookDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Runner.DeltaTime
            );
        }
    }

    #endregion

    #region < Jump >

    private void OnJump(InputAction.CallbackContext context)
    {
        jumpInput = true;
    }

    private void HandleJump()
    {
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (NetworkJumpRequested)
            {
                verticalVelocity = jumpForce;
                isJumping = true;

                UpdateJumpAnimation();
            }
        }
        else
        {
            verticalVelocity += gravity * Runner.DeltaTime;
        }

        // 점프 요청 소비
        NetworkJumpRequested = false;
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
            animator.SetTrigger("Jump");
    }

    private void UpdateSpeedAnimation(Vector2 moveInput)
    {
        if (animator != null)
            animator.SetFloat("Speed", moveInput.magnitude);
    }

    #endregion

    #region < Player Network >

    public void SetPlayerNetwork(PlayerNetwork player)
    {
        playerNetwork = player;
        UpdateName();
    }

    public void SetPlayerIndex(int index)
    {
        if (!Object.HasStateAuthority)
            return;

        PlayerIndex = index;

        Debug.Log(
            $"[DodgeItPlayer SetPlayerIndex] " +
            $"Object={Object.Id}, PlayerIndex={PlayerIndex}"
        );
    }

    public void SetCharacterIndex(int index)
    {
        if (!Object.HasStateAuthority)
            return;

        CharacterIndex = index;

        // 초기 설정 시에도 외형을 즉시 적용한다.
        ApplyCharacter();

        Debug.Log(
            $"[DodgeItPlayer SetCharacterIndex] " +
            $"Object={Object.Id}, CharacterIndex={CharacterIndex}"
        );
    }

    private void FindPlayerNetwork()
    {
        if (Object == null || Runner == null)
            return;

        NetworkObject playerNetworkObject =
            Runner.GetPlayerObject(Object.InputAuthority);

        if (playerNetworkObject == null)
        {
            Debug.LogWarning(
                $"[DodgeItPlayer] PlayerNetwork를 찾을 수 없습니다. " +
                $"InputAuthority={Object.InputAuthority}"
            );
            return;
        }

        PlayerNetwork player =
            playerNetworkObject.GetComponent<PlayerNetwork>();

        if (player == null)
        {
            Debug.LogWarning(
                "[DodgeItPlayer] PlayerNetwork 컴포넌트가 없습니다."
            );
            return;
        }

        SetPlayerNetwork(player);
    }

    private void UpdateName()
    {
        if (playerNetwork == null || nameText == null)
            return;

        nameText.text = playerNetwork.Nickname.ToString();
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
                "[DodgeItPlayer] CharacterDatabase가 연결되지 않았습니다."
            );
            return;
        }

        if (CharacterIndex < 0 ||
            CharacterIndex >= characterDatabase.characters.Length)
        {
            Debug.LogWarning(
                $"[DodgeItPlayer] 유효하지 않은 CharacterIndex: {CharacterIndex}"
            );
            return;
        }

        CharacterData characterData =
            characterDatabase.characters[CharacterIndex];

        if (characterData == null)
        {
            Debug.LogError(
                $"[DodgeItPlayer] CharacterData가 비어 있습니다. " +
                $"Index={CharacterIndex}"
            );
            return;
        }

        if (characterRenderer == null)
        {
            Debug.LogError(
                "[DodgeItPlayer] Character Renderer가 연결되지 않았습니다."
            );
            return;
        }

        if (characterData.characterMaterial == null)
        {
            Debug.LogError(
                $"[DodgeItPlayer] 캐릭터 머티리얼이 없습니다. " +
                $"Index={CharacterIndex}"
            );
            return;
        }

        Material[] materials = characterRenderer.materials;

        if (materials.Length < 2)
        {
            Debug.LogError(
                "[DodgeItPlayer] Renderer의 머티리얼 슬롯이 2개 미만입니다."
            );
            return;
        }

        materials[0] = characterData.characterMaterial;
        characterRenderer.materials = materials;

        Debug.Log(
            $"[DodgeItPlayer] 캐릭터 외형 적용 완료: " +
            $"{characterData.characterName}"
        );
    }

    #endregion
}