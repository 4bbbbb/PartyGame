using UnityEngine;

public class AlienPlayer : MonoBehaviour
{
    [Header("<< Move >>")]
    [SerializeField] private float moveSpeed = 5f;

    private Player_InputActions inputActions;
    private Vector2 moveInput;
    private bool throwObstacleInput;

    #region < Unity >

    private void Awake()
    {
        inputActions = new Player_InputActions();
    }

    private void OnEnable()
    {
        if (inputActions == null)
            return;

        inputActions.DodgeIt_Alien.Enable();
        inputActions.DodgeIt_Alien.ThrowObstacle.performed += OnThrowObstacle;
    }

    private void OnDisable()
    {
        if (inputActions == null)
            return;

        inputActions.DodgeIt_Alien.ThrowObstacle.performed -= OnThrowObstacle;
        inputActions.DodgeIt_Alien.Disable();
    }

    private void OnDestroy()
    {
        inputActions?.Dispose();
    }

    #endregion


    #region < Input >

    private void Update()
    {
        if (inputActions == null)
            return;

        moveInput = inputActions.DodgeIt_Alien.Move.ReadValue<Vector2>();

        Move();
    }

    public Vector2 GetMoveInput()
    {
        return moveInput;
    }

    public bool GetThrowObstacleInput()
    {
        bool value = throwObstacleInput;
        throwObstacleInput = false;

        return value;
    }

    private void OnThrowObstacle(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        throwObstacleInput = true;
    }

    #endregion


    #region < Move >

    private void Move()
    {
        Vector3 movement = Vector3.right * GetMoveInput().x;

        Vector3 nextPosition = transform.position + movement * moveSpeed * Time.deltaTime;

        nextPosition.x = Mathf.Clamp(nextPosition.x, -7f, 7f);

        transform.position = nextPosition;
    }

    #endregion
}
