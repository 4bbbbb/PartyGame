using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CharacterSelectUI : MonoBehaviour
{
    public static CharacterSelectUI Instance { get; private set; }

    [Header("<< Character >>")]
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private SkinnedMeshRenderer characterRenderer;
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private PlayerNetwork playerNetwork;

    [Header("<< Buttons >>")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button selectButton;

    [Header("<< Dots >>")]
    [SerializeField] private GameObject[] pinkDots;
    [SerializeField] private GameObject[] whiteDots;

    private int currentIndex = 0;
    private bool isSelected = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        leftButton.onClick.AddListener(OnClickLeft);
        rightButton.onClick.AddListener(OnClickRight);
        selectButton.onClick.AddListener(OnClickSelect);

        FindMyPlayerNetwork();

        UpdateCharacter(false);
    }

    private void FindMyPlayerNetwork()
    {
        PlayerNetwork[] players =
            FindObjectsByType<PlayerNetwork>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (PlayerNetwork player in players)
        {
            if (player.Object != null && player.Object.HasInputAuthority)
            {
                playerNetwork = player;
                return;
            }
        }
    }

    private void OnClickLeft()
    {
        if (isSelected)
            return;

        int characterCount = characterDatabase.characters.Length;

        for (int i = 0; i < characterCount; i++)
        {
            currentIndex--;

            if (currentIndex < 0)
            {
                currentIndex = characterCount - 1;
            }

            if (!IsCharacterTaken(currentIndex))
            {
                UpdateCharacter(true);
                return;
            }
        }
    }

    private void OnClickRight()
    {
        if (isSelected)
            return;

        int characterCount = characterDatabase.characters.Length;

        for (int i = 0; i < characterCount; i++)
        {
            currentIndex++;

            if (currentIndex >= characterCount)
            {
                currentIndex = 0;
            }

            if (!IsCharacterTaken(currentIndex))
            {
                UpdateCharacter(true);
                return;
            }
        }
    }

    private void OnClickSelect()
    {
        if (isSelected)
            return;

        if (playerNetwork == null)
        {
            FindMyPlayerNetwork();
        }

        if (playerNetwork == null)
        {
            Debug.LogWarning("내 PlayerNetwork를 찾지 못했습니다.");
            return;
        }

        playerNetwork.RPC_RequestCharacterSelect(currentIndex);
    }

    private bool IsCharacterTaken(int index)
    {
        PlayerNetwork[] players =
            FindObjectsByType<PlayerNetwork>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (PlayerNetwork player in players)
        {
            // 자기 자신은 제외
            if (player == playerNetwork)
                continue;

            if (player.CharacterIndex == index)
                return true;
        }

        return false;
    }

    private void UpdateCharacter(bool playHi)
    {
        CharacterData data = characterDatabase.characters[currentIndex];

        // Material 변경
        Material[] materials = characterRenderer.materials;
        materials[0] = data.characterMaterial;
        characterRenderer.materials = materials;

        // 이름 변경
        characterNameText.text = data.characterName;

        // Dot 변경
        UpdateDots();

        // 캐릭터 변경 시 HI 재생
        if (playHi)
        {
            PlayHiAnimation();
        }
    }

    public void OnCharacterSelectSuccess(int selectedIndex)
    {
        currentIndex = selectedIndex;

        isSelected = true;

        characterAnimator.Play("Select", 0, 0f);

        SetSelectedUI();

        UpdateCharacter(false);
    }

    public void OnCharacterSelectFailed(int takenIndex)
    {
        Debug.Log(
            $"캐릭터 선택 실패 : {takenIndex} / 다른 캐릭터로 이동"
        );

        MoveToNextAvailableCharacter();
    }

    private void MoveToNextAvailableCharacter()
    {
        int characterCount = characterDatabase.characters.Length;

        for (int i = 0; i < characterCount; i++)
        {
            currentIndex++;

            if (currentIndex >= characterCount)
            {
                currentIndex = 0;
            }

            if (!IsCharacterTaken(currentIndex))
            {
                UpdateCharacter(true);
                return;
            }
        }

        Debug.LogWarning("선택 가능한 캐릭터가 없습니다.");
    }

    public void RefreshAvailableCharacters()
    {
        if (isSelected)
            return;

        if (IsCharacterTaken(currentIndex))
        {
            MoveToNextAvailableCharacter();
        }
    }

    private void SetSelectedUI()
    {
        leftButton.gameObject.SetActive(false);
        rightButton.gameObject.SetActive(false);

        selectButton.interactable = false;
    }

    private void UpdateDots()
    {
        for (int i = 0; i < pinkDots.Length; i++)
        {
            bool isSelected = i == currentIndex;

            pinkDots[i].SetActive(isSelected);
            whiteDots[i].SetActive(!isSelected);
        }
    }
    

    private void PlayHiAnimation()
    {
        characterAnimator.Play("Hi", 0, 0f);
    }
   
    public void OnCharacterSelectSuccess()
    {
        isSelected = true;

        characterAnimator.Play("Select", 0, 0f);

        leftButton.gameObject.SetActive(false);
        rightButton.gameObject.SetActive(false);

        selectButton.interactable = false;
    }


}