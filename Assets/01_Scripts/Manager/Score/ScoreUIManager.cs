
using Fusion;
using System.Collections.Generic;
using System.Linq;
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
        if (NetworkManager.Instance == null)        
            return;
        

        runner = NetworkManager.Instance.GetRunner();

        if (runner == null)       
            return;        

        SetupScoreBoard();
    }

    private void SetupScoreBoard()
    {
        List<PlayerNetwork> players = new List<PlayerNetwork>();

        foreach (PlayerRef playerRef in runner.ActivePlayers)
        {
            PlayerNetwork player = FindPlayerNetwork(playerRef);

            if (player == null)
                continue;            

            players.Add(player);
        }

        players = players
            .OrderByDescending(player => player.Score)
            .ToList();

        int count = Mathf.Min(players.Count, scoreBars.Length);

        for (int i = 0; i < count; i++)
        {
            PlayerNetwork player = players[i];

            bool isLocalPlayer = player.Object.InputAuthority == runner.LocalPlayer;

            scoreBars[i].gameObject.SetActive(true);

            scoreBars[i].SetPlayer(
                player,
                characterDatabase,
                isLocalPlayer
            );

            scoreBars[i].SetScore(
                player.PreviousScore,
                player.Score
                );
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
            if (player.Object != null &&
                player.Object.InputAuthority == playerRef)
            {
                return player;
            }
        }

        return null;
    }
}
