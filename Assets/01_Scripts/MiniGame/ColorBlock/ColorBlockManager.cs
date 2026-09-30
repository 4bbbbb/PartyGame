using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public class ColorBlockManager : NetworkBehaviour
{
    [Header("<< ColorBlock Player >>")]
    [SerializeField] private NetworkPrefabRef colorBlockPlayerPrefab;

    [Header("<< Player Spawn Points >>")]
    [SerializeField] private Transform[] playerSpawnPoints;

    [Header("<< Round >>")]
    [SerializeField] private float moveTime = 3f;
    [SerializeField] private float roundEndDelay = 0f;

    [Header("<< Monitor >>")]
    [SerializeField] private MonitorUI monitorUI;

    [Header("<< Blocks >>")]
    [SerializeField] private ColorBlock[] blocks;

    [Header("<< Countdown >>")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float countdownTime = 1f;

    #region < Local Data >

    private readonly Dictionary<PlayerRef, ColorBlockPlayer> spawnedPlayers = new();

    private List<BlockCondition> currentConditions =
        new List<BlockCondition>();

    private int roundIndex = 0;
    private bool isGameRunning = false;
    private bool isInitializing = false;

    #endregion


    #region < Network >

    public override void Spawned()
    {
        Debug.Log("===== ColorBlockManager Spawned =====");

        if (!Object.HasStateAuthority)
        {
            return;
        }

        if (isInitializing)
        {
            return;
        }

        isInitializing = true;

        StartCoroutine(InitializeColorBlockGameRoutine());
    }

    private IEnumerator InitializeColorBlockGameRoutine()
    {
        // Runner 준비 대기
        yield return new WaitUntil(() => Runner != null);

        // PlayerNetwork가 생성될 때까지 대기
        yield return new WaitUntil(() => GetActivePlayers().Count > 0);

        Debug.Log("===== PlayerNetwork 확인 완료 =====");

        // ColorBlock 전용 플레이어 생성
        SpawnPlayers();

        // Spawn 직후 한 프레임 대기
        yield return null;

        // 게임 시작
        StartGame();

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
            Debug.LogError("❌ Runner가 없습니다.");
            return;
        }

        if (!colorBlockPlayerPrefab.IsValid)
        {
            Debug.LogError(
                "❌ ColorBlockPlayer Prefab이 연결되지 않았습니다."
            );

            return;
        }

        if (playerSpawnPoints == null ||
            playerSpawnPoints.Length == 0)
        {
            Debug.LogError(
                "❌ ColorBlock Player Spawn Point가 없습니다."
            );

            return;
        }

        List<PlayerNetwork> players = GetActivePlayers();

        if (players.Count == 0)
        {
            Debug.LogError(
                "❌ PlayerNetwork를 하나도 찾지 못했습니다."
            );

            return;
        }

        int spawnCount =
            Mathf.Min(
                players.Count,
                playerSpawnPoints.Length
            );

        for (int i = 0; i < spawnCount; i++)
        {
            PlayerNetwork player = players[i];

            if (player == null)
            {
                continue;
            }

            PlayerRef playerRef = player.PlayerRef;

            // 이미 생성되어 있으면 중복 생성하지 않음
            if (spawnedPlayers.ContainsKey(playerRef))
            {
                continue;
            }

            Transform spawnPoint =
                playerSpawnPoints[i];

            Debug.Log(
                $"===== ColorBlock Player Spawn =====\n" +
                $"Index : {i}\n" +
                $"PlayerRef : {playerRef}\n" +
                $"Nickname : {player.Nickname}\n" +
                $"CharacterIndex : {player.CharacterIndex}\n" +
                $"Position : {spawnPoint.position}"
            );

            NetworkObject playerObject =
                Runner.Spawn(
                    colorBlockPlayerPrefab,
                    spawnPoint.position,
                    spawnPoint.rotation,
                    playerRef
                );

            if (playerObject == null)
            {
                Debug.LogError(
                    $"❌ ColorBlockPlayer Spawn 실패 : {playerRef}"
                );

                continue;
            }

            ColorBlockPlayer colorBlockPlayer =
                playerObject.GetComponent<ColorBlockPlayer>();

            if (colorBlockPlayer == null)
            {
                Debug.LogError(
                    "❌ ColorBlockPlayer 프리팹에 " +
                    "ColorBlockPlayer 컴포넌트가 없습니다."
                );

                Runner.Despawn(playerObject);
                continue;
            }

            // 플레이어 순서
            colorBlockPlayer.SetPlayerIndex(i);

            // Lobby PlayerNetwork 연결
            colorBlockPlayer.SetPlayerNetwork(player);

            // 캐릭터 번호 전달
            colorBlockPlayer.SetCharacterIndex(
                player.CharacterIndex
            );

            spawnedPlayers.Add(
                playerRef,
                colorBlockPlayer
            );
        }
    }

    #endregion


    #region < Game >

    private void StartGame()
    {
        if (isGameRunning)
        {
            return;
        }

        StartCoroutine(GameRoutine());
    }

    private IEnumerator GameRoutine()
    {
        isGameRunning = true;
        roundIndex = 0;

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);

            countdownText.text = "3";
            yield return new WaitForSeconds(countdownTime);

            countdownText.text = "2";
            yield return new WaitForSeconds(countdownTime);

            countdownText.text = "1";
            yield return new WaitForSeconds(countdownTime);

            countdownText.text = "START";
            yield return new WaitForSeconds(1.5f);

            countdownText.gameObject.SetActive(false);
        }

        while (true)
        {
            int conditionCount =
                GetConditionCount();

            currentConditions =
                GetRandomConditions(conditionCount);

            monitorUI.ShowConditions(
                currentConditions
            );

            yield return new WaitForSeconds(moveTime);

            CheckBlocks();

            yield return new WaitForSeconds(1.7f);

            if (roundEndDelay > 0f)
            {
                yield return new WaitForSeconds(
                    roundEndDelay
                );
            }

            roundIndex++;
        }
    }

    #endregion


    #region < Condition >

    private int GetConditionCount()
    {
        if (roundIndex == 0)
            return 1;

        if (roundIndex == 1)
            return 1;

        if (roundIndex == 2)
            return 2;

        if (roundIndex == 3)
            return 2;

        if (roundIndex == 4)
            return 3;

        return Random.Range(1, 4);
    }

    private List<BlockCondition> GetRandomConditions(
        int conditionCount)
    {
        List<BlockCondition> conditions =
            new List<BlockCondition>();

        List<int> groups = new List<int>()
        {
            0, 1, 2, 3
        };

        // 그룹 섞기
        for (int i = 0; i < groups.Count; i++)
        {
            int randomIndex =
                Random.Range(i, groups.Count);

            int temp = groups[i];

            groups[i] =
                groups[randomIndex];

            groups[randomIndex] =
                temp;
        }

        for (int i = 0; i < conditionCount; i++)
        {
            int groupIndex =
                groups[i];

            ConditionType type =
                Random.value < 0.5f
                ? ConditionType.Color
                : ConditionType.Letter;

            conditions.Add(
                new BlockCondition(
                    type,
                    groupIndex
                )
            );
        }

        return conditions;
    }

    private void CheckBlocks()
    {
        foreach (ColorBlock block in blocks)
        {
            foreach (BlockCondition condition
                     in currentConditions)
            {
                if (IsMatch(block, condition))
                {
                    block.Fall();
                    break;
                }
            }
        }
    }

    private bool IsMatch(
        ColorBlock block,
        BlockCondition condition)
    {
        switch (condition.type)
        {
            case ConditionType.Color:
                return IsColorMatch(
                    block.BlockColor,
                    condition.groupIndex
                );

            case ConditionType.Letter:
                return IsLetterMatch(
                    block.BlockLetter,
                    condition.groupIndex
                );
        }

        return false;
    }

    private bool IsColorMatch(
        BlockColor color,
        int groupIndex)
    {
        return groupIndex switch
        {
            0 => color == BlockColor.Red,
            1 => color == BlockColor.Green,
            2 => color == BlockColor.Blue,
            3 => color == BlockColor.Yellow,
            _ => false
        };
    }

    private bool IsLetterMatch(
        BlockLetter letter,
        int groupIndex)
    {
        return groupIndex switch
        {
            0 => letter == BlockLetter.A,
            1 => letter == BlockLetter.B,
            2 => letter == BlockLetter.D,
            3 => letter == BlockLetter.C,
            _ => false
        };
    }

    #endregion
}