
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AlienCardUI : MonoBehaviour
{
    [Header("<< Profile >>")]
    [SerializeField] private Image profileImage;
    [SerializeField] private TMP_Text nicknameText;

    [Header("<< Button >>")]
    [SerializeField] private Button cardButton;

    [Header("<< Card >>")]
    [SerializeField] private GameObject front;
    [SerializeField] private GameObject back;

    [Header("<< Result >>")]
    [SerializeField] private GameObject alienImage;
    [SerializeField] private GameObject normalImage;

    private int cardIndex;
    private PlayerRef playerRef;

    public PlayerRef PlayerRef => playerRef;
    public int CardIndex => cardIndex;

    private void Awake()
    {
        profileImage.gameObject.SetActive(false);
        nicknameText.gameObject.SetActive(false);
    }

    #region Setup

    public void SetPlayer(int index, PlayerRef player)
    {
        cardIndex = index;
        playerRef = player;

        gameObject.SetActive(true);

        back.SetActive(true);
        front.SetActive(false);

        alienImage.SetActive(false);
        normalImage.SetActive(false);

        profileImage.gameObject.SetActive(false);
        nicknameText.gameObject.SetActive(false);

        cardButton.interactable = true;
        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(OnClickCard);
    }

    public void Clear()
    {
        cardIndex = -1;
        playerRef = default;

        profileImage.gameObject.SetActive(false);
        nicknameText.gameObject.SetActive(false);

        alienImage.SetActive(false);
        normalImage.SetActive(false);

        back.SetActive(true);
        front.SetActive(false);

        cardButton.interactable = false;
        cardButton.onClick.RemoveAllListeners();

        gameObject.SetActive(false);
    }

    #endregion

    #region Card Click

    public void OnClickCard()
    {
        Debug.Log(
            $"[ALIEN CARD CLICK] Index={cardIndex}, Player={playerRef}"
        );

        if (AlienManager.Instance == null)
        {
            Debug.LogError("AlienManager가 없습니다.");
            return;
        }

        AlienManager.Instance.SelectCard(cardIndex);
    }

    #endregion

    #region Selection

    public void SetSelected(string nickname, Color characterColor)
    {
        nicknameText.text = nickname;
        profileImage.color = characterColor;

        profileImage.gameObject.SetActive(true);
        nicknameText.gameObject.SetActive(true);

        Debug.Log($"Alien 카드 선택 정보 표시: {nickname}");
    }

    public void SetDisabled()
    {
        cardButton.interactable = false;
    }

    #endregion

    #region Reveal

    public void FlipCard()
    {
        back.SetActive(false);
        front.SetActive(true);

        cardButton.interactable = false;
    }

    public void ShowResult(bool isAlien)
    {
        alienImage.SetActive(isAlien);
        normalImage.SetActive(!isAlien);

        Debug.Log(
            $"Card {cardIndex} → {(isAlien ? "ALIEN" : "NORMAL")}"
        );
    }

    #endregion

    #region Reset

    public void ResetCard()
    {
        profileImage.gameObject.SetActive(false);
        nicknameText.gameObject.SetActive(false);

        alienImage.SetActive(false);
        normalImage.SetActive(false);

        back.SetActive(true);
        front.SetActive(false);

        cardButton.interactable = true;
        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(OnClickCard);
    }

    #endregion
}
