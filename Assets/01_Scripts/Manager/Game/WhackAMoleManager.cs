using Fusion;
using UnityEngine;

public class WhackAMoleManager : NetworkBehaviour
{
    #region < Data >

    [Header("<< Game Data >>")]
    [SerializeField] private WhackAMoleData gameData;

    #endregion


    #region < Networked Data >

    // 현재 라운드
    [Networked]
    public int CurrentRound { get; private set; }

    // 술래의 현재 목숨
    [Networked]
    public int TagHP { get; private set; }

    // 술래
    [Networked]
    public PlayerRef TagPlayer { get; private set; }

    // 현재 게임 상태
    [Networked]
    public WhackAMoleState State { get; private set; }

    #endregion


    #region < Spawn >

    public override void Spawned()
    {
        Debug.Log("===== WhackAMoleManager Spawned =====");

        if (!Object.HasStateAuthority)
            return;

        InitializeGame();
    }

    #endregion


    #region < Initialize >

    private void InitializeGame()
    {
        if (gameData == null)
        {
            Debug.LogError("WhackAMoleData가 연결되지 않았습니다.");
            return;
        }

        if (TagManager.Instance == null)
        {
            Debug.LogError("TagManager를 찾을 수 없습니다.");
            return;
        }

        // 게임 초기화
        CurrentRound = 1;
        TagHP = gameData.TagHP;
        State = WhackAMoleState.Waiting;

        Debug.Log(
            $"===== WhackAMole 초기화 =====\n" +
            $"HP : {TagHP}\n" +
            $"Round : {CurrentRound}"
        );
    }

    #endregion
}