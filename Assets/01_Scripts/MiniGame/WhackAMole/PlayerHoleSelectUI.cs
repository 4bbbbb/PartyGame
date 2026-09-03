using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHoleSelectUI : MonoBehaviour
{
    [Header("<< Manager >>")]
    [SerializeField] private WhackAMoleManager whackAMoleManager;

    [Header("<< Panel >>")]
    [SerializeField] private GameObject panel;

    [Header("<< Selected >>")]
    [SerializeField] private GameObject selectedObject;

    private WhackAMoleInputActions inputActions;

    private bool isSelecting;


    private void Awake()
    {
        inputActions = new WhackAMoleInputActions();

        panel.SetActive(false);

        if (selectedObject != null)
            selectedObject.SetActive(false);
    }


    private void OnEnable()
    {
        inputActions.WhackAMole.SelectW.performed += OnSelectW;
        inputActions.WhackAMole.SelectA.performed += OnSelectA;
        inputActions.WhackAMole.SelectS.performed += OnSelectS;
        inputActions.WhackAMole.SelectD.performed += OnSelectD;
    }


    private void OnDisable()
    {
        inputActions.WhackAMole.SelectW.performed -= OnSelectW;
        inputActions.WhackAMole.SelectA.performed -= OnSelectA;
        inputActions.WhackAMole.SelectS.performed -= OnSelectS;
        inputActions.WhackAMole.SelectD.performed -= OnSelectD;

        inputActions.WhackAMole.Disable();
    }


    #region < Show / Hide >

    public void Show()
    {
        if (panel == null)
            return;

        panel.SetActive(false);

        if (selectedObject != null)
            selectedObject.SetActive(false);

        isSelecting = false;

        if (whackAMoleManager == null)
            return;

        if (whackAMoleManager.Runner == null)
            return;

        // TAG는 이 UI를 사용하지 않음
        bool isTag =
            whackAMoleManager.Runner.LocalPlayer ==
            whackAMoleManager.TagPlayer;

        if (isTag)
            return;

        panel.SetActive(true);

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

    private void OnSelectW(InputAction.CallbackContext context)
    {
        if (!isSelecting)
            return;

        Select(WhackAMoleManager.HoleType.W);
    }


    private void OnSelectA(InputAction.CallbackContext context)
    {
        if (!isSelecting)
            return;

        Select(WhackAMoleManager.HoleType.A);
    }


    private void OnSelectS(InputAction.CallbackContext context)
    {
        if (!isSelecting)
            return;

        Select(WhackAMoleManager.HoleType.S);
    }


    private void OnSelectD(InputAction.CallbackContext context)
    {
        if (!isSelecting)
            return;

        Select(WhackAMoleManager.HoleType.D);
    }

    #endregion


    #region < Button >

    public void OnClickW()
    {
        if (!isSelecting)
            return;

        Select(WhackAMoleManager.HoleType.W);
    }


    public void OnClickA()
    {
        if (!isSelecting)
            return;

        Select(WhackAMoleManager.HoleType.A);
    }


    public void OnClickS()
    {
        if (!isSelecting)
            return;

        Select(WhackAMoleManager.HoleType.S);
    }


    public void OnClickD()
    {
        if (!isSelecting)
            return;

        Select(WhackAMoleManager.HoleType.D);
    }

    #endregion


    #region < Select >

    private void Select(
        WhackAMoleManager.HoleType hole)
    {
        if (!isSelecting)
            return;

        if (whackAMoleManager == null)
            return;

        whackAMoleManager.SelectPlayerHole(hole);

        // 중복 입력 방지
        isSelecting = false;

        inputActions.WhackAMole.Disable();

        // 선택 완료 표시
        if (selectedObject != null)
            selectedObject.SetActive(true);
    }

    #endregion


    public void ShowComplete()
    {
        isSelecting = false;

        inputActions.WhackAMole.Disable();

        if (panel != null)
            panel.SetActive(false);

        if (selectedObject != null)
            selectedObject.SetActive(true);
    }
}