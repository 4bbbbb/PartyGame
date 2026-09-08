using Fusion;
using UnityEngine;

public class ScoreManager : NetworkBehaviour
{
    [Networked, Capacity(4)]
    private NetworkArray<int> Scores => default;

    public int GetScore(PlayerRef playerRef)
    {
        int index = GetPlayerIndex(playerRef);

        if (index < 0)
            return 0;

        return Scores[index];
    }

    public void AddScore(PlayerRef playerRef, int score)
    {
        if (!Object.HasStateAuthority)
            return;

        int index = GetPlayerIndex(playerRef);

        if (index < 0)
            return;

        Scores.Set(index, Scores[index] + score);

        Debug.Log($"Score 변경 : {playerRef} / +{score} / 현재 점수 : {Scores[index]}");
    }

    public void ResetScores()
    {
        if (!Object.HasStateAuthority)
            return;

        for (int i = 0; i < 4; i++)
            Scores.Set(i, 0);
    }

    private int GetPlayerIndex(PlayerRef playerRef)
    {
        int index = 0;

        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (player == playerRef)
                return index;

            index++;
        }

        return -1;
    }
}