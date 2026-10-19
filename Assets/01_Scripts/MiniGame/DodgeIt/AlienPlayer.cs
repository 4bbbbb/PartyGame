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
        Debug.Log(
            $"[AlienPlayer] Spawned | " +
            $"InputAuthority={Object.HasInputAuthority} | " +
            $"StateAuthority={Object.HasStateAuthority}"
        );

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

        Vector2 moveInput =
            inputActions.DodgeIt_Alien.Move.ReadValue<Vector2>();

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

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority,
        Channel = RpcChannel.Unreliable
    )]
    private void RPC_SendMoveInput(Vector2 moveInput)
    {
        NetworkMoveInput = Vector2.ClampMagnitude(moveInput, 1f);
    }

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
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

        Vector3 movement =
            Vector3.right * NetworkMoveInput.x * moveSpeed;

        Vector3 nextPosition =
            transform.position + movement * Runner.DeltaTime;

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
        {
            Debug.LogError(
                "[AlienPlayer] Obstacle Spawn Point가 할당되지 않았습니다."
            );
            return false;
        }

        int randomValue = Random.Range(0, 100);

        NetworkObject selectedPrefab;

        if (randomValue < 40)
            selectedPrefab = obstacle3Prefab;
        else if (randomValue < 80)
            selectedPrefab = obstacle4Prefab;
        else
            selectedPrefab = obstacle5Prefab;

        if (selectedPrefab == null)
        {
            Debug.LogError(
                "[AlienPlayer] 선택된 장애물 프리팹이 할당되지 않았습니다."
            );
            return false;
        }

        NetworkObject obstacleObject = Runner.Spawn(
            selectedPrefab,
            obstacleSpawnPoint.position,
            obstacleSpawnPoint.rotation
        );

        if (obstacleObject == null)
        {
            Debug.LogError("[AlienPlayer] 네트워크 장애물 스폰 실패!");
            return false;
        }

        currentObstacle =
            obstacleObject.GetComponent<DodgeItObstacle>();

        if (currentObstacle == null)
        {
            Debug.LogError(
                "[AlienPlayer] 장애물 프리팹에 DodgeItObstacle이 없습니다."
            );

            Runner.Despawn(obstacleObject);
            return false;
        }

        currentObstacle.SetFollowTarget(obstacleSpawnPoint);

        waitingForNextObstacle = false;

        Debug.Log("[AlienPlayer] 네트워크 장애물 생성 완료");
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

        Debug.Log("[AlienPlayer] 장애물 투하 처리 완료");
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
            $"[AlienPlayer SetPlayerIndex] " +
            $"Object={Object.Id}, PlayerIndex={PlayerIndex}"
        );
    }

    public void SetCharacterIndex(int index)
    {
        if (!Object.HasStateAuthority)
            return;

        CharacterIndex = index;
        ApplyCharacter();

        Debug.Log(
            $"[AlienPlayer SetCharacterIndex] " +
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
                $"[AlienPlayer] PlayerNetwork를 찾을 수 없습니다: " +
                $"{Object.InputAuthority}"
            );
            return;
        }

        PlayerNetwork player =
            playerNetworkObject.GetComponent<PlayerNetwork>();

        if (player == null)
        {
            Debug.LogWarning(
                "[AlienPlayer] PlayerNetwork 컴포넌트가 없습니다."
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
                "[AlienPlayer] CharacterDatabase가 연결되지 않았습니다."
            );
            return;
        }

        if (CharacterIndex < 0 ||
            CharacterIndex >= characterDatabase.characters.Length)
        {
            Debug.LogWarning(
                $"[AlienPlayer] 유효하지 않은 CharacterIndex: {CharacterIndex}"
            );
            return;
        }

        CharacterData characterData =
            characterDatabase.characters[CharacterIndex];

        if (characterData == null)
        {
            Debug.LogError(
                $"[AlienPlayer] CharacterData가 비어 있습니다: {CharacterIndex}"
            );
            return;
        }

        if (characterRenderer == null)
        {
            Debug.LogError(
                "[AlienPlayer] Character Renderer가 연결되지 않았습니다."
            );
            return;
        }

        if (characterData.characterMaterial == null)
        {
            Debug.LogError(
                "[AlienPlayer] 캐릭터 머티리얼이 없습니다."
            );
            return;
        }

        Material[] materials = characterRenderer.materials;

        if (materials.Length < 2)
        {
            Debug.LogError(
                "[AlienPlayer] Renderer의 머티리얼 슬롯이 2개 미만입니다."
            );
            return;
        }

        materials[0] = characterData.characterMaterial;
        characterRenderer.materials = materials;

        Debug.Log(
            $"[AlienPlayer] 캐릭터 외형 적용 완료: " +
            $"{characterData.characterName}"
        );
    }

    #endregion
}