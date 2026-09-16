using Fusion;
using UnityEngine;
using System.Collections;

public class Ball : NetworkBehaviour
{
    [Header("<< Ball Scale >>")]
    [SerializeField]
    private Vector3 startScale =
        new Vector3(4.5f, 4.5f, 4.5f);

    [SerializeField]
    private Vector3 hitScale =
        new Vector3(6.8f, 6.8f, 6.8f);


    [Header("<< Ball Rotation >>")]
    [SerializeField] private float minRotationSpeed = 500f;
    [SerializeField] private float maxRotationSpeed = 1000f;


    [Header("<< Parabola >>")]
    [SerializeField, Range(0.1f, 0.9f)]
    private float peakPosition = 0.55f;


    [Header("<< Ground >>")]
    [SerializeField] private float groundY = 0f;


    [Header("<< Despawn >>")]
    [SerializeField] private float despawnDelay = 0.2f;


    // =========================================================
    // Networked
    // =========================================================

    [Networked]
    public int OwnerIndex { get; private set; }


    [Networked]
    public NetworkBool IsFlying { get; private set; }


    [Networked]
    public NetworkBool IsHitMotion { get; private set; }


    [Networked]
    public NetworkBool HasScored { get; set; }


    // ---------------------------------------------------------
    // Hit Result
    //
    // 0 = None
    // 1 = Excellent
    // 2 = Good
    // ---------------------------------------------------------

    [Networked, OnChangedRender(nameof(OnHitResultChanged))]
    public int HitResult { get; private set; }


    // =========================================================
    // Network Trajectory
    // =========================================================

    [Networked]
    private Vector3 NetworkStartPosition { get; set; }


    [Networked]
    private Vector3 NetworkHitPosition { get; set; }


    [Networked]
    private float NetworkDuration { get; set; }


    [Networked]
    private float NetworkHeight { get; set; }


    [Networked]
    private float NetworkElapsedTime { get; set; }


    // =========================================================
    // Local Trajectory
    // =========================================================

    private Vector3 startPosition;
    private Vector3 hitPosition;

    private Vector3 horizontalDirection;

    private float hitDistance;
    private float hitTime;
    private float horizontalSpeed;

    private float a;
    private float b;
    private float c;

    private float groundDistance;

    private bool localDataReady;
    private bool isDespawning;

    private float rotationSpeed;


    // =========================================================
    // Property
    // =========================================================

    public Vector3 HitDirection =>
        horizontalDirection;


    // =========================================================
    // Spawned
    // =========================================================

    public override void Spawned()
    {
        rotationSpeed =
            Random.Range(
                minRotationSpeed,
                maxRotationSpeed
            );


        transform.localScale =
            startScale;


        if (NetworkDuration > 0f)
        {
            SetupLocalTrajectory();
        }
    }


    // =========================================================
    // Initialize
    // =========================================================

    public void Initialize(
        Vector3 start,
        Vector3 hit,
        float duration,
        float height,
        int ownerIndex
    )
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }


        NetworkStartPosition =
            start;

        NetworkHitPosition =
            hit;

        NetworkDuration =
            duration;

        NetworkHeight =
            height;

        OwnerIndex =
            ownerIndex;

        NetworkElapsedTime =
            0f;


        IsFlying =
            true;

        IsHitMotion =
            false;

        HasScored =
            false;

        HitResult =
            0;


        SetupLocalTrajectory();


        transform.localScale =
            startScale;
    }


    // =========================================================
    // Setup Local Trajectory
    // =========================================================

    private void SetupLocalTrajectory()
    {
        startPosition =
            NetworkStartPosition;

        hitPosition =
            NetworkHitPosition;

        hitTime =
            NetworkDuration;


        if (hitTime <= 0f)
        {
            return;
        }


        Vector3 flatDirection =
            new Vector3(
                hitPosition.x - startPosition.x,
                0f,
                hitPosition.z - startPosition.z
            );


        hitDistance =
            flatDirection.magnitude;


        if (hitDistance <= 0.001f)
        {
            return;
        }


        horizontalDirection =
            flatDirection.normalized;


        horizontalSpeed =
            hitDistance / hitTime;


        // -----------------------------------------------------
        // Parabola
        // -----------------------------------------------------

        float peakX =
            hitDistance * peakPosition;


        float peakY =
            Mathf.Max(
                startPosition.y,
                hitPosition.y
            ) + NetworkHeight;


        c =
            startPosition.y;


        float denominator =
            peakX * peakX -
            peakX * hitDistance;


        if (Mathf.Abs(denominator) < 0.0001f)
        {
            a = 0f;

            b =
                (hitPosition.y - startPosition.y)
                / hitDistance;
        }
        else
        {
            a =
                (
                    peakY
                    - startPosition.y
                    - (
                        (hitPosition.y - startPosition.y)
                        / hitDistance
                    ) * peakX
                )
                / denominator;


            b =
                (
                    hitPosition.y
                    - startPosition.y
                    - a * hitDistance * hitDistance
                )
                / hitDistance;
        }


        groundDistance =
            CalculateGroundDistance();


        localDataReady =
            true;


        transform.position =
            startPosition;


        transform.localScale =
            startScale;
    }


    // =========================================================
    // Fixed Update Network
    // =========================================================

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }


        if (!IsFlying)
        {
            return;
        }


        NetworkElapsedTime +=
            Runner.DeltaTime;


        UpdateAuthorityBall();


        // -----------------------------------------------------
        // Ground
        // -----------------------------------------------------

        if (
            NetworkElapsedTime >=
            GetGroundTime()
        )
        {
            NetworkElapsedTime =
                GetGroundTime();


            UpdateAuthorityBall();


            IsFlying =
                false;


            if (!isDespawning)
            {
                StartCoroutine(
                    DespawnAfterDelay()
                );
            }
        }
    }


    // =========================================================
    // Authority Ball
    // =========================================================

    private void UpdateAuthorityBall()
    {
        if (!localDataReady)
        {
            SetupLocalTrajectory();
        }


        if (!localDataReady)
        {
            return;
        }


        UpdateBallPosition(
            NetworkElapsedTime
        );
    }


    // =========================================================
    // Render
    // =========================================================

    public override void Render()
    {
        if (!localDataReady)
        {
            SetupLocalTrajectory();
        }


        if (!localDataReady)
        {
            return;
        }


        // -----------------------------------------------------
        // Hit Motion 중에는 일반 궤적을 그리지 않는다.
        // -----------------------------------------------------

        if (IsHitMotion)
        {
            return;
        }


        // -----------------------------------------------------
        // 중요
        //
        // IsFlying을 여기서 검사하지 않는다.
        //
        // Host가 IsFlying=false를 보냈더라도
        // Client 화면에서는 NetworkElapsedTime을 기준으로
        // 공을 끝까지 보여줘야 한다.
        // -----------------------------------------------------


        if (Object.HasStateAuthority)
        {
            UpdateBallPosition(
                NetworkElapsedTime
            );

            return;
        }


        // -----------------------------------------------------
        // Client interpolation
        // -----------------------------------------------------

        var interpolator =
            new NetworkBehaviourBufferInterpolator(
                this
            );


        float visualElapsedTime =
            interpolator.Float(
                nameof(NetworkElapsedTime)
            );


        UpdateBallPosition(
            visualElapsedTime
        );
    }


    // =========================================================
    // Update Ball Position
    // =========================================================

    private void UpdateBallPosition(
        float elapsedTime
    )
    {
        if (hitTime <= 0f)
        {
            return;
        }


        float groundTime =
            GetGroundTime();


        float time =
            Mathf.Clamp(
                elapsedTime,
                0f,
                groundTime
            );


        // -----------------------------------------------------
        // Horizontal
        // -----------------------------------------------------

        float horizontalDistance =
            horizontalSpeed * time;


        // -----------------------------------------------------
        // Parabola Y
        // -----------------------------------------------------

        float y =
            a * horizontalDistance * horizontalDistance
            + b * horizontalDistance
            + c;


        // -----------------------------------------------------
        // Position
        // -----------------------------------------------------

        Vector3 position =
            startPosition
            + horizontalDirection *
            horizontalDistance;


        position.y =
            y;


        transform.position =
            position;


        // -----------------------------------------------------
        // Scale
        // -----------------------------------------------------

        float scaleProgress =
            Mathf.Clamp01(
                time / hitTime
            );


        transform.localScale =
            Vector3.Lerp(
                startScale,
                hitScale,
                scaleProgress
            );


        // -----------------------------------------------------
        // Rotation
        //
        // Ground에 도착한 이후에는 회전하지 않는다.
        // -----------------------------------------------------

        if (time < groundTime)
        {
            transform.Rotate(
                Vector3.forward,
                rotationSpeed * Runner.DeltaTime,
                Space.Self
            );
        }
    }


    // =========================================================
    // Ground Time
    // =========================================================

    private float GetGroundTime()
    {
        if (horizontalSpeed <= 0.001f)
        {
            return hitTime;
        }


        if (groundDistance <= 0f)
        {
            return hitTime;
        }


        return groundDistance /
               horizontalSpeed;
    }


    // =========================================================
    // Calculate Ground Distance
    // =========================================================

    private float CalculateGroundDistance()
    {
        float targetC =
            c - groundY;


        float discriminant =
            b * b -
            4f * a * targetC;


        if (discriminant < 0f)
        {
            return hitDistance;
        }


        // -----------------------------------------------------
        // a가 거의 0이면 직선
        // -----------------------------------------------------

        if (Mathf.Abs(a) < 0.0001f)
        {
            if (Mathf.Abs(b) < 0.0001f)
            {
                return hitDistance;
            }


            float linearRoot =
                -targetC / b;


            if (linearRoot > hitDistance)
            {
                return linearRoot;
            }


            return hitDistance;
        }


        float sqrt =
            Mathf.Sqrt(
                discriminant
            );


        float root1 =
            (-b + sqrt) /
            (2f * a);


        float root2 =
            (-b - sqrt) /
            (2f * a);


        float validRoot =
            -1f;


        if (root1 > hitDistance)
        {
            validRoot =
                root1;
        }


        if (root2 > hitDistance)
        {
            if (
                validRoot < 0f ||
                root2 < validRoot
            )
            {
                validRoot =
                    root2;
            }
        }


        if (validRoot < 0f)
        {
            return hitDistance;
        }


        return validRoot;
    }


    // =========================================================
    // Hit Time Error
    // =========================================================

    public float GetHitTimeError()
    {
        return NetworkElapsedTime -
               hitTime;
    }


    // =========================================================
    // Stop Flying
    // =========================================================

    public void StopFlying()
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }


        IsFlying =
            false;
    }


    // =========================================================
    // Excellent Motion
    // =========================================================

    public void PlayExcellentMotion()
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }


        IsHitMotion =
            true;


        HitResult =
            1;
    }


    // =========================================================
    // Good Motion
    // =========================================================

    public void PlayGoodMotion()
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }


        IsHitMotion =
            true;


        HitResult =
            2;
    }


    // =========================================================
    // Hit Result Changed
    // =========================================================

    private void OnHitResultChanged()
    {
        Debug.Log(
            $"[Ball] ★ HitResult Changed = {HitResult}, " +
            $"Ball={Object.Id}"
        );


        BallHitMotion hitMotion =
            GetComponent<BallHitMotion>();


        if (hitMotion == null)
        {
            Debug.LogError(
                "[Ball] BallHitMotion이 없습니다."
            );

            return;
        }


        // -----------------------------------------------------
        // Excellent
        // -----------------------------------------------------

        if (HitResult == 1)
        {
            hitMotion.PlayExcellentMotion();

            return;
        }


        // -----------------------------------------------------
        // Good
        // -----------------------------------------------------

        if (HitResult == 2)
        {
            hitMotion.PlayGoodMotion();

            return;
        }
    }


    // =========================================================
    // Despawn
    // =========================================================

    private IEnumerator DespawnAfterDelay()
    {
        isDespawning =
            true;


        yield return new WaitForSeconds(
            despawnDelay
        );


        if (
            Object != null &&
            Object.IsValid &&
            Object.HasStateAuthority &&
            Runner != null
        )
        {
            Runner.Despawn(
                Object
            );
        }
    }
}