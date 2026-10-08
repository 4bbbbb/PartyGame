using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ColorBlockManager : NetworkBehaviour
{
    [Header("<< ColorBlock Player >>")]
    [SerializeField] private NetworkPrefabRef colorBlockPlayerPrefab;

    [Header("<< Player Spawn Points >>")]
    [SerializeField] private Transform[] playerSpawnPoints;

    [Header("<< Round >>")]
    [SerializeField] private float moveTime = 3f;

    [Header("<< Round Difficulty >>")]
    [SerializeField] private float moveTimeDecrease = 0.3f;
    [SerializeField] private float minimumMoveTime = 1.5f;

    [Header("<< Monitor >>")]
    [SerializeField] private MonitorUI monitorUI;

    [Header("<< Blocks >>")]
    [SerializeField] private ColorBlock[] blocks;

    [Header("<< Countdown >>")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float countdownTime = 1f;
   
    [Header("<< Elimination >>")]
    [SerializeField] private float fallDeathY = -5f;

    [Header("<< Game End")]
    [SerializeField] private GameObject gameEndText;
    [SerializeField] private RawImage freezeFrame;
    public float FallDeathY => fallDeathY;

    #region < Local Data >

    private readonly Dictionary<PlayerRef, ColorBlockPlayer> spawnedPlayers = new();
    private readonly Dictionary<PlayerRef, int> eliminatedRounds = new();

    private List<BlockCondition> currentConditions =  new List<BlockCondition>();

    private int roundIndex = 0;
    private bool isGameRunning = false;
    private bool isInitializing = false;

    [Networked]
    private NetworkBool IsGameEnding { get; set; }

    public bool GameEnding => IsGameEnding;

    #endregion


    #region < Network >

    public override void Spawned()
    {
        Debug.Log("===== ColorBlockManager Spawned =====");

        // 모든 플레이어가 ColorBlock 입력을 제공할 수 있도록 설정
        if (Runner != null)
        {
            Runner.ProvideInput = true;

            Debug.Log(
                $"[ColorBlockManager] ProvideInput = true | " +
                $"LocalPlayer = {Runner.LocalPlayer}"
            );
        }

        // 아래부터는 기존대로 State Authority만 게임을 진행
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


    #region < Network Input >

    private void EnableColorBlockInput()
    {
        if (Runner == null)
            return;

        Runner.ProvideInput = true;

        Debug.Log("[ColorBlockManager] ColorBlock Network Input 활성화");
    }

    private void DisableColorBlockInput()
    {
        if (Runner == null)
            return;

        Runner.ProvideInput = false;

        Debug.Log("[ColorBlockManager] ColorBlock Network Input 비활성화");
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

    public ColorBlockPlayer GetLocalPlayer()
    {
        if (Runner == null)
            return null;

        ColorBlockPlayer[] players =
            FindObjectsByType<ColorBlockPlayer>(FindObjectsSortMode.None);

        foreach (ColorBlockPlayer player in players)
        {
            if (player.Object != null && player.Object.HasInputAuthority)
            {
                return player;
            }
        }

        return null;
    }

    #endregion


    #region < Spawn >

    private void SpawnPlayers()
    {
        if (Runner == null)
        {
            Debug.LogError(" Runner가 없습니다.");
            return;
        }

        if (!colorBlockPlayerPrefab.IsValid)
        {
            Debug.LogError(" ColorBlockPlayer Prefab이 연결되지 않았습니다.");

            return;
        }

        if (playerSpawnPoints == null || playerSpawnPoints.Length < 4)
        {
            Debug.LogError(" ColorBlock Player Spawn Point가 4개 필요합니다.");

            return;
        }

        List<PlayerNetwork> players = GetActivePlayers();

        if (players.Count == 0)
        {
            Debug.LogError(" PlayerNetwork를 하나도 찾지 못했습니다.");

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

            // 같은 PlayerRef로 이미 생성되어 있으면 중복 생성하지 않음
            if (spawnedPlayers.ContainsKey(playerRef))
            {
                continue;
            }

            Transform spawnPoint = playerSpawnPoints[i];

            Debug.Log(
                $"===== ColorBlock Spawn =====\n" +
                $"Index = {i}\n" +
                $"PlayerRef = {playerRef}\n" +
                $"Nickname = {player.Nickname}\n" +
                $"CharacterIndex = {player.CharacterIndex}"
            );

            // --------------------------------------------------
            // ColorBlockPlayer 네트워크 생성
            // --------------------------------------------------

            NetworkObject playerObject = Runner.Spawn(
                colorBlockPlayerPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                playerRef
            );

            if (playerObject == null)
            {
                Debug.LogError($" ColorBlockPlayer Spawn 실패 : {playerRef}");

                continue;
            }

            ColorBlockPlayer colorBlockPlayer = playerObject.GetComponent<ColorBlockPlayer>();

            if (colorBlockPlayer == null)
            {
                Debug.LogError(
                    " ColorBlockPlayer 프리팹에 ColorBlockPlayer 컴포넌트가 없습니다."
                );

                Runner.Despawn(playerObject);

                continue;
            }

            colorBlockPlayer.SetColorBlockManager(this);

            // --------------------------------------------------
            // Player 정보 연결
            // --------------------------------------------------

            // 1. 플레이어 순서
            colorBlockPlayer.SetPlayerIndex(i);

            // 2. Lobby PlayerNetwork 연결
            colorBlockPlayer.SetPlayerNetwork(player);

            // 3. 캐릭터 번호 전달
            colorBlockPlayer.SetCharacterIndex(player.CharacterIndex);

            // --------------------------------------------------
            // 등록
            // --------------------------------------------------

            spawnedPlayers.Add(playerRef, colorBlockPlayer);

            Debug.Log(
                $" ColorBlockPlayer 생성 완료\n" +
                $"PlayerRef = {playerRef}\n" +
                $"PlayerIndex = {i}\n" +
                $"CharacterIndex = {player.CharacterIndex}"
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

        EnableColorBlockInput();

        RPC_StartCountdown();

        yield return new WaitForSeconds(countdownTime * 3f + 1.5f);

        while (true)
        {
            int conditionCount = GetConditionCount();

            currentConditions = GetRandomConditions(conditionCount);

            ShowConditionsToAll(currentConditions);

            yield return new WaitForSeconds(GetCurrentMoveTime());
            if (IsGameEnding)
                yield break;

            CheckBlocks();

            yield return new WaitForSeconds(1.7f);

            if (IsGameEnding)
                yield break;

            roundIndex++;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StartCountdown()
    {
        StartCoroutine(CountdownCoroutine());
    }

    private IEnumerator CountdownCoroutine()
    {
        if (countdownText == null)
            yield break;

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

    private void ShowConditionsToAll(List<BlockCondition> conditions)
    {
        if (!Object.HasStateAuthority)
            return;

        int count = conditions.Count;

        int type1 = count > 0 ? (int)conditions[0].type : 0;
        int group1 = count > 0 ? conditions[0].groupIndex : 0;

        int type2 = count > 1 ? (int)conditions[1].type : 0;
        int group2 = count > 1 ? conditions[1].groupIndex : 0;

        int type3 = count > 2 ? (int)conditions[2].type : 0;
        int group3 = count > 2 ? conditions[2].groupIndex : 0;

        RPC_ShowConditions(
            count,
            type1, group1,
            type2, group2,
            type3, group3
        );
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowConditions(
        int count,
        int type1, int group1,
        int type2, int group2,
        int type3, int group3)
    {
        List<BlockCondition> conditions = new List<BlockCondition>();

        if (count >= 1)
        {
            conditions.Add(
                new BlockCondition(
                    (ConditionType)type1,
                    group1
                )
            );
        }

        if (count >= 2)
        {
            conditions.Add(
                new BlockCondition(
                    (ConditionType)type2,
                    group2
                )
            );
        }

        if (count >= 3)
        {
            conditions.Add(
                new BlockCondition(
                    (ConditionType)type3,
                    group3
                )
            );
        }

        if (monitorUI != null)
        {
            monitorUI.ShowConditions(conditions);
        }
    }

    private float GetCurrentMoveTime()
    {
        int eliminatedCount = eliminatedRounds.Count;

        float currentTime =
            moveTime - eliminatedCount * moveTimeDecrease;

        return Mathf.Max(currentTime, minimumMoveTime);
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

    private List<BlockCondition> GetRandomConditions(int conditionCount)
    {
        List<BlockCondition> conditions = new List<BlockCondition>();

        List<int> groups = new List<int>()
        {
            0, 1, 2, 3
        };

        // 그룹 섞기
        for (int i = 0; i < groups.Count; i++)
        {
            int randomIndex =  Random.Range(i, groups.Count);

            int temp = groups[i];

            groups[i] = groups[randomIndex];

            groups[randomIndex] = temp;
        }

        for (int i = 0; i < conditionCount; i++)
        {
            int groupIndex = groups[i];

            ConditionType type =
                Random.value < 0.5f
                ? ConditionType.Color
                : ConditionType.Letter;

            conditions.Add(new BlockCondition(type, groupIndex)
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
                    block.Fall(); break;
                }
            }
        }
    }    

    private bool IsMatch(ColorBlock block, BlockCondition condition)
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

    private bool IsColorMatch(BlockColor color, int groupIndex)
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

    private bool IsLetterMatch(BlockLetter letter, int groupIndex)
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


    #region < Fall >

    public void PlayerFell(ColorBlockPlayer player)
    {
        if (!Object.HasStateAuthority)
            return;

        if (player == null || player.Object == null)
            return;

        PlayerRef playerRef = player.Object.InputAuthority;

        if (eliminatedRounds.ContainsKey(playerRef))
                return;

        eliminatedRounds.Add(playerRef, roundIndex);

        Debug.Log(
        $"[ColorBlock] Player 탈락 | " +
        $"PlayerRef={playerRef} | " +
        $"Round={roundIndex}"
        );

        Runner.Despawn(player.Object);

        CheckGameEnd();
    }

    #endregion


    #region < End >

    private void CheckGameEnd()
    {
        if (IsGameEnding)
            return;

        int aliveCount = spawnedPlayers.Count - eliminatedRounds.Count;

        Debug.Log(
            $"[ColorBlock] 현재 생존자 수 = {aliveCount} " +
            $"| 전체 플레이어 = {spawnedPlayers.Count} " +
            $"| 탈락자 = {eliminatedRounds.Count}"
        );

        if (aliveCount <= 1)
        {
            IsGameEnding = true;
            Debug.Log("[ColorBlock] 게임 종료 조건 충족!");

            StartCoroutine(GameEndRoutine());
        }
    }

    private IEnumerator GameEndRoutine()
    {
        {
            GiveColorBlockScores();

            RPC_StartFreezeFrame();

            yield return new WaitForSecondsRealtime(1f);

            RPC_ShowGameEnd();

            yield return new WaitForSecondsRealtime(1.5f);

            const int SCORE_SCENE_INDEX = 4;

            Runner.LoadScene(SceneRef.FromIndex(SCORE_SCENE_INDEX));
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StartFreezeFrame()
    {
        StartCoroutine(CaptureFreezeFrame());
    }

    private IEnumerator CaptureFreezeFrame()
    {
        if (freezeFrame == null)
        {
            Debug.LogWarning("[ColorBlock] FreezeFrame이 연결되지 않았습니다.");
            yield break;
        }

        yield return new WaitForEndOfFrame();

        Texture2D screenshot = new Texture2D(
            Screen.width,
            Screen.height,
            TextureFormat.RGB24,
            false
        );

        screenshot.ReadPixels(
            new Rect(0, 0, Screen.width, Screen.height),
            0,
            0
        );

        screenshot.Apply();

        freezeFrame.texture = screenshot;

        freezeFrame.gameObject.SetActive(true);

        Debug.Log("[ColorBlock] Freeze Frame 적용 완료");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowGameEnd()
    {
        if (gameEndText == null)
            return;

        gameEndText.SetActive(true);
    }

    #endregion


    #region < Score >

    private void GiveColorBlockScores()
    {
        if (!Object.HasStateAuthority)
            return;

        Debug.Log("===== ColorBlock 점수 계산 시작 =====");

        List<PlayerNetwork> players = GetActivePlayers();
        
        PlayerNetwork survivor = null;

        foreach (PlayerNetwork player in players)
        {
            if (!eliminatedRounds.ContainsKey(player.PlayerRef))
            {
                survivor = player;
                break;
            }
        }

        List<PlayerNetwork> eliminatedPlayers = players
            .Where(player => eliminatedRounds.ContainsKey(player.PlayerRef))
            .OrderByDescending(player => eliminatedRounds[player.PlayerRef])
            .ToList();

        List<PlayerNetwork> ranking = new();

        if (survivor != null)
        {
            ranking.Add(survivor);
        }

        ranking.AddRange(eliminatedPlayers);

        int currentRank = 1;
        int index = 0;

        while (index < ranking.Count)
        {
            PlayerNetwork player = ranking[index];

            int groupSize = 1;

            if (player != survivor)
            {
                int round = eliminatedRounds[player.PlayerRef];

                while (index + groupSize < ranking.Count)
                {
                    PlayerNetwork nextPlayer = ranking[index + groupSize];

                    if (nextPlayer == survivor)
                        break;

                    if (eliminatedRounds[nextPlayer.PlayerRef] != round)
                        break;

                    groupSize++;
                }
            }

            int score = 0;

            if (currentRank == 1)
                score = 3;
            else if (currentRank == 2)
                score = 2;
            else if (currentRank == 3)
                score = 1;
            else
                score = 0;

            for (int i = 0; i < groupSize; i++)
            {
                PlayerNetwork target = ranking[index + i];

                target.AddScore(score);

                Debug.Log(
                    $"[ColorBlock Score] " +
                    $"Player={target.PlayerRef} | " +
                    $"Rank={currentRank} | " +
                    $"Score=+{score}"
                );
            }

            currentRank += groupSize;
            index += groupSize;
        }

        Debug.Log("===== ColorBlock 점수 계산 완료 =====");
    }

    #endregion
}