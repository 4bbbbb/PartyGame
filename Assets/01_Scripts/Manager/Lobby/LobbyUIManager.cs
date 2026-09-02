using Fusion;
using UnityEngine;

public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager Instance { get; private set; }

    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject hostOnlyButton;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        UpdateStartButton();
    }


    #region < Home >
    public void OnClickHome()
    {
        Debug.Log("===== HOME BUTTON CLICK =====");

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.LeaveRoom();
        }
        else
        {
            Debug.LogError("NetworkManager가 없습니다.");
        }
    }
    #endregion


    #region < Ready >
    public void OnClickReady()
    {
        Debug.Log("Ready 버튼 클릭");

        PlayerNetwork myPlayer = FindMyPlayer();

        if (myPlayer == null)
        {
            Debug.LogError("내 PlayerNetwork를 찾을 수 없습니다.");
            return;
        }

        myPlayer.ToggleReady();
    }

    private PlayerNetwork FindMyPlayer()
    {
        PlayerNetwork[] players = FindObjectsByType<PlayerNetwork>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (PlayerNetwork player in players)
        {
            if (player.Object.HasInputAuthority)
            {
                return player;
            }
        }

        return null;
    }
    #endregion


    #region < Start >
    public void OnClickStart()
    {
        Debug.Log("Start 버튼 클릭");

        if (NetworkManager.Instance == null)
            return;

        if (!NetworkManager.Instance.IsHost)
            return;

        if (!LobbyManager.Instance.AreAllPlayersReady())
        {
            Debug.Log("아직 모든 플레이어가 Ready하지 않았습니다.");
            return;
        }

        NetworkManager.Instance.StartGameCountdown();
    }

    public void UpdateStartButton()
    {
        if (NetworkManager.Instance == null)
            return;

        NetworkRunner runner = NetworkManager.Instance.GetRunner();

        if (runner == null)
            return;

        bool isHost = runner.IsServer;

        startButton.SetActive(isHost);
        hostOnlyButton.SetActive(!isHost);

        if (isHost)
        {
            bool allReady = LobbyManager.Instance.AreAllPlayersReady();

            startButton.GetComponent<UnityEngine.UI.Button>().interactable = allReady;
        }
    }
    #endregion
}
