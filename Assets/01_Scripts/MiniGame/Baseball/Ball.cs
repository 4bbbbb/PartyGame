using Fusion;
using UnityEngine;

public class Ball : NetworkBehaviour
{
    [Header("<< Scale >>")]
    [SerializeField]
    private Vector3 startScale =
        new Vector3(4.5f, 4.5f, 4.5f);

    [SerializeField]
    private Vector3 hitScale =
        new Vector3(6.8f, 6.8f, 6.8f);

    [Header("<< Destroy >>")]
    [SerializeField]
    private float destroyY = -2.5f;

    [SerializeField]
    private float despawnDelay = 0.05f;

    #region < Network >

    [Networked]
    private Vector3 StartPosition { get; set; }

    [Networked]
    private Vector3 HitPosition { get; set; }

    [Networked]
    private float Duration { get; set; }

    [Networked]
    private float Height { get; set; }

    [Networked]
    private float RotationSpeed { get; set; }

    [Networked]
    private float ElapsedTime { get; set; }

    [Networked]
    public int ThrowId { get; private set; }

    [Networked]
    public int BallIndex { get; private set; }

    [Networked]
    public NetworkBool IsFlying { get; private set; }
    #endregion


    #region < Trajectory > 

    private Vector3 horizontalDirection;

    private float horizontalDistance;

    private float horizontalSpeed;

    private float parabolaA;
    private float parabolaB;
    private float parabolaC;

    private float visualElapsedTime;

    private bool trajectoryReady;
    private bool isDespawning;

    private BallSpawnManager spawnManager;

    #endregion


    #region < Public > 

    public float HitTime => Duration;

    public float VisualElapsedTime => visualElapsedTime;

    public float VisualHitTimeError => visualElapsedTime - Duration;

    public Vector3 HitDirection => horizontalDirection;

    #endregion


    #region < Spawn >

    public override void Spawned()
    {
        transform.localScale =
            startScale;

        TrySetupTrajectory();
    }

    #endregion


    #region < Initialize >    

    public void Initialize(
        Vector3 startPosition,
        Vector3 hitPosition,
        float duration,
        float height,
        float rotationSpeed,
        int throwId,
        int ballIndex,
        BallSpawnManager manager
    )
    {
        if (!Object.HasStateAuthority)
            return;

        StartPosition = startPosition;

        HitPosition = hitPosition;

        Duration = duration;

        Height = height;

        RotationSpeed = rotationSpeed;

        ThrowId = throwId;

        BallIndex = ballIndex;

        ElapsedTime = 0f;

        IsFlying = true;

        spawnManager = manager;

        TrySetupTrajectory();

        transform.position = StartPosition;

        transform.localScale = startScale;

        transform.rotation = Quaternion.identity;
    }

    #endregion


    #region < Trajectory SetUp >    

    private void TrySetupTrajectory()
    {
        if (Duration <= 0f)
            return;

        Vector3 flatDirection =
            new Vector3(
                HitPosition.x -
                StartPosition.x,

                0f,

                HitPosition.z -
                StartPosition.z
            );

        horizontalDistance = flatDirection.magnitude;

        if (horizontalDistance <= 0.001f)
            return;

        horizontalDirection = flatDirection.normalized;

        horizontalSpeed = horizontalDistance / Duration;

        // 최고점 X 위치
        float peakX = horizontalDistance * 0.55f;

        // 최고점 Y
        float peakY =
            Mathf.Max(
                StartPosition.y,
                HitPosition.y
            ) + Height;

        parabolaC = StartPosition.y;

        float denominator =
            peakX * peakX -
            peakX * horizontalDistance;

        if (Mathf.Abs(denominator) < 0.0001f)
        {
            parabolaA = 0f;

            parabolaB = (HitPosition.y - StartPosition.y) / horizontalDistance;
        }
        else
        {
            parabolaA =
                (
                    peakY -
                    StartPosition.y -
                    (
                        (
                            HitPosition.y -
                            StartPosition.y
                        ) /
                        horizontalDistance
                    ) *
                    peakX
                ) /
                denominator;

            parabolaB =
                (
                    HitPosition.y -
                    StartPosition.y -
                    parabolaA *
                    horizontalDistance *
                    horizontalDistance
                ) /
                horizontalDistance;
        }

        trajectoryReady = true;
    }

    #endregion


    #region < Update Network >    

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (!IsFlying)
            return;

        if (!trajectoryReady)
        {
            TrySetupTrajectory();

            if (!trajectoryReady)
                return;
        }

        ElapsedTime += Runner.DeltaTime;

        UpdateBall(ElapsedTime);

        
        if (transform.position.y < destroyY)
        {
            DespawnBall();
        }
    }

    #endregion


    #region < Render >    

    public override void Render()
    {
        if (!trajectoryReady)
        {
            TrySetupTrajectory();

            if (!trajectoryReady)
                return;
        }

        if (Object.HasStateAuthority)
        {
            visualElapsedTime = ElapsedTime;

            UpdateBall(visualElapsedTime);

            return;
        }

        var interpolator = new NetworkBehaviourBufferInterpolator(this);

        visualElapsedTime = interpolator.Float(nameof(ElapsedTime));

        UpdateBall(visualElapsedTime);
    }

    #endregion


    #region < Ball >    

    private void UpdateBall(float elapsedTime)
    {
        if (!trajectoryReady)
            return;

        float distance = horizontalSpeed * elapsedTime;

        float y =
            parabolaA * distance * distance
            +
            parabolaB * distance
            +
            parabolaC;

        Vector3 position =
            StartPosition +
            horizontalDirection *
            distance;

        position.y = y;

        transform.position = position;

        float scaleProgress = Mathf.Clamp01(elapsedTime / Duration);

        transform.localScale =
            Vector3.Lerp(
                startScale,
                hitScale,
                scaleProgress
            );

        float rotation = RotationSpeed * elapsedTime;

        transform.rotation = Quaternion.Euler(0f, 0f, rotation);
    }

    #endregion


    #region < Despawn >

    public void DespawnForHit()
    {
        if (!Object.HasStateAuthority)
            return;

        if (isDespawning)
            return;

        if (Object == null || !Object.IsValid || Runner == null)
            return;

        isDespawning = true;
        IsFlying = false;

        if (spawnManager != null)
            spawnManager.OnBallDespawnScheduled(this);

        Runner.Despawn(Object);
    }    

    private void DespawnBall()
    {
        if (isDespawning)
            return;

        if (Object == null || !Object.IsValid || Runner == null)        
            return;       

        isDespawning = true;

        IsFlying = false;
        
        if (spawnManager != null)
        {
            spawnManager.OnBallDespawnScheduled(this);
        }

        Invoke(nameof(Despawn), despawnDelay);
    }

    private void Despawn()
    {
        if (!Object.HasStateAuthority)
            return;

        if (Object == null || !Object.IsValid || Runner == null)       
            return;
        

        Runner.Despawn(Object);
    }
}

#endregion