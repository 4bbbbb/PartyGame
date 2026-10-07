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

    [Networked, Capacity(4)]
    private NetworkArray<int> PlayerBaseballCounts => default;



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
        yield return new WaitUntil(() => Runner != null);

        yield return new WaitUntil(() => GetActivePlayers().Count > 0);

        SpawnPlayers();

        for (int i = 0; i < 4; i++)
        {
            PlayerBaseballCounts.Set(i, 0);
        }

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
            NetworkObject playerObject = Runner.GetPlayerObject(playerRef);

            if (playerObject == null)
            {
                Debug.LogWarning(
                    $"PlayerObject를 찾을 수 없습니다. " +
                    $"PlayerRef = {playerRef}"
                );

                continue;
            }

            PlayerNetwork playerNetwork = playerObject.GetComponent<PlayerNetwork>();

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

    private int GetPlayerIndex(PlayerRef playerRef)
    {
        foreach (KeyValuePair<PlayerRef, BaseballPlayer> pair in spawnedPlayers)
        {
            if (pair.Key != playerRef)
                continue;

            return pair.Value.PlayerIndex;
        }

        return -1;
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


    #region < Score >

    public int GetBaseballCount(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= 4)
            return 0;

        return PlayerBaseballCounts[playerIndex];
    }

    public void AddBaseballCount(int playerIndex, int amount)
    {
        if (!Object.HasStateAuthority)
            return;

        if (playerIndex < 0 || playerIndex >= 4)
            return;

        PlayerBaseballCounts.Set(
            playerIndex,
            PlayerBaseballCounts[playerIndex] + amount
        );

        Debug.Log(
            $"[Baseball Score] " +
            $"PlayerIndex={playerIndex}, " +
            $"Add={amount}, " +
            $"Total={PlayerBaseballCounts[playerIndex]}"
        );
    }

    private Dictionary<PlayerRef, int> CalculateGameScores()
    {
        Dictionary<PlayerRef, int> scores = new();

        List<PlayerNetwork> players =
            GetActivePlayers();

        if (players.Count == 0)
            return scores;

        players = players
            .OrderByDescending(player =>
                PlayerBaseballCounts[
                    GetPlayerIndex(player.PlayerRef)
                ]
            )
            .ToList();

        int rankGroup = 0;
        int previousScore = int.MinValue;

        for (int i = 0; i < players.Count; i++)
        {
            PlayerNetwork player = players[i];

            int baseballScore =
                PlayerBaseballCounts[
                    GetPlayerIndex(player.PlayerRef)
                ];

            if (baseballScore != previousScore)
            {
                rankGroup++;
                previousScore = baseballScore;
            }

            int gameScore;

            switch (rankGroup)
            {
                case 1:
                    gameScore = 3;
                    break;

                case 2:
                    gameScore = 2;
                    break;

                default:
                    gameScore = 1;
                    break;
            }

            scores[player.PlayerRef] = gameScore;

            Debug.Log(
                $"[Baseball Rank] " +
                $"Player={player.PlayerRef}, " +
                $"BaseballScore={baseballScore}, " +
                $"RankGroup={rankGroup}, " +
                $"GameScore={gameScore}"
            );
        }

        return scores;
    }

    private void GiveGameScores()
    {
        Dictionary<PlayerRef, int> scores =
            CalculateGameScores();

        List<PlayerNetwork> players =
            GetActivePlayers();

        foreach (KeyValuePair<PlayerRef, int> score in scores)
        {
            PlayerNetwork player =
                players.FirstOrDefault(
                    p => p.PlayerRef == score.Key
                );

            if (player == null)
            {
                Debug.LogWarning(
                    $"[BaseballManager] 점수를 지급할 PlayerNetwork를 찾을 수 없습니다. " +
                    $"PlayerRef={score.Key}"
                );

                continue;
            }

            int playerIndex =
                GetPlayerIndex(score.Key);

            int baseballCount = 0;

            if (playerIndex >= 0)
            {
                baseballCount =
                    PlayerBaseballCounts[playerIndex];
            }

            player.AddScore(score.Value);

            Debug.Log(
                $"===== Baseball 점수 지급 =====\n" +
                $"Player : {score.Key}\n" +
                $"PlayerIndex : {playerIndex}\n" +
                $"BaseballCount : {baseballCount}\n" +
                $"게임 점수 : +{score.Value}\n" +
                $"누적 Score : {player.Score}"
            );
        }
    }

    // =========================================================
    // GAME RESULT
    // =========================================================

    public void StartGameResult()
    {
        if (!Object.HasStateAuthority)
            return;

        if (!isGameStarted)
            return;

        // 게임 종료
        isGameStarted = false;

        // 야구 점수 → 전체 Score에 반영
        GiveGameScores();

        // 3초 후 Score 씬으로 이동
        StartCoroutine(GoToScoreSceneSequence());
    }

    private IEnumerator GoToScoreSceneSequence()
    {
        if (!Object.HasStateAuthority)
            yield break;

        Debug.Log(
            "[BaseballManager] 게임 종료 - 3초 후 Score 씬으로 이동합니다."
        );

        yield return new WaitForSeconds(3f);

        const int SCORE_SCENE_INDEX = 4;

        Debug.Log(
            $"[BaseballManager] Score 씬 이동 | SceneIndex={SCORE_SCENE_INDEX}"
        );

        Runner.LoadScene(
            SceneRef.FromIndex(SCORE_SCENE_INDEX)
        );
    }

    #endregion
}