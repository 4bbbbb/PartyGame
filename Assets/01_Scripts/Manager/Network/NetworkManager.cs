using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 
/// 1. Fusion 연결
/// 2. Room 생성 / 참가
/// 3. Scene 이동
/// 4. 플레이어 입장 / 퇴장
/// 
/// </summary>

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkManager Instance { get; private set; }

    [SerializeField] private NetworkPrefabRef playerNetworkPrefab;
    [SerializeField] private NetworkPrefabRef gameManagerPrefab;

    private const int TITLE_SCENE_INDEX = 1;
    private const int LOBBY_SCENE_INDEX = 2;
    private const int GAME_SCENE_INDEX = 3;

    private const string ROOM_NAME = "LobbyRoom";

    private NetworkRunner runner;

    private bool gameManagerSpawned;

    public NetworkRunner GetRunner()
    {
        return runner;
    }    

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SceneManager.LoadScene("00_Title");
    }

    public void Play()
    {
        StartGame(GameMode.AutoHostOrClient);
    }

    private async void StartGame(GameMode mode)
    {
        if (runner != null)
            return;

        runner = gameObject.AddComponent<NetworkRunner>();
       
        runner.ProvideInput = false;

        runner.AddCallbacks(this);

        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        SceneRef scene = SceneRef.FromIndex(LOBBY_SCENE_INDEX);

        NetworkSceneInfo sceneInfo = new NetworkSceneInfo();

        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Single);
        }

        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = ROOM_NAME,
            Scene = sceneInfo,
            SceneManager = sceneManager
        });

        if (result.Ok)
        {
            Debug.Log("Fusion 시작 성공");
            Debug.Log($"GameMode : {runner.GameMode}");
            Debug.Log($"IsServer : {runner.IsServer}");
            Debug.Log($"IsClient : {runner.IsClient}");            
        }
        else
        {
            Debug.LogError($"Fusion 시작 실패 : {result.ShutdownReason}");

            Destroy(runner);

            runner = null;
        }
    }

    public bool IsHost
    {
        get
        {
            return runner != null && runner.IsServer;
        }
    }

    public async void StartGameCountdown()
    {
        if (runner == null)
            return;

        if (!runner.IsServer)
            return;

        await System.Threading.Tasks.Task.Delay(3000);


        SceneRef scene = SceneRef.FromIndex(GAME_SCENE_INDEX);

        await runner.LoadScene(scene);
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("서버 연결 성공");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player Joined : {player}");

        if (runner.IsServer)
        {
            NetworkObject playerObject = runner.Spawn(
                playerNetworkPrefab,
                Vector3.zero,
                Quaternion.identity,
                player
            );

            runner.SetPlayerObject(player, playerObject);            
        }

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.PlayerJoined(runner, player);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.PlayerLeft(runner, player);
        }
    }

    public async void LeaveRoom()
    {
        if (runner == null)
        {
            SceneManager.LoadScene(TITLE_SCENE_INDEX);
            return;
        }

        NetworkRunner currentRunner = runner;

        runner = null;

        await currentRunner.Shutdown();

        Destroy(currentRunner);

        SceneManager.LoadScene(TITLE_SCENE_INDEX);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    { 
        this.runner = null;
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log("===== OnDisconnectedFromServer 호출 =====");
        Debug.Log($"Disconnect Reason : {reason}");
    }


    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"Connect Failed : {reason}");
    }

    public void OnConnectRequest(
        NetworkRunner runner,
        NetworkRunnerCallbackArgs.ConnectRequest request,
        byte[] token)
    {
        request.Accept();
    }


    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (!runner.ProvideInput)
            return;

        MiniGameInputData data = default;

        #region << ColorBlock >>
        ColorBlockManager colorBlockManager =
            FindFirstObjectByType<ColorBlockManager>();

        if (colorBlockManager != null)
        {
            ColorBlockPlayer localPlayer =
                colorBlockManager.GetLocalPlayer();

            if (localPlayer != null)
            {
                data.Move = localPlayer.GetMoveInput();
                data.Jump = localPlayer.GetJumpInput();
            }

            input.Set(data);
            return;
        }

        #endregion

        #region << Dodge It >>
        DodgeItManager dodgeItManager =
            FindFirstObjectByType<DodgeItManager>();

        if (dodgeItManager != null)
        {
            DodgeItPlayer localPlayer =
                dodgeItManager.GetLocalDodgeItPlayer();

            AlienPlayer alienPlayer =
                dodgeItManager.GetLocalAlienPlayer();

            if (localPlayer != null)
            {
                data.Move = localPlayer.GetMoveInput();
                data.Jump = localPlayer.GetJumpInput();
            }
            else if (alienPlayer != null)
            {
                data.Move = alienPlayer.GetMoveInput();
                data.DropObstacle = alienPlayer.GetDropObstacleInput();
            }

            input.Set(data);
            return;
        }
        #endregion

        input.Set(data);
    }


    public void OnInputMissing(NetworkRunner runner, PlayerRef player,NetworkInput input)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"세션 개수 : {sessionList.Count}");

        foreach (var session in sessionList)
        {
            Debug.Log($"세션 : {session.Name}");
        }
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnReliableDataReceived(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        ReadOnlySpan<byte> data)
    {
    }

    public void OnReliableDataProgress(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        float progress)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log(
    $"[SceneLoadStart] " +
    $"IsServer={runner.IsServer}, " +
    $"IsRunning={runner.IsRunning}"
);

        Debug.Log("Scene Load Start");
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("Scene Load Done");

        if (runner.IsServer &&
            SceneManager.GetActiveScene().buildIndex == LOBBY_SCENE_INDEX &&
            !gameManagerSpawned)
        {
            NetworkObject gameManagerObject = runner.Spawn(
                gameManagerPrefab,
                Vector3.zero,
                Quaternion.identity
            );

            if (gameManagerObject != null)
            {
                gameManagerSpawned = true;
                Debug.Log("[NetworkManager] Lobby에서 GameManager 생성 완료");
            }
            else
            {
                Debug.LogError("[NetworkManager] GameManager 생성 실패");
            }
        }

        if (TagManager.Instance != null)
        {
            TagManager.Instance.SetupTagUI();
        }
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }
}