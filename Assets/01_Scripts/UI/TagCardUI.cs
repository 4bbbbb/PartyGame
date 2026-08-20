using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TagCardUI : MonoBehaviour
{
    [Header("<< UI >>")]
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private Button cardButton;


    [Header("<< Card >>")]
    [SerializeField] private GameObject front;
    [SerializeField] private GameObject back;


    [Header("<< Result >>")]
    [SerializeField] private GameObject selectedImage;
    [SerializeField] private GameObject tagImage;
    [SerializeField] private GameObject normalImage;

    private int cardIndex;
    private PlayerRef playerRef;


    public PlayerRef PlayerRef => playerRef;
    public int CardIndex => cardIndex;


    #region < Setup >

    // TagManager가 카드에 플레이어 정보를 넣어주는 함수
    public void SetPlayer(int index, PlayerRef player, string nickname)
    {
        cardIndex = index;
        playerRef = player;

        nicknameText.text = nickname;

        gameObject.SetActive(true);


        // --------------------------------
        // 초기 상태
        // --------------------------------

        // 처음에는 카드 뒷면을 보여준다.
        back.SetActive(true);
        front.SetActive(false);

        // 선택 표시 제거
        selectedImage.SetActive(false);

        // 결과 표시 제거
        tagImage.SetActive(false);
        normalImage.SetActive(false);


        // 카드 선택 가능
        cardButton.interactable = true;


        // 기존 이벤트 제거
        cardButton.onClick.RemoveAllListeners();

        // 카드 클릭 이벤트 등록
        cardButton.onClick.AddListener(OnClickCard);
    }


    // 카드 초기화
    public void Clear()
    {
        cardIndex = -1;
        playerRef = default;

        nicknameText.text = "";


        selectedImage.SetActive(false);

        tagImage.SetActive(false);
        normalImage.SetActive(false);


        // 초기 상태 = Back
        back.SetActive(true);
        front.SetActive(false);


        // 비활성화
        cardButton.interactable = false;

        cardButton.onClick.RemoveAllListeners();

        gameObject.SetActive(false);
    }

    #endregion


    #region < Card Click >

    // 카드 클릭
    public void OnClickCard()
    {
        Debug.Log(
            $"카드 클릭 : " +
            $"Card = {cardIndex}, " +
            $"Card Player = {playerRef}"
        );

        PlayerNetwork myPlayer = FindMyPlayer();

        if (myPlayer == null)
        {
            Debug.LogError("내 PlayerNetwork를 찾을 수 없습니다.");
            return;
        }

        myPlayer.SelectTagCard(cardIndex);
    }

    private PlayerNetwork FindMyPlayer()
    {
        PlayerNetwork[] players =
            FindObjectsByType<PlayerNetwork>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (PlayerNetwork player in players)
        {
            if (player.Object.HasInputAuthority)
            {
                return player;
            }
        }

        return null;
    }

    #endregion


    #region < Selection >

    // 내가 선택한 카드
    public void SetSelected()
    {
        Debug.Log($"카드 선택 표시 : Card {cardIndex}");


        // Back 상태에서 Selected 표시
        selectedImage.SetActive(true);


        // 이미 선택된 카드는 다시 선택 불가
        cardButton.interactable = false;
    }


    // 다른 사람이 선택한 카드
    public void SetDisabled()
    {
        cardButton.interactable = false;
    }

    #endregion


    #region < Reveal >

    // 카드 뒤집기
    public void FlipCard()
    {
        Debug.Log($"카드 뒤집기 : Card {cardIndex}");


        // Back OFF
        back.SetActive(false);


        // Front ON
        front.SetActive(true);


        // 더 이상 클릭 불가
        cardButton.interactable = false;
    }


    // 최종 결과 표시
    public void ShowResult(bool isTag)
    {
        if (isTag)
        {
            Debug.Log($"Card {cardIndex} → TAG");

            tagImage.SetActive(true);
            normalImage.SetActive(false);
        }
        else
        {
            Debug.Log($"Card {cardIndex} → NORMAL");

            tagImage.SetActive(false);
            normalImage.SetActive(true);
        }
    }

    #endregion
}