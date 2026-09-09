using Fusion;
using UnityEngine;

public class ScoreUIManager : MonoBehaviour
{
    [Header("<< Score Bars >>")]
    [SerializeField] private ScoreBarUI[] scoreBars;

    [Header("<< Character >>")]
    [SerializeField] private CharacterDatabase characterDatabase;

    private NetworkRunner runner;


    private void Start()
    {
        runner = NetworkManager.Instance.GetRunner();

        if (runner == null)
        {
            Debug.LogError("NetworkRunner를 찾을 수 없습니다.");
            return;
        }

        SetupScoreBoard();
    }


    private void SetupScoreBoard()
    {
        int index = 0;

        foreach (PlayerRef playerRef in runner.ActivePlayers)
        {
            if (index >= scoreBars.Length)
                break;

            PlayerNetwork playerNetwork = FindPlayerNetwork(playerRef);

            if (playerNetwork == null)
            {
                Debug.LogWarning(
                    $"PlayerNetwork를 찾을 수 없습니다. PlayerRef = {playerRef}"
                );

                continue;
            }

            scoreBars[index].SetPlayer(playerNetwork, characterDatabase);

            scoreBars[index].SetScore(playerNetwork.Score);

            index++;
        }

        for (int i = index; i < scoreBars.Length; i++)
        {
            scoreBars[i].gameObject.SetActive(false);
        }
    }


    private PlayerNetwork FindPlayerNetwork(PlayerRef playerRef)
    {
        PlayerNetwork[] players =
            FindObjectsByType<PlayerNetwork>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (PlayerNetwork player in players)
        {
            if (player.Object.InputAuthority == playerRef)
                return player;
        }

        return null;
    }
}