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

    //--------------------------------------------------
    // Callbacks
    //--------------------------------------------------

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("서버 연결 성공");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player Joined : {player}");

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.PlayerJoined(player);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player Left : {player}");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"Shutdown : {shutdownReason}");

        this.runner = null;
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"Disconnected : {reason}");
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