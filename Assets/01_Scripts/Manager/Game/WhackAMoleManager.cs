using Fusion;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WhackAMoleManager : NetworkBehaviour
{
    #region < Data >

    [Header("<< Game Data >>")]
    [SerializeField] private WhackAMoleData gameData;

    [Header("<< Player >>")]
    [SerializeField] private NetworkObject playerPrefab;

    [Header("<< Character >>")]
    [SerializeField] private CharacterDatabase characterDatabase;

    [Header("<< Spawn Points >>")]
    [SerializeField] private Transform tagSpawnPoint;
    [SerializeField] private Transform[] playerSpawnPoints;

    [Header("<< Tag Holes >>")]
    [SerializeField] private Transform[] tagHolePoints;

    #endregion


    #region < Enum >

    public enum HoleType
    {
        W,
        A,
        S,
        D
    }

    #endregion


    #region < Networked Data >

    // 현재 라운드
    [Networked]
    public int CurrentRound { get; private set; }

    // 술래의 현재 목숨
    [Networked]
    public int TagHP { get; private set; }

    // 술래
    [Networked]
    public PlayerRef TagPlayer { get; private set; }

    // 현재 게임 상태
    [Networked]
    public WhackAMoleState State { get; private set; }

    // 술래가 선택한 구멍
    [Networked]
    private HoleType TagHole { get; set; }

    // 일반 플레이어들의 선택
    [Networked, Capacity(4)]
    private NetworkArray<HoleType> PlayerChoices => default;

    #endregion


    #region < Local Data >

    // 실제 게임에 Spawn된 캐릭터
    private Dictionary<PlayerRef, WhackAMolePlayer> spawnedPlayers =
        new Dictionary<PlayerRef, WhackAMolePlayer>();

    #endregion


    #region < Network >

    /// <summary>
    /// 현재 방에 존재하는 PlayerNetwork 목록을 가져온다.
    /// Fusion의 PlayerObject 연결을 사용한다.
    /// </summary>
    private List<PlayerNetwork> GetActivePlayers()
    {
        List<PlayerNetwork> players = new List<PlayerNetwork>();

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

    public override void Spawned()
    {
        Debug.Log("===== WhackAMoleManager Spawned =====");

        if (!Object.HasStateAuthority)
            return;

        InitializeGame();
    }


    /// <summary>
    /// 게임에서 사용할 실제 플레이어 캐릭터를 생성한다.
    /// SpawnPoint의 Position / Rotation / Scale을 적용한다.
    /// </summary>
    private void SpawnPlayers()
    {
        Debug.Log("===== WhackAMole 플레이어 Spawn =====");

        if (Runner == null)
        {
            Debug.LogError("Runner가 없습니다.");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError(
                "WhackAMole Player Prefab이 연결되지 않았습니다."
            );
            return;
        }

        if (characterDatabase == null)
        {
            Debug.LogError(
                "CharacterDatabase가 연결되지 않았습니다."
            );
            return;
        }

        List<PlayerNetwork> players = GetActivePlayers();

        Debug.Log($"현재 플레이어 수 : {players.Count}");

        if (players.Count == 0)
        {
            Debug.LogError(
                "PlayerNetwork를 하나도 찾지 못했습니다."
            );
            return;
        }


        // -----------------------------
        // 플레이어 Spawn
        // -----------------------------

        for (int i = 0; i < players.Count; i++)
        {
            PlayerNetwork player = players[i];

            Debug.Log(
                $"게임 플레이어 확인 : " +
                $"PlayerRef = {player.PlayerRef}, " +
                $"CharacterIndex = {player.CharacterIndex}"
            );


            // 이미 생성된 플레이어라면 다시 생성하지 않는다.
            if (spawnedPlayers.ContainsKey(player.PlayerRef))
            {
                Debug.Log(
                    $"이미 Spawn된 플레이어입니다 : {player.PlayerRef}"
                );

                continue;
            }


            // -----------------------------
            // Spawn Point 결정
            // -----------------------------

            Transform spawnPoint;

            if (player.PlayerRef == TagPlayer)
            {
                // TAG
                spawnPoint = tagSpawnPoint;

                Debug.Log(
                    $"TAG 플레이어 Spawn : {player.PlayerRef}"
                );
            }
            else
            {
                // 일반 플레이어
                int normalPlayerIndex =
                    GetNormalPlayerIndex(players, player);

                if (normalPlayerIndex < 0 ||
                    normalPlayerIndex >= playerSpawnPoints.Length)
                {
                    Debug.LogError(
                        $"일반 플레이어 Spawn Point가 부족합니다. " +
                        $"Player = {player.PlayerRef}"
                    );

                    continue;
                }

                spawnPoint =
                    playerSpawnPoints[normalPlayerIndex];

                Debug.Log(
                    $"일반 플레이어 Spawn : " +
                    $"{player.PlayerRef} / " +
                    $"Position Index = {normalPlayerIndex}"
                );
            }


            // -----------------------------
            // 실제 게임 캐릭터 Spawn
            // -----------------------------

            NetworkObject playerObject = Runner.Spawn(
                playerPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                player.PlayerRef
            );

            if (playerObject == null)
            {
                Debug.LogError(
                    $"게임 플레이어 Spawn 실패 : " +
                    $"{player.PlayerRef}"
                );

                continue;
            }


            // -----------------------------
            // Spawn Point Scale 적용
            // -----------------------------

            playerObject.transform.localScale =
                spawnPoint.lossyScale;


            // -----------------------------
            // WhackAMolePlayer 확인
            // -----------------------------

            WhackAMolePlayer whackAMolePlayer =
                playerObject.GetComponent<WhackAMolePlayer>();

            if (whackAMolePlayer == null)
            {
                Debug.LogError(
                    "WhackAMolePlayer 컴포넌트를 찾을 수 없습니다."
                );

                continue;
            }


            // -----------------------------
            // 캐릭터 설정
            // -----------------------------

            whackAMolePlayer.SetCharacterIndex(
                player.CharacterIndex
            );


            // -----------------------------
            // 로컬 Dictionary 저장
            // -----------------------------

            spawnedPlayers.Add(
                player.PlayerRef,
                whackAMolePlayer
            );


            Debug.Log(
                $"게임 플레이어 Spawn 완료 : " +
                $"Player = {player.PlayerRef}, " +
                $"CharacterIndex = {player.CharacterIndex}"
            );
        }


        Debug.Log(
            $"===== WhackAMole 플레이어 Spawn 완료 =====\n" +
            $"Spawned Player 수 : {spawnedPlayers.Count}"
        );
    }


    /// <summary>
    /// 일반 플레이어 중 몇 번째인지 찾는다.
    /// TAG는 제외한다.
    /// </summary>
    private int GetNormalPlayerIndex(
        List<PlayerNetwork> players,
        PlayerNetwork targetPlayer)
    {
        int index = 0;

        foreach (PlayerNetwork player in players)
        {
            if (player.PlayerRef == TagPlayer)
                continue;

            if (player.PlayerRef == targetPlayer.PlayerRef)
                return index;

            index++;
        }

        return -1;
    }

    #endregion


    #region < Initialize >

    private void InitializeGame()
    {
        if (gameData == null)
        {
            Debug.LogError(
                "WhackAMoleData가 연결되지 않았습니다."
            );
            return;
        }

        CurrentRound = 1;
        TagHP = gameData.TagHP;
        State = WhackAMoleState.Waiting;

        Debug.Log(
            $"===== WhackAMole 초기화 =====\n" +
            $"HP : {TagHP}\n" +
            $"Round : {CurrentRound}"
        );
    }

    #endregion


    #region < Start >

    public void SetTagPlayer(PlayerRef tagPlayer)
    {
        if (!Object.HasStateAuthority)
            return;

        TagPlayer = tagPlayer;

        Debug.Log(
            $"===== WhackAMole 두더지 결정 =====\n" +
            $"두더지 : {TagPlayer}"
        );
    }


    public void StartGame()
    {
        if (!Object.HasStateAuthority)
            return;

        if (TagPlayer == default)
        {
            Debug.LogWarning(
                "TagPlayer가 설정되지 않았습니다."
            );
            return;
        }

        State = WhackAMoleState.TagSelecting;

        Debug.Log(
            $"===== WhackAMole 시작 =====\n" +
            $"Tag Player : {TagPlayer}\n" +
            $"Round : {CurrentRound}\n" +
            $"HP : {TagHP}"
        );

        SpawnPlayers();
    }

    #endregion
}