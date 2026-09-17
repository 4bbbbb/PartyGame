using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public class BaseballManager : NetworkBehaviour
{
    public static BaseballManager Instance { get; private set; }

    [Header("<< Ball Spawn >>")]
    [SerializeField] private BallSpawnManager ballSpawnManager;

    [Header("<< Baseball Player >>")]
    [SerializeField] private NetworkPrefabRef baseballPlayerPrefab;
    [SerializeField] private Transform[] playerSpawnPoints;

    [Header("<< Start UI >>")]
    [SerializeField] private GameObject startText;

    [Header("<< Practice UI >>")]
    [SerializeField] private GameObject practiceText;

    [Header("<< Game Setting >>")]
    [SerializeField] private float startMessageDuration = 1.5f;

    #region < Local Data >

    // State Authority에서 생성한 BaseballPlayer 목록
    private readonly Dictionary<PlayerRef, BaseballPlayer> spawnedPlayers = new();

    private bool isGameStarted;
    private bool isInitializing;

    #endregion

    #region < Networked UI >

    [Networked, OnChangedRender(nameof(OnPracticeTextChanged))]
    private NetworkBool IsPracticeTextVisible { get; set; }

    [Networked, OnChangedRender(nameof(OnStartTextChanged))]
    private NetworkBool IsStartTextVisible { get; set; }

    #endregion

    #region < Instance >

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    #endregion


    #region < Network >

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            if (isInitializing)
                return;

            isInitializing = true;

            IsPracticeTextVisible = false;
            IsStartTextVisible = false;

            StartCoroutine(InitializeBaseballGameRoutine());
        }

        UpdatePracticeText();
        UpdateStartText();
    }

    private IEnumerator InitializeBaseballGameRoutine()
    {
        // Runner가 준비될 때까지 대기
        yield return new WaitUntil(() => Runner != null);

        // PlayerNetwork가 생성될 때까지 대기
        yield return new WaitUntil(() => GetActivePlayers().Count > 0);

        SpawnPlayers();

        yield return null;

        StartBaseballGame();

        isInitializing = false;
    }

    #endregion


    #region < Player >

    private List<PlayerNetwork> GetActivePlayers()
    {
        List<PlayerNetwork> players = new();

        if (Runner == null)
        {
            return players;
        }

        foreach (PlayerRef playerRef in Runner.ActivePlayers)
        {
            NetworkObject playerObject =
                Runner.GetPlayerObject(playerRef);

            if (playerObject == null)
            {
                Debug.LogWarning(
                    $"PlayerObject를 찾을 수 없습니다. " +
                    $"PlayerRef = {playerRef}"
                );

                continue;
            }

            PlayerNetwork playerNetwork =
                playerObject.GetComponent<PlayerNetwork>();

            if (playerNetwork == null)
            {
                Debug.LogError(
                    $"PlayerObject에 PlayerNetwork가 없습니다. " +
                    $"PlayerRef = {playerRef}"
                );

                continue;
            }

            players.Add(playerNetwork);
        }

        return players
            .OrderBy(player => player.PlayerRef.RawEncoded)
            .ToList();
    }

    #endregion


    #region < Spawn >

    private void SpawnPlayers()
    {
        if (Runner == null)
        {
            Debug.LogError("Runner가 없습니다.");
            return;
        }

        if (!baseballPlayerPrefab.IsValid)
        {
            Debug.LogError(
                "BaseballPlayer Prefab이 연결되지 않았습니다."
            );

            return;
        }

        if (playerSpawnPoints == null || playerSpawnPoints.Length < 4)
        {
            Debug.LogError("BaseballPlayer Spawn Point가 4개 필요합니다.");

            return;
        }

        List<PlayerNetwork> players = GetActivePlayers();

        if (players.Count == 0)
        {
            Debug.LogError(
                "PlayerNetwork를 하나도 찾지 못했습니다."
            );

            return;
        }

        int spawnCount = Mathf.Min(players.Count, playerSpawnPoints.Length);

        for (int i = 0; i < spawnCount; i++)
        {
            PlayerNetwork player = players[i];

            if (player == null)
            {
                continue;
            }

            PlayerRef playerRef = player.PlayerRef;

            // 같은 PlayerRef로 이미 생성된 경우 중복 생성하지 않음
            if (spawnedPlayers.ContainsKey(playerRef))
            {
                continue;
            }

            Transform spawnPoint = playerSpawnPoints[i];

            Debug.Log(
                $"[Baseball Spawn] " +
                $"Index={i}, " +
                $"PlayerRef={playerRef}, " +
                $"Nickname={player.Nickname}, " +
                $"CharacterIndex={player.CharacterIndex}"
            );

            NetworkObject playerObject = Runner.Spawn(
                baseballPlayerPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                playerRef
            );

            if (playerObject == null)
            {
                Debug.LogError(
                    $"BaseballPlayer Spawn 실패 : {playerRef}"
                );

                continue;
            }

            BaseballPlayer baseballPlayer = playerObject.GetComponent<BaseballPlayer>();

            if (baseballPlayer == null)
            {
                Debug.LogError(
                    "BaseballPlayer 프리팹에 " +
                    "BaseballPlayer 컴포넌트가 없습니다."
                );

                Runner.Despawn(playerObject);
                continue;
            }

            // 위치와 크기 설정
            playerObject.transform.localScale = spawnPoint.lossyScale;

            // 각 플레이어의 순서 번호를 네트워크 변수에 설정
            baseballPlayer.SetPlayerIndex(i);

            // PlayerNetwork 연결
            baseballPlayer.SetPlayerNetwork(player);

            // 각 플레이어의 캐릭터 번호를 BaseballPlayer에 동기화
            baseballPlayer.SetCharacterIndex(player.CharacterIndex);

            spawnedPlayers.Add(playerRef, baseballPlayer);
        }
    }

    #endregion


    #region < Baseball Game >

    private void StartBaseballGame()
    {
        isGameStarted = false;

        IsStartTextVisible = false;
        IsPracticeTextVisible = false;

        StartCoroutine(StartBaseballGameRoutine());
    }

    private IEnumerator StartBaseballGameRoutine()
    {
        if (ballSpawnManager == null)
            yield break;

        // 모든 클라이언트에서 연습 안내 텍스트 표시
        IsPracticeTextVisible = true;

        // 연습구 시작
        ballSpawnManager.BeginPractice();

        // 연습구 종료까지 대기
        yield return new WaitUntil(() => !ballSpawnManager.IsPracticeRunning);

        // 연습구 종료 후 잠시 대기
        yield return new WaitForSeconds(1f);

        // 모든 클라이언트에서 연습 안내 텍스트 숨김
        IsPracticeTextVisible = false;

        // 모든 클라이언트에서 START 텍스트 표시
        IsStartTextVisible = true;

        yield return new WaitForSeconds(startMessageDuration);

        // 모든 클라이언트에서 START 텍스트 숨김
        IsStartTextVisible = false;

        // 실제 게임 시작
        isGameStarted = true;

        ballSpawnManager.BeginGame();
    }

    #endregion


    #region < UI >
    private void OnPracticeTextChanged()
    {
        UpdatePracticeText();
    }

    private void OnStartTextChanged()
    {
        UpdateStartText();
    }

    private void UpdatePracticeText()
    {
        if (practiceText == null)
            return;

        practiceText.SetActive(IsPracticeTextVisible);
    }

    private void UpdateStartText()
    {
        if (startText == null)
            return;

        startText.SetActive(IsStartTextVisible);
    }
    #endregion
}