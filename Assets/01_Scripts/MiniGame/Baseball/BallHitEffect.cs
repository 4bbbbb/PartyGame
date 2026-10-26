using DG.Tweening;
using Fusion;
using UnityEngine;

public class BallHitEffect : NetworkBehaviour
{
    public enum HitResult
    {
        Excellent,
        Good
    }

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

    // --------------------------------------------------
    // Network
    // --------------------------------------------------

    [Networked]
    public int PlayerIndex { get; private set; }

    [Networked]
    public HitResult Result { get; private set; }

    [Networked]
    private Vector3 HitDirection { get; set; }

    // ★ Host가 결정한 실제 시작 위치
    [Networked]
    private Vector3 SpawnPosition { get; set; }


    // --------------------------------------------------
    // Initialize
    // --------------------------------------------------

    public void Initialize(
        int playerIndex,
        HitResult result,
        Vector3 hitDirection,
        Vector3 spawnPosition)
    {
        if (!Object.HasStateAuthority)
            return;

        PlayerIndex = playerIndex;
        Result = result;
        HitDirection = hitDirection;

        // ★ 실제 HitPoint 위치를 Networked로 저장
        SpawnPosition = spawnPosition;
    }


    // --------------------------------------------------
    // Spawned
    // --------------------------------------------------

    public override void Spawned()
    {
        // ★ 모든 Peer가 Host가 결정한 위치를 사용
        transform.position = SpawnPosition;

        // 시작 회전도 통일
        transform.rotation = Quaternion.identity;

        PlayMotion();
    }


    // --------------------------------------------------
    // Hit Motion
    // --------------------------------------------------


    private void PlayMotion()
    {
        float distance;
        float height;
        float duration;
        float depthDistance;
        Vector3 horizontalDirection;
        Vector3 rotation;

        if (Result == HitResult.Excellent)
        {
            // 홈런: 왼쪽으로 높고 멀리
            distance = excellentDistance;
            height = excellentHeight;
            duration = excellentDuration;
            depthDistance = excellentDepthDistance;

            horizontalDirection = Vector3.left;
            rotation = new Vector3(720f, 0f, 0f);
        }
        else
        {
            // 안타: 오른쪽으로 낮고 빠르게
            distance = goodDistance;
            height = goodHeight;
            duration = goodDuration;
            depthDistance = goodDepthDistance;

            horizontalDirection = Vector3.right;
            rotation = new Vector3(180f, 0f, 0f);
        }

        Vector3 startPosition = SpawnPosition;

        // 깊이 이동은 공이 날아오던 방향과 분리
        // 카메라의 앞뒤 방향을 기준으로 설정
        Vector3 depthDirection = Vector3.forward;

        Vector3 targetPosition =
            startPosition
            + horizontalDirection * distance
            + depthDirection * depthDistance;

        targetPosition.y += height;

        transform.position = startPosition;
        transform.localScale = hitScale;
        transform.rotation = Quaternion.identity;

        transform.DOKill();

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            transform.DOMove(targetPosition, duration)
                .SetEase(Ease.OutQuad)
        );

        sequence.Join(
            transform.DOScale(endScale, duration)
                .SetEase(Ease.InQuad)
        );

        sequence.Join(
            transform.DORotate(rotation, duration, RotateMode.FastBeyond360)
        );

        sequence.OnComplete(DespawnEffect);
    }



    // --------------------------------------------------
    // Despawn
    // --------------------------------------------------

    private void DespawnEffect()
    {
        transform.DOKill();

        if (!Object.HasStateAuthority)
            return;

        if (
            Object == null ||
            !Object.IsValid ||
            Runner == null
        )
        {
            return;
        }

        Runner.Despawn(Object);
    }


    // --------------------------------------------------
    // Despawned
    // --------------------------------------------------

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        transform.DOKill();
    }
}