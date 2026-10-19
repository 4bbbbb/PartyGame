using Fusion;
using System.Collections.Generic;
using System.Linq;
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

    [Networked]
    public PlayerRef AlienPlayer { get; private set; }

    private readonly Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new();

    /// <summary>
    /// AlienManager에서 선정된 Alien 플레이어를 전달받는다.
    /// </summary>
    public void SetAlienPlayer(PlayerRef alienPlayer)
    {
        if (Object == null || !Object.HasStateAuthority)
            return;

        if (!Runner.ActivePlayers.Contains(alienPlayer))
        {
            Debug.LogWarning($"유효하지 않은 Alien 플레이어: {alienPlayer}");
            return;
        }

        AlienPlayer = alienPlayer;

        Debug.Log($"Dodge It! Alien 선정 완료: {AlienPlayer}");
    }

    /// <summary>
    /// Alien 선정이 완료된 후 본게임을 시작한다.
    /// </summary>
    public void StartGame()
    {
        if (Object == null || !Object.HasStateAuthority)
            return;

        if (!Runner.ActivePlayers.Contains(AlienPlayer))
        {
            Debug.LogError("Alien 플레이어가 설정되지 않았습니다.");
            return;
        }

        if (alienPrefab == null || alienSpawnPoint == null)
        {
            Debug.LogError("Alien 프리팹 또는 스폰 위치가 연결되지 않았습니다.");
            return;
        }

        if (dodgeItPlayerPrefab == null ||
            playerSpawnPoints == null ||
            playerSpawnPoints.Length < 3)
        {
            Debug.LogError(
                "DodgeItPlayer 프리팹과 스폰 위치 3개를 확인해 주세요."
            );
            return;
        }

        List<PlayerRef> players = Runner.ActivePlayers
            .OrderBy(player => player.RawEncoded)
            .ToList();

        if (players.Count != 4)
        {
            Debug.LogWarning(
                $"Dodge It!은 현재 4인 플레이를 기준으로 합니다. 현재 인원: {players.Count}"
            );
            return;
        }

        SpawnAlien();
        SpawnPlayers(players);

        Debug.Log("===== Dodge It! 게임 시작 =====");
    }

    public override void Spawned()
    {
        Debug.Log("===== DodgeItManager Spawned =====");

        if (AlienManager.Instance != null)
        {
            AlienManager.Instance.OnAlienSelected += HandleAlienSelected;
            Debug.Log("Alien 선정 이벤트 연결 완료");
        }
        else
        {
            Debug.LogError("AlienManager.Instance를 찾을 수 없습니다.");
        }
    }

    private void HandleAlienSelected(PlayerRef alienPlayer)
    {
        Debug.Log($"DodgeItManager Alien 수신: {alienPlayer}");

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

    /// <summary>
    /// Alien 플레이어 스폰
    /// </summary>
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
        {
            Debug.LogError("[DodgeItManager] Alien 스폰에 실패했습니다.");
            return;
        }

        AlienPlayer alienPlayerComponent =
            alienObject.GetComponent<AlienPlayer>();

        if (alienPlayerComponent == null)
        {
            Debug.LogError(
                "[DodgeItManager] Alien 프리팹에 AlienPlayer가 없습니다."
            );

            Runner.Despawn(alienObject);
            return;
        }

        NetworkObject playerNetworkObject =
            Runner.GetPlayerObject(AlienPlayer);

        if (playerNetworkObject == null)
        {
            Debug.LogError(
                $"[DodgeItManager] Alien의 PlayerNetwork를 찾을 수 없습니다: {AlienPlayer}"
            );

            Runner.Despawn(alienObject);
            return;
        }

        PlayerNetwork playerNetwork =
            playerNetworkObject.GetComponent<PlayerNetwork>();

        if (playerNetwork == null)
        {
            Debug.LogError(
                "[DodgeItManager] Alien의 PlayerNetwork 컴포넌트가 없습니다."
            );

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

        Debug.Log(
            $"[DodgeItManager] Alien 스폰 완료: " +
            $"Player={AlienPlayer}, " +
            $"PlayerIndex={alienPlayerIndex}, " +
            $"CharacterIndex={playerNetwork.CharacterIndex}"
        );
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
            {
                Debug.LogError(
                    "[DodgeItManager] 플레이어 스폰 위치가 부족합니다."
                );
                return;
            }

            Transform spawnPoint = playerSpawnPoints[spawnIndex];

            NetworkObject playerObject = Runner.Spawn(
                dodgeItPlayerPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                player
            );

            if (playerObject == null)
            {
                Debug.LogError(
                    $"[DodgeItManager] 플레이어 스폰 실패: {player}"
                );
                continue;
            }

            DodgeItPlayer dodgePlayer =
                playerObject.GetComponent<DodgeItPlayer>();

            NetworkObject playerNetworkObject =
                Runner.GetPlayerObject(player);

            PlayerNetwork playerNetwork = playerNetworkObject != null
                ? playerNetworkObject.GetComponent<PlayerNetwork>()
                : null;

            if (dodgePlayer == null || playerNetwork == null)
            {
                Debug.LogError(
                    $"[DodgeItManager] 플레이어 구성 요소를 찾을 수 없습니다: {player}"
                );

                Runner.Despawn(playerObject);
                continue;
            }

            int playerIndex = players.IndexOf(player);

            dodgePlayer.SetPlayerNetwork(playerNetwork);
            dodgePlayer.SetPlayerIndex(playerIndex);
            dodgePlayer.SetCharacterIndex(playerNetwork.CharacterIndex);

            spawnedPlayers.Add(player, playerObject);
            spawnIndex++;

            Debug.Log(
                $"[DodgeItManager] DodgeItPlayer 스폰 완료: " +
                $"Player={player}, " +
                $"PlayerIndex={playerIndex}, " +
                $"CharacterIndex={playerNetwork.CharacterIndex}"
            );
        }
    }    
}