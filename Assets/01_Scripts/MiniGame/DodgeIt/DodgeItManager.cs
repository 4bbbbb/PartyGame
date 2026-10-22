using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Fusion;
using UnityEngine;

public class DodgeItManager : NetworkBehaviour
{
    [Header("<< Character Database >>")]
    [SerializeField] private CharacterDatabase characterDatabase;

    [Header("<< Alien >>")]
    [SerializeField] private NetworkObject alienPrefab;
    [SerializeField] private Transform alienSpawnPoint;

    [Header("<< DodgeIt Player >>")]
    [SerializeField] private NetworkObject dodgeItPlayerPrefab;
    [SerializeField] private Transform[] playerSpawnPoints;

    [Header("<< Timer >>")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("<< Game End >>")]
    [SerializeField] private GameObject gameEnd;

    [Header("<< Elimination VFX >>")]
    [SerializeField] private GameObject eliminateVFXPrefab;

    [Networked]
    public PlayerRef AlienPlayer { get; private set; }

    [Networked]
    private int RemainingTime { get; set; } = 30;

    [Networked]
    private NetworkBool IsGameEnding { get; set; }

    private bool isEndGameScheduled;

    private Coroutine timerCoroutine;

    private readonly Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new();

    private readonly Dictionary<PlayerRef, int> eliminatedTicks = new();



    public void SetAlienPlayer(PlayerRef alienPlayer)
    {
        if (Object == null || !Object.HasStateAuthority)
            return;

        if (!Runner.ActivePlayers.Contains(alienPlayer))        
            return;
        

        AlienPlayer = alienPlayer;

        Debug.Log($"Dodge It! Alien 선정 완료: {AlienPlayer}");
    }

   
    public void StartGame()
    {
        if (Object == null || !Object.HasStateAuthority)
            return;

        if (!Runner.ActivePlayers.Contains(AlienPlayer))       
            return;
        

        if (alienPrefab == null || alienSpawnPoint == null)
            return;
      

        if (dodgeItPlayerPrefab == null ||
            playerSpawnPoints == null ||
            playerSpawnPoints.Length < 3)
        {            
            return;
        }

        List<PlayerRef> players = Runner.ActivePlayers
            .OrderBy(player => player.RawEncoded)
            .ToList();

        if (players.Count != 4)
            return;        

        SpawnAlien();
        SpawnPlayers(players);

        StartTimer();

        Debug.Log("===== Dodge It! 게임 시작 =====");
    }

    public override void Spawned()
    {
        if (AlienManager.Instance != null)
        {
            AlienManager.Instance.OnAlienSelected += HandleAlienSelected;
        }
        else
        {
            Debug.LogError("AlienManager.Instance를 찾을 수 없습니다.");
        }
    }

    private void HandleAlienSelected(PlayerRef alienPlayer)
    {
        if (!Object.HasStateAuthority)
            return;

        SetAlienPlayer(alienPlayer);
        StartGame();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (AlienManager.Instance != null)
            AlienManager.Instance.OnAlienSelected -= HandleAlienSelected;
    }


    #region < Spawn >

    private void SpawnAlien()
    {
        if (spawnedPlayers.ContainsKey(AlienPlayer))
            return;

        NetworkObject alienObject = Runner.Spawn(
            alienPrefab,
            alienSpawnPoint.position,
            alienSpawnPoint.rotation,
            AlienPlayer
        );

        if (alienObject == null)        
            return;
        

        AlienPlayer alienPlayerComponent =
            alienObject.GetComponent<AlienPlayer>();

        if (alienPlayerComponent == null)
        {           
            Runner.Despawn(alienObject);
            return;
        }

        NetworkObject playerNetworkObject =
            Runner.GetPlayerObject(AlienPlayer);

        if (playerNetworkObject == null)
        {     
            Runner.Despawn(alienObject);
            return;
        }

        PlayerNetwork playerNetwork = playerNetworkObject.GetComponent<PlayerNetwork>();

        if (playerNetwork == null)
        {    
            Runner.Despawn(alienObject);
            return;
        }

        List<PlayerRef> players = Runner.ActivePlayers
            .OrderBy(player => player.RawEncoded)
            .ToList();

        int alienPlayerIndex = players.IndexOf(AlienPlayer);

        alienPlayerComponent.SetPlayerNetwork(playerNetwork);
        alienPlayerComponent.SetPlayerIndex(alienPlayerIndex);
        alienPlayerComponent.SetCharacterIndex(playerNetwork.CharacterIndex);

        spawnedPlayers.Add(AlienPlayer, alienObject);       
    }

    private void SpawnPlayers(List<PlayerRef> players)
    {
        int spawnIndex = 0;

        foreach (PlayerRef player in players)
        {
            if (player == AlienPlayer)
                continue;

            if (spawnedPlayers.ContainsKey(player))
                continue;

            if (spawnIndex >= playerSpawnPoints.Length)            
                return;
            

            Transform spawnPoint = playerSpawnPoints[spawnIndex];

            NetworkObject playerObject = Runner.Spawn(
                dodgeItPlayerPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                player
            );

            if (playerObject == null)
            {
                continue;
            }

            DodgeItPlayer dodgePlayer = playerObject.GetComponent<DodgeItPlayer>();

            NetworkObject playerNetworkObject = Runner.GetPlayerObject(player);

            PlayerNetwork playerNetwork = playerNetworkObject != null
                ? playerNetworkObject.GetComponent<PlayerNetwork>()
                : null;

            if (dodgePlayer == null || playerNetwork == null)
            {          
                Runner.Despawn(playerObject);
                continue;
            }

            int playerIndex = players.IndexOf(player);

            dodgePlayer.SetPlayerNetwork(playerNetwork);
            dodgePlayer.SetPlayerIndex(playerIndex);
            dodgePlayer.SetCharacterIndex(playerNetwork.CharacterIndex);
            dodgePlayer.SetDodgeItManager(this);

            spawnedPlayers.Add(player, playerObject);
            spawnIndex++;          
        }
    }

    #endregion


    #region < Timer >

    private void StartTimer()
    {
        if (!Object.HasStateAuthority)
            return;

        RemainingTime = 30;
        IsGameEnding = false;
        isEndGameScheduled = false;

        if (gameEnd != null)
            gameEnd.SetActive(false);

        UpdateTimerUI();

        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        timerCoroutine = StartCoroutine(TimerRoutine());
    }

    private IEnumerator TimerRoutine()
    {
        while (RemainingTime > 0 && !IsGameEnding)
        {
            yield return new WaitForSeconds(1f);

            if (IsGameEnding)
                yield break;

            RemainingTime--;
        }

        timerCoroutine = null;

        if (!IsGameEnding)
            EndGame();
    }

    public override void Render()
    {
        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
            timerText.text = RemainingTime.ToString();
    }

    #endregion


    #region < VFX >

    public void SpawnEliminateVFX(Vector3 position)
    {
        if (!Object.HasStateAuthority)
            return;

        RPC_SpawnEliminateVFX(position);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SpawnEliminateVFX(Vector3 position)
    {
        Debug.Log(
            $"[VFX RPC] 실행됨 | " +
            $"LocalPlayer: {Runner.LocalPlayer} | " +
            $"HasStateAuthority: {Object.HasStateAuthority}"
        );

        SpawnEliminateVFXLocal(position);
    }

    private void SpawnEliminateVFXLocal(Vector3 position)
    {
        if (eliminateVFXPrefab == null)
        {
            Debug.LogError("[VFX] eliminateVFXPrefab이 연결되지 않았습니다!");
            return;
        }

        Instantiate(
            eliminateVFXPrefab,
            position,
            Quaternion.identity
        );

        Debug.Log($"[VFX] 생성 완료: {position}");
    }

    #endregion


    #region < End >

    private void EndGame()
    {
        if (!Object.HasStateAuthority || IsGameEnding)
            return;

        IsGameEnding = true;

        StartCoroutine(GameEndRoutine());
    }

    private IEnumerator GameEndRoutine()
    {
        GiveDodgeItScores();

        RPC_FreezeGame();

        yield return new WaitForSecondsRealtime(1f);

        RPC_ShowGameEnd();

        yield return new WaitForSecondsRealtime(1.5f);

        const int SCORE_SCENE_INDEX = 3;

        Time.timeScale = 1f;

        Runner.LoadScene(SceneRef.FromIndex(SCORE_SCENE_INDEX));
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_FreezeGame()
    {
        Time.timeScale = 0f;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowGameEnd()
    {
        Time.timeScale = 1f;

        if (gameEnd != null)
            gameEnd.SetActive(true);
    }


    public void CheckAllPlayersEliminated()
    {
        if (Object == null)
            return;

        if (!Object.HasStateAuthority || IsGameEnding || isEndGameScheduled)
            return;

        int aliveCount = 0;

        foreach (var pair in spawnedPlayers)
        {
            if (pair.Key == AlienPlayer)
                continue;

            NetworkObject playerObject = pair.Value;

            if (playerObject == null)
                continue;

            DodgeItPlayer player = playerObject.GetComponent<DodgeItPlayer>();

            if (player == null)
                continue;

            // Fusion에서 아직 Spawn되지 않았거나
            // 이미 Despawn된 플레이어는 건너뛴다.
            if (player.Object == null || !player.Object.IsValid)
                continue;

            if (!player.IsEliminated)
                aliveCount++;
        }

        if (aliveCount == 0)
        {
            isEndGameScheduled = true;

            // 타이머를 즉시 멈춘다.
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }

            Debug.Log("[Dodge It!] 전원 탈락! 타이머 중지, 1초 후 게임 종료");

            StartCoroutine(EndGameAfterDelay());
        }
    }

    private IEnumerator EndGameAfterDelay()
    {
        yield return new WaitForSeconds(1f);

        EndGame();
    }

    public void RecordElimination(PlayerRef player)
    {
        if (!Object.HasStateAuthority)
            return;

        if (eliminatedTicks.ContainsKey(player))
            return;

        eliminatedTicks[player] = Runner.Tick.Raw;

        Debug.Log(
            $"[Dodge It!] 탈락 기록 저장 완료: {player} | Tick={Runner.Tick.Raw}"
        );
    }

    #endregion


    #region < Score >

    private void GiveDodgeItScores()
    {
        if (!Object.HasStateAuthority)
            return;

        Debug.Log("===== Dodge It! 점수 계산 시작 =====");

        List<PlayerRef> players = Runner.ActivePlayers
            .OrderBy(player => player.RawEncoded)
            .ToList();

        List<PlayerRef> dodgePlayers = players
            .Where(player => player != AlienPlayer)
            .ToList();

        bool allEliminated = dodgePlayers.All(
            player => eliminatedTicks.ContainsKey(player)
        );

        if (allEliminated)
        {
            AddPlayerScore(AlienPlayer, 3);

            Debug.Log("[Dodge It!] 전원 탈락! Alien +3점");

            Debug.Log("===== Dodge It! 점수 계산 완료 =====");
            return;
        }

        List<PlayerRef> survivors = dodgePlayers
            .Where(player => !eliminatedTicks.ContainsKey(player))
            .ToList();

        foreach (PlayerRef survivor in survivors)
        {
            AddPlayerScore(survivor, 3);

            Debug.Log($"[Dodge It! Score] 생존: {survivor} | +3점");
        }

        List<IGrouping<int, PlayerRef>> eliminationGroups = dodgePlayers
            .Where(player => eliminatedTicks.ContainsKey(player))
            .GroupBy(player => eliminatedTicks[player])
            .OrderBy(group => group.Key)
            .ToList();

        int eliminationOrder = 0;

        foreach (IGrouping<int, PlayerRef> group in eliminationGroups)
        {
            int score;

            if (group.Count() > 1)
            {
                score = 1;
            }
            else
            {
                eliminationOrder++;

                score = eliminationOrder;
            }

            foreach (PlayerRef player in group)
            {
                AddPlayerScore(player, score);

                Debug.Log(
                    $"[Dodge It! Score] 탈락: {player} | " +
                    $"Tick={group.Key} | +{score}점"
                );
            }
        }

        Debug.Log($"[Dodge It! Score] Alien: {AlienPlayer} | +0점");
        Debug.Log("===== Dodge It! 점수 계산 완료 =====");
    }

    private void AddPlayerScore(PlayerRef playerRef, int score)
    {
        NetworkObject playerObject = Runner.GetPlayerObject(playerRef);

        if (playerObject == null)
        {
            Debug.LogWarning(
                $"[Dodge It!] PlayerNetwork를 찾을 수 없습니다: {playerRef}"
            );
            return;
        }

        PlayerNetwork playerNetwork =
            playerObject.GetComponent<PlayerNetwork>();

        if (playerNetwork == null)
        {
            Debug.LogWarning(
                $"[Dodge It!] PlayerNetwork 컴포넌트가 없습니다: {playerRef}"
            );
            return;
        }

        playerNetwork.AddScore(score);
    }

    #endregion
}