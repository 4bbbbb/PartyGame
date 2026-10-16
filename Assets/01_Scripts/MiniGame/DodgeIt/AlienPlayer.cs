
using System.Collections;
using UnityEngine;

public class AlienPlayer : MonoBehaviour
{
    [Header("<< Move >>")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("<< Obstacle Spawn >>")]
    [SerializeField] private Transform obstacleSpawnPoint;
    [SerializeField] private GameObject obstacle3Prefab;
    [SerializeField] private GameObject obstacle4Prefab;
    [SerializeField] private GameObject obstacle5Prefab;

    [SerializeField] private float firstSpawnDelay = 1f;
    [SerializeField] private float nextSpawnDelay = 2f;

    private Player_InputActions inputActions;
    private Vector2 moveInput;
    private bool dropObstacleInput;

    private DodgeItObstacle currentObstacle;
    private bool waitingForNextObstacle;

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
        inputActions.DodgeIt_Alien.DropObstacle.performed += OnDropObstacle;
        StartCoroutine(SpawnObstacleLoop());
    }

    private void OnDisable()
    {
        if (inputActions == null)
            return;

        inputActions.DodgeIt_Alien.DropObstacle.performed -= OnDropObstacle;
        inputActions.DodgeIt_Alien.Disable();

        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        inputActions?.Dispose();
    }

    private void Update()
    {
        if (inputActions == null)
            return;

        moveInput = inputActions.DodgeIt_Alien.Move.ReadValue<Vector2>();

        Move();

        if (GetDropObstacleInput())
            DropObstacle();
    }

    #endregion


    #region < Input >

    public Vector2 GetMoveInput()
    {
        return moveInput;
    }

    public bool GetDropObstacleInput()
    {
        bool value = dropObstacleInput;
        dropObstacleInput = false;
        return value;
    }

    private void OnDropObstacle(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        dropObstacleInput = true;
    }

    #endregion


    #region < Move >

    private void Move()
    {
        Vector3 movement = Vector3.right * GetMoveInput().x;

        Vector3 nextPosition =
            transform.position + movement * moveSpeed * Time.deltaTime;

        nextPosition.x = Mathf.Clamp(nextPosition.x, -7f, 7f);

        transform.position = nextPosition;
    }

    #endregion


    #region < Obstacle >

    private IEnumerator SpawnObstacleLoop()
    {
        yield return new WaitForSeconds(firstSpawnDelay);

        while (true)
        {
            SpawnObstacle();

            while (currentObstacle != null)
            {
                yield return null;
            }

            while (!waitingForNextObstacle)
            {
                yield return null;
            }

            yield return new WaitForSeconds(nextSpawnDelay);

            waitingForNextObstacle = false;
        }
    }

    private void SpawnObstacle()
    {
        if (obstacleSpawnPoint == null)
        {
            Debug.LogError("Obstacle Spawn Point가 할당되지 않았어!");
            return;
        }

        int randomValue = Random.Range(0, 100);

        GameObject selectedPrefab;

        if (randomValue < 40)
            selectedPrefab = obstacle3Prefab;
        else if (randomValue < 80)
            selectedPrefab = obstacle4Prefab;
        else
            selectedPrefab = obstacle5Prefab;

        if (selectedPrefab == null)       
            return;
        

        GameObject obstacleObject = Instantiate(
            selectedPrefab,
            obstacleSpawnPoint.position,
            obstacleSpawnPoint.rotation
        );

        obstacleObject.transform.SetParent(obstacleSpawnPoint);

        currentObstacle = obstacleObject.GetComponent<DodgeItObstacle>();

        if (currentObstacle == null)
        {
            Debug.LogError("장애물 프리팹에 DodgeItObstacle이 없어!");
            Destroy(obstacleObject);
            return;
        }

        waitingForNextObstacle = false;
    }

    private void DropObstacle()
    {
        if (currentObstacle == null || waitingForNextObstacle)
            return;

        // SpawnPoint에서 분리해 현재 위치에 고정
        currentObstacle.transform.SetParent(null);

        // 중력 활성화
        currentObstacle.Drop();

        currentObstacle = null;
        waitingForNextObstacle = true;
    }

    #endregion
}
