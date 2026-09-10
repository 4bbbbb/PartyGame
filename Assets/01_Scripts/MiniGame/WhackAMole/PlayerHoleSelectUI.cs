using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHoleSelectUI : MonoBehaviour
{
    [Header("<< Manager >>")]
    [SerializeField] private WhackAMoleManager whackAMoleManager;

    [Header("<< Panel >>")]
    [SerializeField] private GameObject panel;

    [Header("<< Player Select >>")]
    [SerializeField] private TMP_Text[] playerSelectTexts;

    [Header("<< Selected >>")]
    [SerializeField] private GameObject selectedObject;   

    private Player_InputActions inputActions;

    private bool isSelecting;


    private void Awake()
    {
        inputActions = new Player_InputActions();

        panel.SetActive(false);

        if (selectedObject != null)
            selectedObject.SetActive(false);

        if (playerSelectTexts != null)
        {
            foreach (TMP_Text text in playerSelectTexts)
            {
                if (text != null)
                    text.gameObject.SetActive(false);
            }
        }
    }


    private void OnEnable()
    {
        inputActions.WhackAMole.Select1.performed += OnSelect1;
        inputActions.WhackAMole.Select2.performed += OnSelect2;
        inputActions.WhackAMole.Select3.performed += OnSelect3;
        inputActions.WhackAMole.Select4.performed += OnSelect4;
    }


    private void OnDisable()
    {
        inputActions.WhackAMole.Select1.performed -= OnSelect1;
        inputActions.WhackAMole.Select2.performed -= OnSelect2;
        inputActions.WhackAMole.Select3.performed -= OnSelect3;
        inputActions.WhackAMole.Select4.performed -= OnSelect4;

        inputActions.WhackAMole.Disable();
    }


    #region < Show / Hide >

    public void Show()
    {
        if (panel == null)
            return;

        panel.SetActive(true);

        if (selectedObject != null)
            selectedObject.SetActive(false);

        HidePlayerSelectTexts();

        isSelecting = false;

        if (whackAMoleManager == null)
            return;

        if (whackAMoleManager.Runner == null)
            return;

        bool isTag = whackAMoleManager.Runner.LocalPlayer == whackAMoleManager.TagPlayer;

        if (isTag)
            return;

        isSelecting = true;
        inputActions.WhackAMole.Enable();
    }


    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);

        isSelecting = false;

        inputActions.WhackAMole.Disable();
    }

    #endregion


    #region < Keyboard >

    private void OnSelect1(InputAction.CallbackContext context)
    {
        if (!isSelecting)
            return;

        Select(WhackAMoleManager.HoleType.Hole1);
    }


    private void OnSelect2(InputAction.CallbackContext context)
    {
        if (!isSelecting)
            return;

        Select(WhackAMoleManager.HoleType.Hole2);
    }


    private void OnSelect3(InputAction.CallbackContext context)
    {
        if (!isSelecting)
            return;

        Select(WhackAMoleManager.HoleType.Hole3);
    }


    private void OnSelect4(InputAction.CallbackContext context)
    {
        if (!isSelecting)
            return;

        Select(WhackAMoleManager.HoleType.Hole4);
    }

    #endregion


    #region < Button >

    public void OnClick1()
    {
        if (!isSelecting)
            return;

        Select(WhackAMoleManager.HoleType.Hole1);
    }


    public void OnClick2()
    {
        if (!isSelecting)
            return;

        Select(WhackAMoleManager.HoleType.Hole2);
    }


    public void OnClick3()
    {
        if (!isSelecting)
            return;

        Select(WhackAMoleManager.HoleType.Hole3);
    }


    public void OnClick4()
    {
        if (!isSelecting)
            return;

        Select(WhackAMoleManager.HoleType.Hole4);
    }

    #endregion


    #region < Select >

    private void Select(WhackAMoleManager.HoleType hole)
    {
        if (!isSelecting)
            return;

        if (whackAMoleManager == null)
            return;

        whackAMoleManager.SelectPlayerHole(hole);

        // 중복 입력 방지
        isSelecting = false;

        inputActions.WhackAMole.Disable();        
    }

    #endregion


    #region < Player Select Text >

    public void ShowPlayerSelected(int playerIndex)
    {
        if (playerSelectTexts == null)
            return;

        if (playerIndex < 0 || playerIndex >= playerSelectTexts.Length)
            return;

        TMP_Text text = playerSelectTexts[playerIndex];

        if (text == null)
            return;

        text.text = "선택 완료";

        text.gameObject.SetActive(true);
    }


    public void HidePlayerSelectTexts()
    {
        if (playerSelectTexts == null)
            return;

        foreach (TMP_Text text in playerSelectTexts)
        {
            if (text != null)
                text.gameObject.SetActive(false);
        }
    }

    #endregion


    public void ShowComplete()
    {
        isSelecting = false;

        inputActions.WhackAMole.Disable();

        if (panel != null)
            panel.SetActive(false);

        HidePlayerSelectTexts();

        if (selectedObject != null)
            selectedObject.SetActive(true);
    }

    public void HideComplete()
    {
        if (selectedObject != null)
            selectedObject.SetActive(false);
    }
}