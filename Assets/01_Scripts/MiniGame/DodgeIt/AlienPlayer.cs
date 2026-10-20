using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class AlienPlayer : NetworkBehaviour
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


    [Header("<< Obstacle Spawn >>")]
    [SerializeField] private Transform obstacleSpawnPoint;
    [SerializeField] private NetworkObject obstacle3Prefab;
    [SerializeField] private NetworkObject obstacle4Prefab;
    [SerializeField] private NetworkObject obstacle5Prefab;

    [SerializeField] private float firstSpawnDelay = 1f;
    [SerializeField] private float nextSpawnDelay = 2f;


    private Player_InputActions inputActions;

    [Networked]
    private Vector2 NetworkMoveInput { get; set; }

    private bool dropRequested;
    private bool waitingForNextObstacle;
    private bool isSpawning;

    private DodgeItObstacle currentObstacle;


    private PlayerNetwork playerNetwork;

    public PlayerNetwork PlayerNetwork => playerNetwork;

    #region < Unity >

    private void Awake()
    {
        inputActions = new Player_InputActions();
    }

    public override void Spawned()
    {      
        FindPlayerNetwork();
        ApplyCharacter();

        if (Object.HasInputAuthority)
        {
            inputActions.DodgeIt_Alien.Enable();
            inputActions.DodgeIt_Alien.DropObstacle.performed += OnDropObstacle;
        }

        if (Object.HasStateAuthority && !isSpawning)
        {
            isSpawning = true;
            StartCoroutine(SpawnObstacleLoop());
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        DisableInput();
        StopAllCoroutines();
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

        inputActions.DodgeIt_Alien.DropObstacle.performed -= OnDropObstacle;
        inputActions.DodgeIt_Alien.Disable();
    }

    private void Update()
    {
        if (Object == null || !Object.HasInputAuthority)
            return;

        Vector2 moveInput = inputActions.DodgeIt_Alien.Move.ReadValue<Vector2>();

        RPC_SendMoveInput(moveInput);
    }

    #endregion

    #region < Input >

    private void OnDropObstacle(InputAction.CallbackContext context)
    {
        if (Object == null || !Object.HasInputAuthority)
            return;

        RPC_RequestDropObstacle();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Unreliable)]
    private void RPC_SendMoveInput(Vector2 moveInput)
    {
        NetworkMoveInput = Vector2.ClampMagnitude(moveInput, 1f);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestDropObstacle()
    {
        dropRequested = true;
    }

    #endregion

    #region < Move >

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        Vector3 movement = Vector3.right * NetworkMoveInput.x * moveSpeed;

        Vector3 nextPosition = transform.position + movement * Runner.DeltaTime;

        nextPosition.x = Mathf.Clamp(nextPosition.x, -7f, 7f);

        transform.position = nextPosition;

        if (dropRequested)
        {
            dropRequested = false;
            DropObstacle();
        }
    }

    #endregion

    #region < Obstacle >

    private IEnumerator SpawnObstacleLoop()
    {
        yield return new WaitForSeconds(firstSpawnDelay);

        while (true)
        {
            if (!SpawnObstacle())
                yield break;

            while (currentObstacle != null)
                yield return null;

            while (!waitingForNextObstacle)
                yield return null;

            yield return new WaitForSeconds(nextSpawnDelay);

            waitingForNextObstacle = false;
        }
    }

    private bool SpawnObstacle()
    {
        if (!Object.HasStateAuthority)
            return false;

        if (obstacleSpawnPoint == null)                   
            return false;
        

        int randomValue = Random.Range(0, 100);

        NetworkObject selectedPrefab;

        if (randomValue < 40)
            selectedPrefab = obstacle3Prefab;
        else if (randomValue < 80)
            selectedPrefab = obstacle4Prefab;
        else
            selectedPrefab = obstacle5Prefab;

        if (selectedPrefab == null)                   
            return false;
        

        NetworkObject obstacleObject = Runner.Spawn(
            selectedPrefab,
            obstacleSpawnPoint.position,
            obstacleSpawnPoint.rotation
        );

        if (obstacleObject == null)       
            return false;
        

        currentObstacle = obstacleObject.GetComponent<DodgeItObstacle>();

        if (currentObstacle == null)
        {    
            Runner.Despawn(obstacleObject);
            return false;
        }

        currentObstacle.SetFollowTarget(obstacleSpawnPoint);

        waitingForNextObstacle = false;

        return true;
    }

    private void DropObstacle()
    {
        if (!Object.HasStateAuthority)
            return;

        if (currentObstacle == null || waitingForNextObstacle)
            return;

        currentObstacle.Drop();

        currentObstacle = null;
        waitingForNextObstacle = true;
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
        

        PlayerNetwork player =
            playerNetworkObject.GetComponent<PlayerNetwork>();

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
}