using Fusion;
using TMPro;
using UnityEngine;

/// <summary>
/// 
/// 1. 플레이어 목록
/// 2. Ready
/// 3. Host Start
/// 
/// </summary>

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    private int playerCount = 0;


    [SerializeField] private PlayerInfoUI hostSlot;
    [SerializeField] private PlayerInfoUI player2Slot;
    [SerializeField] private PlayerInfoUI player3Slot;
    [SerializeField] private PlayerInfoUI player4Slot;

    [SerializeField] private TMP_Text playerCountText;

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
        hostSlot.Clear();
        player2Slot.Clear();
        player3Slot.Clear();
        player4Slot.Clear();

        UpdatePlayerCount();
    }

    public void PlayerJoined(PlayerRef player)
    {
        Debug.Log("LobbyManager.PlayerJoined 호출");

        playerCount++;

        Debug.Log($"playerCount = {playerCount}");


        switch (playerCount)
        {
            case 1:
                hostSlot.SetPlayer(PlayerData.Nickname);
                break;

            case 2:
                player2Slot.SetPlayer("Player2");
                break;

            case 3:
                player3Slot.SetPlayer("Player3");
                break;

            case 4:
                player4Slot.SetPlayer("Player4");
                break;
        }

        UpdatePlayerCount();
    }

    public void PlayerLeft(PlayerRef player)
    {
        playerCount--;

        UpdatePlayerCount();
    }

    private void UpdatePlayerCount()
    {
        playerCountText.text = $"{playerCount}";
    }
}
