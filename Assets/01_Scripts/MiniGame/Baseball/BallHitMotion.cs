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
    [SerializeField] private Vector3 hitScale = new Vector3(8f, 8f, 8f);
    [SerializeField] private Vector3 endScale = new Vector3(4f, 4f, 4f);

    private bool isHit;

    public void PlayExcellentMotion()
    {
        if (isHit)
            return;

        isHit = true;

        transform.DOKill();

        Ball ball = GetComponent<Ball>();

        if (ball == null)
        {
            Debug.LogWarning("BallHitMotion : Ball 컴포넌트를 찾을 수 없습니다.");
            Destroy(gameObject);
            return;
        }

        Vector3 startPosition = transform.position;

        // 공이 날아왔던 방향의 반대쪽
        Vector3 hitDirection = ball.HitDirection;

        // 화면상 왼쪽 이동
        Vector3 horizontalDirection =
            Vector3.left * excellentDistance;

        // 공이 날아온 반대 방향으로 깊이 이동
        Vector3 depthDirection =
            hitDirection * excellentDepthDistance;

        Vector3 targetPosition =
            startPosition +
            horizontalDirection +
            depthDirection;

        targetPosition.y += excellentHeight;

        transform.localScale = hitScale;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            transform.DOMove(
                targetPosition,
                excellentDuration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Join(
            transform.DOScale(
                endScale,
                excellentDuration
            ).SetEase(Ease.InQuad)
        );

        sequence.Join(
            transform.DORotate(
                new Vector3(720f, 0f, 0f),
                excellentDuration,
                RotateMode.FastBeyond360
            )
        );

        sequence.OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }

    public void PlayGoodMotion()
    {
        if (isHit)
            return;

        isHit = true;

        transform.DOKill();

        Ball ball = GetComponent<Ball>();

        if (ball == null)
        {
            Debug.LogWarning("BallHitMotion : Ball 컴포넌트를 찾을 수 없습니다.");
            Destroy(gameObject);
            return;
        }

        Vector3 startPosition = transform.position;

        // 공이 날아왔던 방향의 반대쪽
        Vector3 hitDirection = ball.HitDirection;

        // 화면상 오른쪽 이동
        Vector3 horizontalDirection =
            Vector3.right * goodDistance;

        // 공이 날아온 반대 방향으로 깊이 이동
        Vector3 depthDirection =
            hitDirection * goodDepthDistance;

        Vector3 targetPosition =
            startPosition +
            horizontalDirection +
            depthDirection;

        targetPosition.y += goodHeight;

        transform.localScale = hitScale;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            transform.DOMove(
                targetPosition,
                goodDuration
            ).SetEase(Ease.OutQuad)
        );

        sequence.Join(
            transform.DOScale(
                endScale,
                goodDuration
            ).SetEase(Ease.InQuad)
        );

        sequence.Join(
            transform.DORotate(
                new Vector3(180f, 0f, 0f),
                goodDuration,
                RotateMode.FastBeyond360
            )
        );

        sequence.OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }
}