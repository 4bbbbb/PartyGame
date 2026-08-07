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

    private const int TITLE_SCENE_INDEX = 1;
    private const int LOBBY_SCENE_INDEX = 2;
    private const int GAME_SCENE_INDEX = 3;

    private const string ROOM_NAME = "LobbyRoom";

    private NetworkRunner runner;

    public NetworkRunner GetRunner()
    {
        return runner;
    }

    [SerializeField] private NetworkPrefabRef playerPrefab;


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

    // UI의 Play 버튼에서 호출
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
                playerPrefab,
                Vector3.zero,
                Quaternion.identity,
                player
            );

            Debug.Log($"Player Spawn 완료 : {playerObject}");
        }

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.PlayerJoined(runner, player);
        }
    }

    //public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    //{
    //    Debug.Log($"Player Left : {player}");
    //}

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("===== OnPlayerLeft 호출 =====");
        Debug.Log($"Player Left : {player}");

        if (LobbyManager.Instance != null)
        {
            Debug.Log("LobbyManager.PlayerLeft 호출");

            LobbyManager.Instance.PlayerLeft(runner, player);
        }
    }

    public async void LeaveRoom()
{
    if (runner == null)
    {
        Debug.Log("나갈 Fusion 방이 없습니다.");

        SceneManager.LoadScene(TITLE_SCENE_INDEX);
        return;
    }

    Debug.Log("방 나가기 시작");

    NetworkRunner currentRunner = runner;

    runner = null;

    await currentRunner.Shutdown();

    Debug.Log("Fusion 방 나가기 완료");

    Destroy(currentRunner);

    SceneManager.LoadScene(TITLE_SCENE_INDEX);

    Debug.Log("Title 씬 이동");
}

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log("===== OnShutdown 호출 =====");
        Debug.Log($"Shutdown Reason : {shutdownReason}");
        Debug.Log($"Runner GameMode : {runner.GameMode}");
        Debug.Log($"Runner IsServer : {runner.IsServer}");
        Debug.Log($"Runner IsClient : {runner.IsClient}");

        this.runner = null;
    }

    public void OnDisconnectedFromServer(
    NetworkRunner runner,
    NetDisconnectReason reason)
    {
        Debug.Log("===== OnDisconnectedFromServer 호출 =====");
        Debug.Log($"Disconnect Reason : {reason}");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"Connect Failed : {reason}");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        request.Accept();
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"세션 개수 : {sessionList.Count}");

        foreach (var session in sessionList)
        {
            Debug.Log($"세션 : {session.Name}");
        }
    }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("Scene Load Start");
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("Scene Load Done");
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}