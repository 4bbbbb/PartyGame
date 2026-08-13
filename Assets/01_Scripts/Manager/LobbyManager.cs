using Fusion;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// 
/// 1. 네트워크 플레이어 목록
/// 2. PlayerNetwork 관리
/// 
/// </summary>

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("<< Player Slots >>")]
    [SerializeField] private PlayerInfoUI hostSlot;
    [SerializeField] private PlayerInfoUI player2Slot;
    [SerializeField] private PlayerInfoUI player3Slot;
    [SerializeField] private PlayerInfoUI player4Slot;

    [Header("<< Player Count >>")]
    [SerializeField] private TMP_Text playerCountText;

    private int playerCount = 0;


    private void Awake()
    {
        Debug.Log("LobbyManager Awake");

        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ClearAllSlots();

        if (NetworkManager.Instance != null)
        {
            // Lobby 진입 직후에는 기존 PlayerNetwork를 기준으로 한 번 갱신
            // 실제 PlayerJoined에서 다시 갱신됨
        }
    }

    public void PlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"LobbyManager.PlayerJoined : {player}");

        RefreshPlayerList(runner);
    }

    public void PlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"LobbyManager.PlayerLeft : {player}");

        RefreshPlayerList(runner, player);
    }

    public void RefreshPlayerList(NetworkRunner runner, PlayerRef? leavingPlayer = null)
    {
        PlayerNetwork[] allPlayers = FindObjectsByType<PlayerNetwork>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        HashSet<PlayerRef> activePlayers = runner.ActivePlayers.ToHashSet();

        List<PlayerNetwork> players = allPlayers
            .Where(player => activePlayers.Contains(player.PlayerRef))
            .ToList();

        // 혹시 OnPlayerLeft 직후 ActivePlayers 반영 타이밍이 겹쳐도
        // 퇴장한 PlayerRef는 무조건 제외
        if (leavingPlayer.HasValue)
        {
            players = players
                .Where(player => player.PlayerRef != leavingPlayer.Value)
                .ToList();
        }

        players = players
            .OrderBy(player => player.PlayerRef.RawEncoded)
            .ToList();

        playerCount = players.Count;

        Debug.Log($"현재 Fusion 플레이어 수 : {activePlayers.Count}");
        Debug.Log($"현재 UI 플레이어 수 : {playerCount}");

        ClearAllSlots();

        for (int i = 0; i < players.Count && i < 4; i++)
        {
            string nickname = players[i].Nickname.ToString();
            bool ready = players[i].IsReady;

            Debug.Log(
                $"슬롯 {i + 1} : " +
                $"PlayerRef = {players[i].PlayerRef}, " +
                $"Nickname = {nickname}"
            );

            switch (i)
            {
                case 0:
                    hostSlot.SetPlayer(nickname);
                    hostSlot.SetReady(ready);
                    break;

                case 1:
                    player2Slot.SetPlayer(nickname);
                    player2Slot.SetReady(ready);
                    break;

                case 2:
                    player3Slot.SetPlayer(nickname);
                    player3Slot.SetReady(ready);
                    break;

                case 3:
                    player4Slot.SetPlayer(nickname);
                    player4Slot.SetReady(ready);
                    break;
            }
        }

        UpdatePlayerCount();
    }

    public bool AreAllPlayersReady()
    {
        if (NetworkManager.Instance == null)
            return false;

        NetworkRunner runner = NetworkManager.Instance.GetRunner();

        if (runner == null)
            return false;

        PlayerNetwork[] allPlayers = FindObjectsByType<PlayerNetwork>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        HashSet<PlayerRef> activePlayers = runner.ActivePlayers.ToHashSet();

        List<PlayerNetwork> players = allPlayers
            .Where(player => activePlayers.Contains(player.PlayerRef))
            .ToList();

        // 플레이어가 한 명도 없으면 Start 불가
        if (players.Count == 0)
            return false;

        // 현재 들어와 있는 모든 플레이어가 Ready인지 확인
        foreach (PlayerNetwork player in players)
        {
            if (!player.IsReady)
            {
                return false;
            }
        }

        return true;
    }

    private void UpdatePlayerCount()
    {
        playerCountText.text = $"{playerCount}";
    }

    private void ClearAllSlots()
    {
        hostSlot.Clear();
        player2Slot.Clear();
        player3Slot.Clear();
        player4Slot.Clear();
    }
}
