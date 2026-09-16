using DG.Tweening;
using UnityEngine;

public class BallHitMotion : MonoBehaviour
{
    [Header("<< Excellent >>")]
    [SerializeField] private float excellentDistance = 12f;
    [SerializeField] private float excellentHeight = 8f;
    [SerializeField] private float excellentDepthDistance = 12f;
    [SerializeField] private float excellentDuration = 1.2f;


    [Header("<< Good >>")]
    [SerializeField] private float goodDistance = 3f;
    [SerializeField] private float goodHeight = 1.2f;
    [SerializeField] private float goodDepthDistance = 5f;
    [SerializeField] private float goodDuration = 0.35f;


    [Header("<< Scale >>")]
    [SerializeField]
    private Vector3 hitScale =
        new Vector3(8f, 8f, 8f);

    [SerializeField]
    private Vector3 endScale =
        new Vector3(4f, 4f, 4f);


    private bool isHit;

    private Ball ball;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        ball =
            GetComponent<Ball>();
    }


    // =========================================================
    // Excellent
    // =========================================================

    public void PlayExcellentMotion()
    {
        if (isHit)
            return;

        isHit = true;


        if (ball == null)
        {
            ball =
                GetComponent<Ball>();
        }


        if (ball == null)
        {
            return;
        }


        PlayMotion(
            distance: excellentDistance,
            height: excellentHeight,
            depthDistance: excellentDepthDistance,
            duration: excellentDuration,
            horizontalDirection: Vector3.left,
            rotation: new Vector3(
                720f,
                0f,
                0f
            )
        );
    }


    // =========================================================
    // Good
    // =========================================================

    public void PlayGoodMotion()
    {
        if (isHit)
            return;

        isHit = true;


        if (ball == null)
        {
            ball =
                GetComponent<Ball>();
        }


        if (ball == null)
        {
            return;
        }


        PlayMotion(
            distance: goodDistance,
            height: goodHeight,
            depthDistance: goodDepthDistance,
            duration: goodDuration,
            horizontalDirection: Vector3.right,
            rotation: new Vector3(
                180f,
                0f,
                0f
            )
        );
    }


    // =========================================================
    // Play Motion
    // =========================================================

    private void PlayMotion(
        float distance,
        float height,
        float depthDistance,
        float duration,
        Vector3 horizontalDirection,
        Vector3 rotation
    )
    {
        transform.DOKill();


        Vector3 startPosition =
            transform.position;


        Vector3 hitDirection =
            ball.HitDirection;


        Vector3 depthDirection =
            hitDirection *
            depthDistance;


        Vector3 targetPosition =
            startPosition
            +
            horizontalDirection *
            distance
            +
            depthDirection;


        targetPosition.y += height;


        // =====================================================
        // Hit Scale
        // =====================================================

        transform.localScale =
            hitScale;


        // =====================================================
        // DOTween
        // =====================================================

        Sequence sequence =
            DOTween.Sequence();


        sequence.Append(
            transform
                .DOMove(
                    targetPosition,
                    duration
                )
                .SetEase(
                    Ease.OutQuad
                )
        );


        sequence.Join(
            transform
                .DOScale(
                    endScale,
                    duration
                )
                .SetEase(
                    Ease.InQuad
                )
        );


        sequence.Join(
            transform
                .DORotate(
                    rotation,
                    duration,
                    RotateMode.FastBeyond360
                )
        );


        sequence.OnComplete(
            DespawnBall
        );
    }


    // =========================================================
    // Despawn
    // =========================================================

    private void DespawnBall()
    {
        transform.DOKill();


        if (ball == null)
        {
            ball =
                GetComponent<Ball>();
        }


        if (ball == null)
            return;


        // =====================================================
        // Despawn¿∫ Host∏∏
        // =====================================================

        if (
            ball.Object != null &&
            ball.Object.IsValid &&
            ball.Object.HasStateAuthority &&
            ball.Runner != null
        )
        {
            ball.Runner.Despawn(
                ball.Object
            );
        }
    }
}