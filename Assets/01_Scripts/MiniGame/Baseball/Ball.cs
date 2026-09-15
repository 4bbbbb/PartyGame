using Fusion;
using UnityEngine;

public class Ball : NetworkBehaviour
{
    [Header("<< Move >>")]
    [SerializeField] private float moveDuration = 2f;

    [Header("<< Peak >>")]
    [SerializeField, Range(0.5f, 0.6f)]
    private float peakPosition = 0.55f;

    [Header("<< Scale >>")]
    [SerializeField]
    private Vector3 startScale = new Vector3(4.5f, 4.5f, 4.5f);

    [SerializeField]
    private Vector3 hitScale = new Vector3(6.8f, 6.8f, 6.8f);

    [Header("<< Rotation >>")]
    [SerializeField] private float minRotationSpeed = 500f;
    [SerializeField] private float maxRotationSpeed = 1000f;

    [Header("<< Despawn >>")]
    [SerializeField] private float despawnDelay = 0.3f;


    #region < Networked Data >

    // 이 공을 담당하는 플레이어 번호
    [Networked]
    public int OwnerIndex { get; private set; } = -1;

    // 공이 현재 날아가는 중인지
    [Networked]
    public NetworkBool IsFlying { get; private set; }

    // 공이 이미 점수를 지급했는지
    [Networked]
    public NetworkBool HasScored { get; private set; }

    #endregion


    #region < Ball Data >

    private Vector3 startPosition;
    private Vector3 hitPosition;
    private Vector3 horizontalDirection;

    public Vector3 HitDirection => -horizontalDirection;

    // 포물선
    private float a;
    private float b;
    private float c;

    // Start → Hit 수평 거리
    private float hitDistance;

    // Start → Ground 수평 거리
    private float groundDistance;

    // 현재 공 이동 시간
    private float moveTime;

    // Hit까지 걸리는 시간
    private float hitTime;

    // 회전
    private float currentRotationSpeed;
    private Vector3 rotationAxis;

    private bool isDespawning;

    #endregion


    #region < Properties >

    // 현재 공이 HitPoint에 얼마나 가까운지
    public float HitTimeError
    {
        get
        {
            return moveTime - hitTime;
        }
    }

    #endregion


    #region < Network >

    public override void Spawned()
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        IsFlying = false;
        HasScored = false;
    }


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

        MoveBall();
    }

    #endregion


    #region < Initialize >

    public void Initialize(
        Vector3 startPosition,
        Vector3 hitPosition,
        float duration,
        float height,
        int ownerIndex
    )
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        this.startPosition = startPosition;
        this.hitPosition = hitPosition;

        OwnerIndex = ownerIndex;

        hitTime = duration;


        // --------------------------------
        // Start → Hit 수평 방향
        // --------------------------------

        Vector3 horizontal =
            hitPosition - startPosition;

        horizontal.y = 0f;

        hitDistance =
            horizontal.magnitude;

        if (hitDistance <= 0.01f)
        {
            Debug.LogWarning(
                $"Ball의 Start와 Hit 위치가 너무 가깝습니다. " +
                $"OwnerIndex = {ownerIndex}"
            );

            return;
        }

        horizontalDirection =
            horizontal.normalized;


        // --------------------------------
        // 포물선 계산
        // --------------------------------

        CalculateParabola(height);


        // --------------------------------
        // Ground 계산
        // --------------------------------

        CalculateGroundDistance();


        // --------------------------------
        // 초기화
        // --------------------------------

        transform.position =
            startPosition;

        transform.localScale =
            startScale;

        moveTime = 0f;

        HasScored = false;
        IsFlying = true;

        isDespawning = false;


        // --------------------------------
        // 회전
        // --------------------------------

        currentRotationSpeed =
            Random.Range(
                minRotationSpeed,
                maxRotationSpeed
            );

        rotationAxis =
            Random.onUnitSphere;
    }

    #endregion


    #region < Parabola >

    private void CalculateParabola(float height)
    {
        /*
         * y = ax² + bx + c
         *
         * Start
         * x = 0
         * y = startPosition.y
         *
         * Peak
         * x = hitDistance * peakPosition
         * y = height
         *
         * Hit
         * x = hitDistance
         * y = hitPosition.y
         */

        float startY =
            startPosition.y;

        float hitX =
            hitDistance;

        float hitY =
            hitPosition.y;

        float peakX =
            hitDistance * peakPosition;

        float peakY =
            height;

        c = startY;

        float hitDeltaY =
            hitY - startY;

        float peakDeltaY =
            peakY - startY;

        a =
            (hitDeltaY / hitX -
             peakDeltaY / peakX)
            / (hitX - peakX);

        b =
            hitDeltaY / hitX -
            a * hitX;
    }


    private void CalculateGroundDistance()
    {
        /*
         * y = ax² + bx + c
         *
         * 바닥에서는 y = 0
         */

        float discriminant =
            b * b - 4f * a * c;

        if (discriminant < 0f)
        {
            groundDistance =
                hitDistance * 2f;

            return;
        }

        float sqrt =
            Mathf.Sqrt(discriminant);

        float root1 =
            (-b + sqrt) / (2f * a);

        float root2 =
            (-b - sqrt) / (2f * a);

        groundDistance = -1f;

        if (root1 > hitDistance)
        {
            groundDistance = root1;
        }

        if (root2 > hitDistance &&
            (groundDistance < 0f ||
             root2 < groundDistance))
        {
            groundDistance = root2;
        }

        if (groundDistance < 0f)
        {
            groundDistance =
                hitDistance * 2f;
        }
    }

    #endregion


    #region < Move >

    private void MoveBall()
    {
        moveTime += Runner.DeltaTime;

        float horizontalSpeed =
            hitDistance / hitTime;

        float horizontalDistance =
            horizontalSpeed * moveTime;

        float y =
            a * horizontalDistance * horizontalDistance
            + b * horizontalDistance
            + c;

        Vector3 position =
            startPosition
            + horizontalDirection * horizontalDistance;

        position.y = y;

        transform.position =
            position;


        // --------------------------------
        // Ground
        // --------------------------------

        if (position.y <= 0f)
        {
            // 화면에서 보이지 않도록 아래로 이동
            position.y = -1f;

            transform.position =
                position;

            IsFlying = false;

            if (!isDespawning)
            {
                isDespawning = true;

                StartCoroutine(
                    DespawnAfterDelay()
                );
            }

            return;
        }
    }


    private System.Collections.IEnumerator DespawnAfterDelay()
    {
        yield return new WaitForSeconds(
            despawnDelay
        );

        if (Object != null &&
            Object.IsValid)
        {
            Debug.Log(
                $"[Ball] Despawn 실행 - " +
                $"OwnerIndex: {OwnerIndex}, " +
                $"Position: {transform.position}"
            );

            Runner.Despawn(Object);
        }
    }

    #endregion


    #region < Hit >

    public void StopFlying()
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        IsFlying = false;
    }

    #endregion


    #region < Hit Motion >

    public void PlayHitMotion(bool isExcellent)
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        RPC_PlayHitMotion(isExcellent);
    }


    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    private void RPC_PlayHitMotion(
        bool isExcellent
    )
    {
        BallHitMotion ballHitMotion =
            GetComponent<BallHitMotion>();

        if (ballHitMotion == null)
        {
            return;
        }

        if (isExcellent)
        {
            ballHitMotion.PlayExcellentMotion();
        }
        else
        {
            ballHitMotion.PlayGoodMotion();
        }
    }

    #endregion


    #region < Score >

    private PlayerNetwork FindOwnerPlayer()
    {
        PlayerNetwork[] players =
            FindObjectsByType<PlayerNetwork>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        System.Array.Sort(
            players,
            (a, b) =>
                a.PlayerRef.RawEncoded.CompareTo(
                    b.PlayerRef.RawEncoded
                )
        );

        if (OwnerIndex < 0 ||
            OwnerIndex >= players.Length)
        {
            Debug.LogWarning(
                $"공의 OwnerIndex가 잘못되었습니다. " +
                $"OwnerIndex = {OwnerIndex}"
            );

            return null;
        }

        return players[OwnerIndex];
    }


    public void AddBaseballCount()
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        if (HasScored)
        {
            return;
        }

        PlayerNetwork ownerPlayer =
            FindOwnerPlayer();

        if (ownerPlayer == null)
        {
            return;
        }

        ownerPlayer.AddBaseballCount(1);

        HasScored = true;
    }

    #endregion


    #region < Debug >

    public override void Despawned(
        NetworkRunner runner,
        bool hasState
    )
    {
        Debug.Log(
            $"[Ball Despawned] " +
            $"HasState: {hasState}, " +
            $"Position: {transform.position}, " +
            $"OwnerIndex: {OwnerIndex}"
        );
    }

    #endregion
}