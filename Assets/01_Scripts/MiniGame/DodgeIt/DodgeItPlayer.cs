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

    [Networked]
    public NetworkBool IsEliminated { get; private set; }

    private float verticalVelocity;
    private bool isJumping;
    private bool jumpInput;

    private PlayerNetwork playerNetwork;
    private DodgeItManager dodgeItManager;

    public void SetDodgeItManager(DodgeItManager manager)
    {
        dodgeItManager = manager;
    }
    public PlayerNetwork PlayerNetwork => playerNetwork;

    #region < Unity >

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputActions = new Player_InputActions();
    }

    public override void Spawned()
    {     
        FindPlayerNetwork();

        ApplyCharacter();

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

        Vector2 moveInput = inputActions.DodgeIt.Move.ReadValue<Vector2>();

        bool jumpPressed = jumpInput;
        jumpInput = false;

        RPC_SendInput(moveInput, jumpPressed);
    }

    #endregion


    #region < Network Input >

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Unreliable)]
    private void RPC_SendInput(Vector2 moveInput, NetworkBool jumpPressed)
    {
        NetworkMoveInput = Vector2.ClampMagnitude(moveInput, 1f);

        if (jumpPressed)
        {
            NetworkJumpRequested = true;
        }
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
    }

    public void SetCharacterIndex(int index)
    {
        if (!Object.HasStateAuthority)
            return;

        CharacterIndex = index;

        ApplyCharacter();       
    }

    private void FindPlayerNetwork()
    {
        if (Object == null || Runner == null)
            return;

        NetworkObject playerNetworkObject =
            Runner.GetPlayerObject(Object.InputAuthority);

        if (playerNetworkObject == null)
            return;
        

        PlayerNetwork player = playerNetworkObject.GetComponent<PlayerNetwork>();

        if (player == null)            
            return;        

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
            return;
        

        if (CharacterIndex < 0 || CharacterIndex >= characterDatabase.characters.Length)        
            return;        

        CharacterData characterData = characterDatabase.characters[CharacterIndex];

        if (characterData == null)
            return;
        

        if (characterRenderer == null)
            return;
        

        if (characterData.characterMaterial == null)
            return;
        

        Material[] materials = characterRenderer.materials;

        if (materials.Length < 2)
            return;        

        materials[0] = characterData.characterMaterial;
        characterRenderer.materials = materials;        
    }

    #endregion


    #region < Move >

    public override void FixedUpdateNetwork()
    {
        if (IsEliminated)
            return;

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
        Vector3 movement = Vector3.right * moveInput.x * moveSpeed;

        movement.y = verticalVelocity;

        controller.Move(movement * Runner.DeltaTime);

        Vector3 position = transform.position;

        position.x = Mathf.Clamp(position.x, minX, maxX);

        transform.position = position;

        if (Mathf.Abs(moveInput.x) > 0.01f)
        {
            Vector3 lookDirection = new Vector3(moveInput.x, 0f, 0f);

            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

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


    #region < Eliminate >


    public void Eliminate()
    {
        if (!Object.HasStateAuthority || IsEliminated)
            return;

        if (dodgeItManager == null)
        {
            Debug.LogError("[Dodge It!] DodgeItManager 참조가 null입니다!");
            return;
        }

        dodgeItManager.RecordElimination(Object.InputAuthority);

        IsEliminated = true;

        Vector3 eliminatePosition = transform.position;

        Debug.Log($"[VFX] 플레이어 탈락! 위치: {eliminatePosition}");

        dodgeItManager.SpawnEliminateVFX(eliminatePosition);

        dodgeItManager.CheckAllPlayersEliminated();

        Runner.Despawn(Object);
    }


    public override void Render()
    {
        if (IsEliminated)
        {
            if (characterRenderer != null)
                characterRenderer.enabled = false;

            if (nameText != null)
                nameText.gameObject.SetActive(false);
        }
    }

    #endregion

}