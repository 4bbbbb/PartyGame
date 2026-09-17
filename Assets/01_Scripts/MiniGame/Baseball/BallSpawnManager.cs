using System.Collections;
using TMPro;
using Fusion;
using UnityEngine;

public class BallSpawnManager : NetworkBehaviour
{
    [Header("<< Ball >>")]
    [SerializeField]
    private NetworkPrefabRef ballPrefab;

    [Header("<< Hit Effect >>")]
    [SerializeField]
    private NetworkPrefabRef hitEffectPrefab;

    [Header("<< Points >>")]
    [SerializeField]
    private Transform[] startPoints;

    [SerializeField]
    private Transform[] hitPoints;

    [Header("<< Practice >>")]
    [SerializeField]
    private int practiceThrows = 3;

    [SerializeField]
    private float practiceDuration = 1.8f;

    [SerializeField]
    private float practiceHeight = 4f;

    [SerializeField]
    private float practiceInterval = 1.0f;

    [Header("<< Game >>")]
    [SerializeField]
    private int totalThrows = 30;

    [Header("<< Normal Ball >>")]
    [SerializeField]
    private float normalMinDuration = 1.0f;

    [SerializeField]
    private float normalMaxDuration = 1.8f;

    [SerializeField]
    private float normalMinHeight = 3f;

    [SerializeField]
    private float normalMaxHeight = 5f;

    [SerializeField]
    private float normalMinInterval = 0.8f;

    [SerializeField]
    private float normalMaxInterval = 1.2f;

    [Header("<< Fast Ball >>")]
    [SerializeField]
    private float fastMinDuration = 0.5f;

    [SerializeField]
    private float fastMaxDuration = 1.8f;

    [SerializeField]
    private float fastMinHeight = 3f;

    [SerializeField]
    private float fastMaxHeight = 6f;

    [SerializeField]
    private float fastMinInterval = 0.6f;

    [SerializeField]
    private float fastMaxInterval = 1.0f;

    [Header("<< UI >>")]
    [SerializeField]
    private TextMeshProUGUI ballCountText;

    [Header("<< Practice UI >>")]
    [SerializeField]
    private GameObject practiceHitPointUI;

    private Coroutine practiceCoroutine;
    private Coroutine gameCoroutine;

    private int currentThrow;

    private bool isPracticeRunning;
    private bool isGameRunning;

    private int activeBallCount;

    public bool IsPracticeRunning =>
        isPracticeRunning;

    public bool IsGameRunning =>
        isGameRunning;

    public int CurrentThrow =>
        currentThrow;

    // =========================================================
    // NETWORKED UI
    // =========================================================

    [Networked, OnChangedRender(nameof(OnBallCountUIChanged))]
    private NetworkBool IsBallCountVisible { get; set; }

    [Networked, OnChangedRender(nameof(OnBallCountUIChanged))]
    private int DisplayBallCount { get; set; }

    [Networked, OnChangedRender(nameof(OnPracticeHitPointUIChanged))]
    private NetworkBool IsPracticeHitPointVisible { get; set; }

    // =========================================================
    // SPAWNED
    // =========================================================

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            IsBallCountVisible =
                false;

            DisplayBallCount =
                0;

            IsPracticeHitPointVisible =
                false;
        }

        UpdateBallCountUI();

        UpdatePracticeHitPointUI();
    }

    // =========================================================
    // PRACTICE
    // =========================================================

    public void BeginPractice()
    {
        if (!Object.HasStateAuthority)
            return;

        if (
            isPracticeRunning ||
            isGameRunning
        )
        {
            return;
        }

        practiceCoroutine =
            StartCoroutine(
                PracticeThrowRoutine()
            );
    }

    private IEnumerator PracticeThrowRoutine()
    {
        isPracticeRunning =
            true;

        currentThrow =
            0;

        SetBallCountUI(
            false,
            0
        );

        IsPracticeHitPointVisible =
            true;

        for (
            int i = 0;
            i < practiceThrows;
            i++
        )
        {
            currentThrow++;

            SpawnBalls(
                practiceDuration,
                practiceHeight
            );

            yield return new WaitUntil(
                () =>
                    activeBallCount <= 0
            );

            yield return new WaitForSeconds(
                practiceInterval
            );
        }

        IsPracticeHitPointVisible =
            false;

        isPracticeRunning =
            false;

        practiceCoroutine =
            null;
    }

    // =========================================================
    // GAME
    // =========================================================

    public void BeginGame()
    {
        if (!Object.HasStateAuthority)
            return;

        if (
            isPracticeRunning ||
            isGameRunning
        )
        {
            return;
        }

        gameCoroutine =
            StartCoroutine(
                GameThrowRoutine()
            );
    }

    private IEnumerator GameThrowRoutine()
    {
        isGameRunning =
            true;

        currentThrow =
            0;

        SetBallCountUI(
            true,
            totalThrows
        );

        for (
            int i = 0;
            i < totalThrows;
            i++
        )
        {
            currentThrow++;

            SetBallCountUI(
                true,
                totalThrows - i
            );

            float duration;
            float height;
            float interval;

            // Throw 1 ~ 3
            if (i < 3)
            {
                duration =
                    normalMaxDuration;

                height =
                    normalMinHeight;

                interval =
                    normalMaxInterval;
            }

            // Throw 4 ~ 15
            else if (i < 15)
            {
                duration =
                    Random.Range(
                        normalMinDuration,
                        normalMaxDuration
                    );

                height =
                    Random.Range(
                        normalMinHeight,
                        normalMaxHeight
                    );

                interval =
                    Random.Range(
                        normalMinInterval,
                        normalMaxInterval
                    );
            }

            // Throw 16 ~ 30
            else
            {
                duration =
                    Random.Range(
                        fastMinDuration,
                        fastMaxDuration
                    );

                height =
                    Random.Range(
                        fastMinHeight,
                        fastMaxHeight
                    );

                interval =
                    Random.Range(
                        fastMinInterval,
                        fastMaxInterval
                    );
            }

            SpawnBalls(
                duration,
                height
            );

            yield return new WaitUntil(
                () =>
                    activeBallCount <= 0
            );

            yield return new WaitForSeconds(
                interval
            );
        }

        SetBallCountUI(
            true,
            0
        );

        isGameRunning =
            false;

        gameCoroutine =
            null;
    }

    // =========================================================
    // SPAWN BALLS
    // =========================================================

    private void SpawnBalls(
        float duration,
        float height
    )
    {
        if (!Object.HasStateAuthority)
            return;

        if (!ballPrefab.IsValid)
            return;

        if (
            startPoints == null ||
            startPoints.Length < 4
        )
        {
            Debug.LogError(
                "[BallSpawnManager] " +
                "Start Point가 4개보다 적습니다."
            );

            return;
        }

        if (
            hitPoints == null ||
            hitPoints.Length < 4
        )
        {
            Debug.LogError(
                "[BallSpawnManager] " +
                "Hit Point가 4개보다 적습니다."
            );

            return;
        }

        activeBallCount =
            0;

        // 같은 Throw의 4개 Ball은
        // 같은 회전 속도를 사용
        float rotationSpeed =
            Random.Range(
                500f,
                1000f
            );

        for (
            int i = 0;
            i < 4;
            i++
        )
        {
            NetworkObject ballObject =
                Runner.Spawn(
                    ballPrefab,
                    startPoints[i].position,
                    Quaternion.identity
                );

            if (ballObject == null)
            {
                Debug.LogError(
                    $"[BallSpawnManager] " +
                    $"Ball Spawn 실패. " +
                    $"BallIndex={i}"
                );

                continue;
            }

            Ball ball =
                ballObject.GetComponent<Ball>();

            if (ball == null)
            {
                Debug.LogError(
                    "[BallSpawnManager] " +
                    "Spawn된 Object에 " +
                    "Ball 컴포넌트가 없습니다."
                );

                Runner.Despawn(
                    ballObject
                );

                continue;
            }

            activeBallCount++;

            // i = BallIndex
            //
            // Ball 0 → Player 0
            // Ball 1 → Player 1
            // Ball 2 → Player 2
            // Ball 3 → Player 3
            ball.Initialize(
                startPoints[i].position,
                hitPoints[i].position,
                duration,
                height,
                rotationSpeed,
                currentThrow,
                i,
                this
            );
        }
    }

    // =========================================================
    // HIT EFFECT SPAWN
    // =========================================================

    public void SpawnHitEffect(
    int playerIndex,
    BallHitEffect.HitResult result,
    Vector3 hitDirection
)
    {
        if (!Object.HasStateAuthority)
            return;

        if (!hitEffectPrefab.IsValid)
        {
            Debug.LogError(
                "[BallSpawnManager] " +
                "Hit Effect Prefab이 설정되지 않았습니다."
            );

            return;
        }

        if (
            hitPoints == null ||
            playerIndex < 0 ||
            playerIndex >= hitPoints.Length
        )
        {
            Debug.LogError(
                $"[BallSpawnManager] " +
                $"잘못된 PlayerIndex | " +
                $"PlayerIndex={playerIndex}"
            );

            return;
        }


        // --------------------------------------------------
        // 실제 HitPoint
        // --------------------------------------------------

        Transform hitPoint =
            hitPoints[playerIndex];

        Vector3 spawnPosition = hitPoint.position + Vector3.forward * 0.3f;


        Debug.Log(
            $"[HitEffect] Spawn | " +
            $"PlayerIndex={playerIndex} | " +
            $"HitPoint={spawnPosition} | " +
            $"Result={result}"
        );


        // --------------------------------------------------
        // Spawn
        // --------------------------------------------------

        NetworkObject effectObject =
            Runner.Spawn(
                hitEffectPrefab,
                spawnPosition,
                Quaternion.identity,
                null,
                (runner, obj) =>
                {
                    BallHitEffect effect =
                        obj.GetComponent<BallHitEffect>();

                    if (effect == null)
                    {
                        Debug.LogError(
                            "[BallSpawnManager] " +
                            "BaseballHitEffect가 없습니다."
                        );

                        return;
                    }


                    // ★ Host가 계산한 정확한 HitPoint를 전달
                    effect.Initialize(
                        playerIndex,
                        result,
                        hitDirection,
                        spawnPosition
                    );
                }
            );


        if (effectObject == null)
        {
            Debug.LogError(
                "[BallSpawnManager] " +
                "Hit Effect Spawn 실패."
            );

            return;
        }
    }

    // =========================================================
    // BALL DESPAWN CALLBACK
    // =========================================================

    public void OnBallDespawnScheduled(
        Ball ball
    )
    {
        if (!Object.HasStateAuthority)
            return;

        if (activeBallCount <= 0)
            return;

        activeBallCount--;

        Debug.Log(
            $"[BallSpawnManager] " +
            $"Ball 종료 | " +
            $"ThrowId={ball.ThrowId}, " +
            $"BallIndex={ball.BallIndex}, " +
            $"남은 Ball={activeBallCount}"
        );
    }

    // =========================================================
    // BALL COUNT UI
    // =========================================================

    private void OnBallCountUIChanged()
    {
        UpdateBallCountUI();
    }

    private void UpdateBallCountUI()
    {
        if (ballCountText == null)
            return;

        ballCountText.gameObject.SetActive(
            IsBallCountVisible
        );

        ballCountText.text =
            DisplayBallCount.ToString();
    }

    private void SetBallCountUI(
        bool isVisible,
        int count
    )
    {
        if (!Object.HasStateAuthority)
            return;

        IsBallCountVisible =
            isVisible;

        DisplayBallCount =
            count;

        UpdateBallCountUI();
    }

    // =========================================================
    // PRACTICE HIT POINT UI
    // =========================================================

    private void OnPracticeHitPointUIChanged()
    {
        UpdatePracticeHitPointUI();
    }

    private void UpdatePracticeHitPointUI()
    {
        if (practiceHitPointUI == null)
            return;

        practiceHitPointUI.SetActive(
            IsPracticeHitPointVisible
        );
    }
}