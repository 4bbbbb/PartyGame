
using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AlienManager : NetworkBehaviour
{
    public static AlienManager Instance { get; private set; }

    // Alien이 선정되면 DodgeItManager에서 받을 수 있는 이벤트
    public event Action<PlayerRef> OnAlienSelected;

    [Header("<< Character >>")]
    [SerializeField] private CharacterDatabase characterDatabase;

    [Header("<< Panel >>")]
    [SerializeField] private GameObject alienPanel;
    [SerializeField] private GameObject threePlayerPanel;
    [SerializeField] private GameObject fourPlayerPanel;

    [Header("<< Cards >>")]
    [SerializeField] private AlienCardUI[] threePlayerCards;
    [SerializeField] private AlienCardUI[] fourPlayerCards;

    private AlienCardUI[] currentCards;

    // 각 카드에 선택한 플레이어
    [Networked, Capacity(4)]
    private NetworkArray<PlayerRef> SelectedCards => default;

    // 어떤 카드가 Alien인지
    [Networked, Capacity(4)]
    private NetworkArray<NetworkBool> CardIsAlien => default;

    // 최종 Alien 플레이어
    [Networked]
    public PlayerRef AlienPlayer { get; private set; }

    // 모든 플레이어의 카드 선택 완료 여부
    [Networked]
    public NetworkBool IsSelectionComplete { get; private set; }

    // 결과 공개 완료 여부
    [Networked]
    public NetworkBool IsRevealComplete { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }


    public override void Spawned()
    {
        Debug.Log("===== AlienManager Spawned =====");

        SetupAlienUI();

        if (Runner.IsServer)
            InitializeAlienCard();
    }


     #region < Setup >

    // Dodge It!의 카드 선택 UI를 표시할 때 호출
    public void SetupAlienUI()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        int playerCount = Runner.ActivePlayers.Count();

        Debug.Log($"AlienManager 플레이어 수 : {playerCount}");

        alienPanel.SetActive(true);
        threePlayerPanel.SetActive(false);
        fourPlayerPanel.SetActive(false);

        if (playerCount == 3)
        {
            Debug.Log("3명 → 3인 카드 패널");

            threePlayerPanel.SetActive(true);
            currentCards = threePlayerCards;
        }
        else if (playerCount == 4)
        {
            Debug.Log("4명 → 4인 카드 패널");

            fourPlayerPanel.SetActive(true);
            currentCards = fourPlayerCards;
        }
        else
        {
            currentCards = null;

            Debug.LogWarning(
                $"AlienManager는 3~4명을 지원합니다. 현재 인원: {playerCount}"
            );

            return;
        }

        SetupCards(currentCards);
    }

    private void SetupCards(AlienCardUI[] cards)
    {
        List<PlayerRef> players = Runner.ActivePlayers
            .OrderBy(player => player.RawEncoded)
            .ToList();

        for (int i = 0; i < cards.Length; i++)
        {
            if (i >= players.Count)
            {
                cards[i].Clear();
                continue;
            }

            cards[i].SetPlayer(i, players[i]);
        }
    }

    #endregion

    #region Initialize Alien

    // Host가 Alien 카드를 무작위로 선정
    private void InitializeAlienCard()
    {
        int playerCount = Runner.ActivePlayers.Count();

        if (playerCount < 3 || playerCount > 4)
            return;

        for (int i = 0; i < 4; i++)
        {
            SelectedCards.Set(i, default);
            CardIsAlien.Set(i, false);
        }

        AlienPlayer = default;
        IsSelectionComplete = false;
        IsRevealComplete = false;

        int alienIndex = UnityEngine.Random.Range(0, playerCount);

        CardIsAlien.Set(alienIndex, true);

        Debug.Log($"===== Alien 카드 선정 ===== 인덱스: {alienIndex}");
    }

    #endregion

    #region Select Card

    // 카드 버튼의 OnClick에서 호출
    public void SelectCard(int cardIndex)
    {
        if (IsSelectionComplete || IsRevealComplete)
            return;

        Debug.Log($"Alien 카드 선택 요청: {cardIndex}");

        RPC_SelectCard(cardIndex);
    }

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority,
        HostMode = RpcHostMode.SourceIsHostPlayer
    )]
    private void RPC_SelectCard(int cardIndex, RpcInfo info = default)
    {
        PlayerRef selectingPlayer = info.Source;
        int playerCount = Runner.ActivePlayers.Count();

        if (IsSelectionComplete || IsRevealComplete)
            return;

        if (cardIndex < 0 || cardIndex >= playerCount)
            return;

        if (!Runner.ActivePlayers.Contains(selectingPlayer))
            return;

        if (HasPlayerAlreadySelected(selectingPlayer))
        {
            Debug.Log($"{selectingPlayer}는 이미 카드를 선택했습니다.");
            return;
        }

        if (SelectedCards[cardIndex] != default)
        {
            Debug.Log($"카드 {cardIndex}는 이미 선택됐습니다.");
            return;
        }

        SelectedCards.Set(cardIndex, selectingPlayer);

        Debug.Log(
            $"카드 선택 성공: Card {cardIndex} → {selectingPlayer}"
        );

        RPC_UpdateCardUI(cardIndex, selectingPlayer);

        CheckSelectionComplete(playerCount);
    }

    private bool HasPlayerAlreadySelected(PlayerRef player)
    {
        for (int i = 0; i < 4; i++)
        {
            if (SelectedCards[i] == player)
                return true;
        }

        return false;
    }

    #endregion

    #region Card UI

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateCardUI(int cardIndex, PlayerRef selectingPlayer)
    {
        if (currentCards == null)
            return;

        if (cardIndex < 0 || cardIndex >= currentCards.Length)
            return;

        PlayerNetwork playerNetwork = FindPlayerNetwork(selectingPlayer);

        if (playerNetwork == null)
        {
            Debug.LogWarning(
                $"PlayerNetwork를 찾을 수 없습니다: {selectingPlayer}"
            );
            return;
        }

        if (characterDatabase == null ||
            characterDatabase.characters == null)
        {
            Debug.LogError("CharacterDatabase가 연결되지 않았습니다.");
            return;
        }

        string nickname = playerNetwork.Nickname.ToString();
        int characterIndex = playerNetwork.CharacterIndex;

        if (characterIndex < 0 ||
            characterIndex >= characterDatabase.characters.Length)
        {
            Debug.LogWarning($"잘못된 CharacterIndex: {characterIndex}");
            return;
        }

        CharacterData characterData =
            characterDatabase.characters[characterIndex];

        currentCards[cardIndex].SetSelected(
            nickname,
            characterData.characterColor
        );

        Debug.Log(
            $"카드 UI 갱신: {nickname} / {characterData.characterName}"
        );
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
            if (player.PlayerRef == playerRef)
                return player;
        }

        return null;
    }

    #endregion

    #region Selection Complete

    private void CheckSelectionComplete(int playerCount)
    {
        int selectedCount = 0;

        for (int i = 0; i < playerCount; i++)
        {
            if (SelectedCards[i] != default)
                selectedCount++;
        }

        Debug.Log($"현재 카드 선택: {selectedCount}/{playerCount}");

        if (selectedCount < playerCount)
            return;

        IsSelectionComplete = true;

        Debug.Log("===== 모든 플레이어 선택 완료 =====");

        RPC_RevealCards();
    }

    #endregion

    #region Reveal Alien

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_RevealCards()
    {
        Debug.Log("===== Alien 카드 공개 시작 =====");

        if (currentCards == null)
            return;

        foreach (AlienCardUI card in currentCards)
            card.FlipCard();

        RevealResults();
    }

    private void RevealResults()
    {
        if (IsRevealComplete || !Runner.IsServer)
            return;

        int playerCount = Runner.ActivePlayers.Count();

        for (int cardIndex = 0; cardIndex < playerCount; cardIndex++)
        {
            if (!CardIsAlien[cardIndex])
                continue;

            AlienPlayer = SelectedCards[cardIndex];
            IsRevealComplete = true;

            Debug.Log(
                $"===== ALIEN 선정 =====\n" +
                $"Alien Card: {cardIndex}\n" +
                $"Alien Player: {AlienPlayer}"
            );

            RPC_ShowResults(cardIndex, AlienPlayer);
            return;
        }

        Debug.LogError("Alien 카드를 찾을 수 없습니다.");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowResults(int alienCardIndex, PlayerRef alienPlayer)
    {
        Debug.Log(
            $"===== Alien 결과 공개 =====\n" +
            $"Alien Card: {alienCardIndex}\n" +
            $"Alien Player: {alienPlayer}"
        );

        if (currentCards == null)
            return;

        StartCoroutine(
            ShowAlienResultSequence(alienCardIndex, alienPlayer)
        );
    }

    private IEnumerator ShowAlienResultSequence(
        int alienCardIndex,
        PlayerRef alienPlayer)
    {
        for (int i = 0; i < currentCards.Length; i++)
        {
            currentCards[i].ShowResult(i == alienCardIndex);
        }

        yield return new WaitForSeconds(1.5f);

        foreach (AlienCardUI card in currentCards)
            card.gameObject.SetActive(false);

        alienPanel.SetActive(false);

        Debug.Log($"Alien 선정 완료: {alienPlayer}");

        // 다음 단계에서 DodgeItManager와 연결할 지점
        OnAlienSelected?.Invoke(alienPlayer);
    }

    #endregion

    #region < Reset >

    public void ResetAlien()
    {
        if (Object == null || !Object.HasStateAuthority)
            return;

        Debug.Log("===== AlienManager Reset 시작 =====");

        IsSelectionComplete = false;
        IsRevealComplete = false;
        AlienPlayer = default;

        for (int i = 0; i < 4; i++)
        {
            SelectedCards.Set(i, default);
            CardIsAlien.Set(i, false);
        }

        if (currentCards != null)
        {
            foreach (AlienCardUI card in currentCards)
                card.ResetCard();
        }

        SetupUI();
        InitializeAlienCard();

        Debug.Log("===== AlienManager Reset 완료 =====");
    }

    #endregion
}
