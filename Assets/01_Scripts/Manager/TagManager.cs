using Fusion;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TagManager : NetworkBehaviour
{
    public static TagManager Instance { get; private set; }


    [Header("<< Panel >>")]
    [SerializeField] private GameObject threePlayerPanel;
    [SerializeField] private GameObject fourPlayerPanel;


    [Header("<< Cards >>")]
    [SerializeField] private TagCardUI[] threePlayerCards;
    [SerializeField] private TagCardUI[] fourPlayerCards;

   
    [Networked, Capacity(4)]
    private NetworkArray<PlayerRef> SelectedCards => default;


    // 어떤 카드가 TAG인지
    // Host가 랜덤으로 하나 결정
    [Networked, Capacity(4)]
    private NetworkArray<NetworkBool> CardIsTag => default;


    // 최종 술래
    [Networked]
    public PlayerRef TagPlayer { get; private set; }


    // 모든 플레이어가 카드 선택 완료
    [Networked]
    public NetworkBool IsSelectionComplete { get; private set; }


    // 카드 공개 완료
    [Networked]
    public NetworkBool IsRevealComplete { get; private set; }

    private TagCardUI[] currentCards;


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
        Debug.Log("===== TagManager Start 호출 =====");

        if (NetworkManager.Instance == null)
        {
            Debug.LogError("NetworkManager 없음");
            return;
        }

        NetworkRunner runner = NetworkManager.Instance.GetRunner();

        if (runner == null)
        {
            Debug.LogError("Runner 없음");
            return;
        }

        Debug.Log($"현재 플레이어 수 : {runner.ActivePlayers.Count()}");
    }


    public override void Spawned()
    {
        Debug.Log("===== TagManager Spawned =====");

        SetupUI();


        // Host만 TAG 카드를 랜덤 결정
        if (Runner.IsServer)
        {
            InitializeTagCard();
        }
    }


    #region < Setup >

    private void SetupUI()
    {
        int playerCount = Runner.ActivePlayers.Count();

        Debug.Log(
            $"TagManager 플레이어 수 : {playerCount}"
        );


        // 일단 둘 다 끄기
        threePlayerPanel.SetActive(false);
        fourPlayerPanel.SetActive(false);


        if (playerCount == 3)
        {
            Debug.Log("3명 → 3인 카드 Panel");

            threePlayerPanel.SetActive(true);

            currentCards = threePlayerCards;

            SetupCards(currentCards);
        }
        else if (playerCount == 4)
        {
            Debug.Log("4명 → 4인 카드 Panel");

            fourPlayerPanel.SetActive(true);

            currentCards = fourPlayerCards;

            SetupCards(currentCards);
        }
        else
        {
            Debug.LogWarning(
                $"TagManager는 3~4명만 지원합니다. " +
                $"현재 플레이어 : {playerCount}"
            );
        }
    }


    private void SetupCards(TagCardUI[] cards)
    {
        List<PlayerRef> players = Runner.ActivePlayers
            .OrderBy(player => player.RawEncoded)
            .ToList();


        for (int i = 0; i < cards.Length; i++)
        {
            if (i < players.Count)
            {
                PlayerRef player = players[i];

                PlayerNetwork playerNetwork = FindPlayerNetwork(player);


                if (playerNetwork != null)
                {
                    cards[i].SetPlayer(
                        i,
                        player,
                        playerNetwork.Nickname.ToString()
                    );
                }
            }
            else
            {
                cards[i].Clear();
            }
        }
    }


    private PlayerNetwork FindPlayerNetwork(PlayerRef playerRef)
    {
        PlayerNetwork[] players =
            FindObjectsByType<PlayerNetwork>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );


        return players.FirstOrDefault(player => player.PlayerRef == playerRef);
    }

    #endregion


    #region < Initialize TAG >

    // Host가 카드 하나를 랜덤으로 TAG로 지정    
    private void InitializeTagCard()
    {
        int playerCount = Runner.ActivePlayers.Count();

        if (playerCount < 3 || playerCount > 4)
            return;


        // 초기화
        for (int i = 0; i < 4; i++)
        {
            SelectedCards.Set(i, default);
            CardIsTag.Set(i, false);
        }


        // 랜덤으로 TAG 카드 하나 결정
        int tagIndex = Random.Range(0, playerCount);

        CardIsTag.Set(tagIndex, true);


        Debug.Log(
            $"===== TAG 카드 결정 =====\n" +
            $"TAG Card Index : {tagIndex}"
        );
    }

    #endregion


    #region < Select Card >

    // 카드 버튼을 눌렀을 때 호출   
    public void ServerSelectCard(int cardIndex, PlayerRef selectingPlayer)
    {
        if (!Runner.IsServer)
            return;

        if (IsSelectionComplete)
            return;

        int playerCount = Runner.ActivePlayers.Count();

        Debug.Log(
            $"===== 카드 선택 요청 =====\n" +
            $"선택자 : {selectingPlayer}\n" +
            $"카드 : {cardIndex}"
        );


        // 카드 번호 확인
        if (cardIndex < 0 || cardIndex >= playerCount)
        {
            Debug.LogWarning($"잘못된 카드 번호 : {cardIndex}");

            return;
        }


        // 이미 카드를 고른 플레이어인지 확인
        if (HasPlayerAlreadySelected(selectingPlayer))
        {
            Debug.Log($"{selectingPlayer}는 이미 카드를 선택했습니다.");

            return;
        }


        // 이미 다른 사람이 선택한 카드인지 확인
        if (SelectedCards[cardIndex] != default)
        {
            Debug.Log($"Card {cardIndex}는 이미 선택됐습니다.");

            return;
        }


        // 카드 선택 기록
        SelectedCards.Set(cardIndex, selectingPlayer);


        Debug.Log(
            $"카드 선택 성공 : " +
            $"Card {cardIndex} → {selectingPlayer}"
        );


        // 모든 클라이언트 UI 갱신
        RPC_UpdateCardUI(cardIndex, selectingPlayer);


        // 모두 선택했는지 확인
        CheckSelectionComplete(playerCount);
    }


    private bool HasPlayerAlreadySelected(PlayerRef player)
    {
        for (int i = 0; i < 4; i++)
        {
            if (SelectedCards[i] == player)
            {
                return true;
            }
        }

        return false;
    }

    #endregion


    #region < Card UI >

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateCardUI(int cardIndex, PlayerRef selectingPlayer)
    {
        if (currentCards == null)
            return;


        if (cardIndex < 0 || cardIndex >= currentCards.Length)
            return;


        TagCardUI card = currentCards[cardIndex];


        // 선택된 카드 잠금
        card.SetSelected();


        Debug.Log(
            $"카드 UI 갱신 : " +
            $"Card {cardIndex} / " +
            $"Player {selectingPlayer}"
        );
    }

    #endregion


    #region < Selection Complete >

    private void CheckSelectionComplete(int playerCount)
    {
        int selectedCount = 0;


        for (int i = 0; i < playerCount; i++)
        {
            if (SelectedCards[i] != default)
            {
                selectedCount++;
            }
        }


        Debug.Log($"현재 카드 선택 : " +  $"{selectedCount} / {playerCount}");


        if (selectedCount >= playerCount)
        {
            IsSelectionComplete = true;

            Debug.Log("===== 모든 플레이어 선택 완료 =====");

            RPC_RevealCards();
        }
    }

    #endregion


    #region < Reveal >

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_RevealCards()
    {
        Debug.Log("===== 카드 뒤집기 시작 =====");


        if (currentCards == null)
            return;


        // 모든 카드 뒤집기
        for (int i = 0; i < currentCards.Length; i++)
        {
            currentCards[i].FlipCard();
        }


        // 현재는 테스트를 위해 바로 결과 공개
        // 나중에 카드 뒤집기 애니메이션이 끝난 뒤 호출하도록 변경
        RevealResults();
    }


    private void RevealResults()
    {
        if (IsRevealComplete)
            return;


        if (!Runner.IsServer)
            return;


        IsRevealComplete = true;


        int playerCount = Runner.ActivePlayers.Count();


        // TAG 카드 찾기
        for (int cardIndex = 0; cardIndex < playerCount; cardIndex++)
        {
            if (CardIsTag[cardIndex])
            {
                TagPlayer = SelectedCards[cardIndex];


                Debug.Log(
                    $"===== TAG PLAYER =====\n" +
                    $"TAG Card : {cardIndex}\n" +
                    $"TAG Player : {TagPlayer}"
                );


                RPC_ShowResults(cardIndex, TagPlayer);

                break;
            }
        }
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowResults(int tagCardIndex, PlayerRef tagPlayer)
    {
        Debug.Log(
            $"===== 카드 결과 공개 =====\n" +
            $"TAG Card : {tagCardIndex}\n" +
            $"TAG Player : {tagPlayer}"
        );


        if (currentCards == null)
            return;


        for (int i = 0; i < currentCards.Length; i++)
        {
            currentCards[i].ShowResult(
                i == tagCardIndex
            );
        }


        // 여기까지 오면 TagPlayer가 확정됨
        // 나중에 WhackaMoleManager 시작
    }

    #endregion
}