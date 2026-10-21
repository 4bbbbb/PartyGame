
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("<< Mini Games >>")]
    [SerializeField]
    private string[] miniGameSceneNames =
    {
        "02_Baseball",
        "02_ColorBlock",
        "02_DodgeIt",
        "02_WhackaMole"
    };

    [Header("<< Game Rules >>")]
    [SerializeField] private string scoreSceneName = "03_Score";
    [SerializeField] private float scoreWaitSeconds = 5f;
    [SerializeField] private int winningScore = 10;

    private readonly List<string> remainingMiniGames = new();

    private bool gameStarted;
    private bool gameFinished;
    private bool scoreRoutineRunning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void Spawned()
    {
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

        Debug.Log(
            $"[GameManager] Spawned - " +
            $"HasStateAuthority: {Object.HasStateAuthority}, " +
            $"Scene: {gameObject.scene.name}"
        );
    }

    private void OnDestroy()
    {
        Debug.LogError(
            $"[GameManager] OnDestroy 호출! " +
            $"Object: {gameObject.name}, " +
            $"Scene: {gameObject.scene.name}, " +
            $"Instance 일치: {Instance == this}"
        );

        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    // 로비의 게임 시작 버튼에서 호출할 함수
    public void StartMiniGames()
    {
        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("[GameManager] 호스트만 게임을 시작할 수 있습니다.");
            return;
        }

        if (gameStarted)
        {
            Debug.LogWarning("[GameManager] 이미 게임이 시작되었습니다.");
            return;
        }

        gameStarted = true;
        gameFinished = false;

        RefillMiniGames();
        StartNextMiniGame();
    }

    // 아직 플레이하지 않은 미니게임 목록 채우기
    private void RefillMiniGames()
    {
        remainingMiniGames.Clear();

        foreach (string sceneName in miniGameSceneNames)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                remainingMiniGames.Add(sceneName);
            }
        }

        Debug.Log(
            $"[GameManager] 미니게임 목록 초기화: " +
            $"{remainingMiniGames.Count}개"
        );
    }

    // 남은 미니게임 중 하나를 랜덤으로 실행
    private void StartNextMiniGame()
    {
        if (!Object.HasStateAuthority || gameFinished)
        {
            return;
        }

        if (remainingMiniGames.Count == 0)
        {
            Debug.LogWarning("[GameManager] 남은 미니게임이 없습니다.");
            return;
        }

        int randomIndex = Random.Range(0, remainingMiniGames.Count);
        string sceneName = remainingMiniGames[randomIndex];

        // 선택한 게임은 목록에서 제거해 중복 방지
        remainingMiniGames.RemoveAt(randomIndex);

        int sceneBuildIndex = GetSceneBuildIndex(sceneName);

        if (sceneBuildIndex < 0)
        {
            Debug.LogError(
                $"[GameManager] 씬을 찾을 수 없습니다: {sceneName}\n" +
                "씬 이름과 Build Profiles 설정을 확인해 주세요."
            );
            return;
        }

        Debug.Log($"[GameManager] 다음 미니게임: {sceneName}");

        Runner.LoadScene(SceneRef.FromIndex(sceneBuildIndex));
    }

    // Score 씬에 도착하면 호스트가 다음 진행을 관리
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!Object.HasStateAuthority ||
            !gameStarted ||
            gameFinished ||
            scene.name != scoreSceneName ||
            scoreRoutineRunning)
        {
            return;
        }

        StartCoroutine(ScoreSceneRoutine());
    }

    private IEnumerator ScoreSceneRoutine()
    {
        scoreRoutineRunning = true;

        Debug.Log(
            $"[GameManager] {scoreSceneName} 진입. " +
            $"{scoreWaitSeconds}초 대기"
        );

        yield return new WaitForSeconds(scoreWaitSeconds);

        // 이번 순서에서 아직 플레이하지 않은 게임이 남아 있음
        if (remainingMiniGames.Count > 0)
        {
            scoreRoutineRunning = false;
            StartNextMiniGame();
            yield break;
        }

        // 미니게임 4개를 모두 플레이한 시점에만 우승 점수 확인
        int highestScore = GetHighestPlayerScore();

        Debug.Log(
            $"[GameManager] 한 사이클 종료. " +
            $"최고 점수: {highestScore}"
        );

        if (highestScore >= winningScore)
        {
            gameFinished = true;

            Debug.Log(
                $"[GameManager] 최종 게임 종료! " +
                $"최고 점수 {highestScore}점"
            );

            // 최종 결과를 보여 주기 위해 Score 씬에 머무름
            scoreRoutineRunning = false;
            yield break;
        }

        // 목표 점수에 도달하지 못했으면 새 사이클 시작
        Debug.Log("[GameManager] 목표 점수 미달. 다음 사이클 시작");

        RefillMiniGames();

        scoreRoutineRunning = false;
        StartNextMiniGame();
    }

    // 현재 접속 중인 플레이어 중 최고 누적 점수 반환
    private int GetHighestPlayerScore()
    {
        int highestScore = 0;

        foreach (PlayerRef playerRef in Runner.ActivePlayers)
        {
            if (Runner.TryGetPlayerObject(
                playerRef,
                out NetworkObject playerObject))
            {
                PlayerNetwork playerNetwork =
                    playerObject.GetComponent<PlayerNetwork>();

                if (playerNetwork != null)
                {
                    highestScore = Mathf.Max(
                        highestScore,
                        playerNetwork.Score
                    );
                }
            }
        }

        return highestScore;
    }

    private int GetSceneBuildIndex(string sceneName)
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings;

        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string fileName = Path.GetFileNameWithoutExtension(scenePath);

            if (fileName == sceneName)
            {
                return i;
            }
        }

        return -1;
    }
}
