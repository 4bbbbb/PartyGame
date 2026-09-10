using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class TagHoleSelectUI : MonoBehaviour
{
    [Header("<< Manager >>")]
    [SerializeField] private WhackAMoleManager whackAMoleManager;

    [Header("<< Panel >>")]
    [SerializeField] private GameObject panel;

    [Header("<< Selected >>")]
    [SerializeField] private GameObject selectedObject;

    [Header("<< Buttons >>")]
    [SerializeField] private Button[] holeButtons;

    private Player_InputActions inputActions;

    private bool isSelecting;


    private void Awake()
    {
        inputActions = new Player_InputActions();

        panel.SetActive(false);

        if (selectedObject != null)
            selectedObject.SetActive(false);
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

        isSelecting = false;

        SetButtonInteractable(false);

        if (whackAMoleManager == null)
            return;

        if (whackAMoleManager.Runner == null)
            return;

        // TAG인지 확인
        bool isTag = whackAMoleManager.Runner.LocalPlayer == whackAMoleManager.TagPlayer;

        if (isTag)
        {
            isSelecting = true;

            SetButtonInteractable(true);

            inputActions.WhackAMole.Enable();
        }
    }


    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);

        isSelecting = false;

        SetButtonInteractable(false);

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

        whackAMoleManager.SelectTagHole(hole);

        // 중복 선택 방지
        isSelecting = false;

        SetButtonInteractable(false);

        inputActions.WhackAMole.Disable();
    }

    #endregion


    #region < Complete >

    public void ShowComplete()
    {
        // 선택 중인 상태 종료
        isSelecting = false;

        SetButtonInteractable(false);

        inputActions.WhackAMole.Disable();

        // 선택 완료 문구 표시
        if (selectedObject != null)
            selectedObject.SetActive(true);

        // TagSelectPanel 숨기기
        if (panel != null)
            panel.SetActive(false);
    }

    #endregion


    #region < Button >

    private void SetButtonInteractable(bool value)
    {
        if (holeButtons == null)
            return;

        foreach (Button button in holeButtons)
        {
            if (button != null)
                button.interactable = value;
        }
    }

    #endregion
}