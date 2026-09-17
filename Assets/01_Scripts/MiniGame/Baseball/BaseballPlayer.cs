using Fusion;
using UnityEngine;

public class BaseballPlayer : NetworkBehaviour
{
    [Header("<< Character >>")]
    [SerializeField]
    private Renderer characterRenderer;

    [Header("<< Character Database >>")]
    [SerializeField]
    private CharacterDatabase characterDatabase;

    [Header("<< Animator >>")]
    [SerializeField]
    private Animator animator;

    [Header("<< Score >>")]
    [SerializeField]
    private int excellentScore = 3;

    [SerializeField]
    private int goodScore = 1;

    [Header("<< Timing >>")]
    [SerializeField]
    private float excellentTiming = 0.06f;

    [SerializeField]
    private float goodTiming = 0.2f;

    private PlayerNetwork playerNetwork;

    public PlayerNetwork PlayerNetwork =>
        playerNetwork;

    // =========================================================
    // NETWORKED
    // =========================================================

    [Networked]
    public int PlayerIndex { get; set; }

    [Networked, OnChangedRender(nameof(OnCharacterIndexChanged))]
    public int CharacterIndex { get; set; } = -1;

    private int lastHitThrowId = -1;

    // =========================================================
    // SPAWNED
    // =========================================================

    public override void Spawned()
    {
        Debug.Log(
            $"[BaseballPlayer Spawned] " +
            $"Object={Object.Id}, " +
            $"InputAuthority={Object.InputAuthority}, " +
            $"HasInputAuthority={Object.HasInputAuthority}, " +
            $"PlayerIndex={PlayerIndex}, " +
            $"CharacterIndex={CharacterIndex}"
        );

        ApplyCharacter();
    }

    // =========================================================
    // PLAYER NETWORK
    // =========================================================

    public void SetPlayerNetwork(
        PlayerNetwork playerNetwork
    )
    {
        this.playerNetwork =
            playerNetwork;
    }

    // =========================================================
    // PLAYER INDEX
    // =========================================================

    public void SetPlayerIndex(
        int index
    )
    {
        if (!Object.HasStateAuthority)
            return;

        PlayerIndex =
            index;

        Debug.Log(
            $"[BaseballPlayer SetPlayerIndex] " +
            $"Object={Object.Id}, " +
            $"PlayerIndex={PlayerIndex}"
        );
    }

    // =========================================================
    // CHARACTER INDEX
    // =========================================================

    public void SetCharacterIndex(
        int characterIndex
    )
    {
        if (!Object.HasStateAuthority)
            return;

        CharacterIndex =
            characterIndex;

        Debug.Log(
            $"[BaseballPlayer SetCharacterIndex] " +
            $"Object={Object.Id}, " +
            $"CharacterIndex={characterIndex}"
        );
    }

    // =========================================================
    // INPUT
    // =========================================================

    private void Update()
    {
        if (!Object.HasInputAuthority)
            return;

        if (
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetMouseButtonDown(0)
        )
        {
            RPC_PlayHit();
        }
    }

    // =========================================================
    // ANIMATION EVENT
    // =========================================================

    public void OnBatHitPoint()
    {
        if (!Object.HasInputAuthority)
            return;

        Ball targetBall = FindBestBall();

        if (targetBall == null)
        {
            Debug.Log(
                $"[BaseballPlayer] " +
                $"BAT HIT POINT - Ball 없음 | " +
                $"PlayerIndex={PlayerIndex}"
            );

            return;
        }

        float visualHitTimeError =
            targetBall.VisualHitTimeError;

        NetworkId ballId =
            targetBall.Object;

        Debug.Log(
            $"[BaseballPlayer] BAT HIT POINT | " +
            $"PlayerIndex={PlayerIndex}, " +
            $"BallIndex={targetBall.BallIndex}, " +
            $"ThrowId={targetBall.ThrowId}, " +
            $"Error={visualHitTimeError:F4}"
        );

        RPC_RequestHit(
            ballId,
            targetBall.ThrowId,
            targetBall.BallIndex,
            visualHitTimeError
        );
    }

    // =========================================================
    // FIND MY BALL
    // =========================================================

    private Ball FindBestBall()
    {
        Ball[] balls =
            FindObjectsByType<Ball>(
                FindObjectsSortMode.None
            );

        Ball bestBall =
            null;

        float bestError =
            float.MaxValue;

        foreach (
            Ball ball in balls
        )
        {
            if (ball == null)
                continue;

            if (
                ball.Object == null ||
                !ball.Object.IsValid
            )
            {
                continue;
            }

            if (!ball.IsFlying)
                continue;

            // ⭐ 핵심
            //
            // 내 PlayerIndex와 같은 BallIndex만 사용
            if (
                ball.BallIndex !=
                PlayerIndex
            )
            {
                continue;
            }

            float error =
                Mathf.Abs(
                    ball.VisualHitTimeError
                );

            if (
                error <
                bestError
            )
            {
                bestError =
                    error;

                bestBall =
                    ball;
            }
        }

        return bestBall;
    }

    // =========================================================
    // HIT ANIMATION
    // =========================================================

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.All
    )]
    private void RPC_PlayHit()
    {
        if (animator == null)
            return;

        animator.SetTrigger(
            "Hit"
        );
    }

    // =========================================================
    // HIT REQUEST
    // =========================================================

    [Rpc(
    RpcSources.InputAuthority,
    RpcTargets.StateAuthority
)]
    private void RPC_RequestHit(
    NetworkId ballId,
    int throwId,
    int ballIndex,
    float visualHitTimeError
)
    {
        if (!Object.HasStateAuthority)
            return;

        Debug.Log(
            $"[BaseballPlayer] HIT RPC | " +
            $"PlayerIndex={PlayerIndex}, " +
            $"BallIndex={ballIndex}, " +
            $"ThrowId={throwId}, " +
            $"Error={visualHitTimeError:F4}"
        );

        // =========================================================
        // Player가 보낸 Ball Reference 확인
        // =========================================================

        if (!Runner.TryFindObject(
        ballId,
            out NetworkObject ballObject
        ))
        {
            Debug.LogWarning(
                $"[BaseballPlayer] " +
                $"RPC로 받은 Ball을 찾을 수 없음 | " +
                $"PlayerIndex={PlayerIndex}, " +
                $"BallIndex={ballIndex}, " +
                $"ThrowId={throwId}"
            );

            return;
        }

        Ball ball =
            ballObject.GetComponent<Ball>();

        if (ball == null)
        {
            Debug.LogWarning(
                "[BaseballPlayer] " +
                "NetworkObject에 Ball 컴포넌트가 없습니다."
            );

            return;
        }

        // =========================================================
        // 안전 검증
        // =========================================================

        if (!ball.IsFlying)
        {
            Debug.Log(
                $"[BaseballPlayer] " +
                $"이미 종료된 Ball | " +
                $"BallIndex={ball.BallIndex}, " +
                $"ThrowId={ball.ThrowId}"
            );

            return;
        }

        if (ball.ThrowId != throwId)
        {
            Debug.LogWarning(
                $"[BaseballPlayer] " +
                $"ThrowId 불일치 | " +
                $"RPC={throwId}, " +
                $"Ball={ball.ThrowId}"
            );

            return;
        }

        if (ball.BallIndex != ballIndex)
        {
            Debug.LogWarning(
                $"[BaseballPlayer] " +
                $"BallIndex 불일치 | " +
                $"RPC={ballIndex}, " +
                $"Ball={ball.BallIndex}"
            );

            return;
        }

        if (ball.BallIndex != PlayerIndex)
        {
            Debug.LogWarning(
                $"[BaseballPlayer] " +
                $"PlayerIndex / BallIndex 불일치 | " +
                $"PlayerIndex={PlayerIndex}, " +
                $"BallIndex={ball.BallIndex}"
            );

            return;
        }

        // =========================================================
        // 이미 이 Throw에서 성공했는지 확인
        // =========================================================

        if (lastHitThrowId == throwId)
        {
            Debug.Log(
                $"[BaseballPlayer] " +
                $"이미 성공한 Throw | " +
                $"ThrowId={throwId}, " +
                $"PlayerIndex={PlayerIndex}"
            );

            return;
        }

        // =========================================================
        // 판정
        // =========================================================

        CheckHitTiming(
            ball,
            visualHitTimeError
        );
    }
    // =========================================================
    // HIT CHECK
    // =========================================================

    private void CheckHitTiming(
      Ball targetBall,
      float visualHitTimeError
  )
    {
        if (!Object.HasStateAuthority)
            return;

        float error =
            Mathf.Abs(
                visualHitTimeError
            );

        Debug.Log(
            $"[BaseballPlayer] Hit 판정 | " +
            $"PlayerIndex={PlayerIndex}, " +
            $"BallIndex={targetBall.BallIndex}, " +
            $"ThrowId={targetBall.ThrowId}, " +
            $"Error={error:F4}"
        );

        // =========================================================
        // EXCELLENT
        // =========================================================

        if (error <= excellentTiming)
        {
            Debug.Log(
                $"[BaseballPlayer] " +
                $"PLAYER {PlayerIndex} EXCELLENT!"
            );

            AddScore(
                excellentScore
            );

            lastHitThrowId =
                targetBall.ThrowId;

            DespawnPlayerBall(
                targetBall,
                BallHitEffect.HitResult.Excellent
            );

            return;
        }

        // =========================================================
        // GOOD
        // =========================================================

        if (error <= goodTiming)
        {
            Debug.Log(
                $"[BaseballPlayer] " +
                $"PLAYER {PlayerIndex} GOOD!"
            );

            AddScore(
                goodScore
            );

            lastHitThrowId =
                targetBall.ThrowId;

            DespawnPlayerBall(
                targetBall,
                BallHitEffect.HitResult.Good
            );

            return;
        }

        // =========================================================
        // MISS
        // =========================================================

        Debug.Log(
            $"[BaseballPlayer] " +
            $"PLAYER {PlayerIndex} MISS!"
        );

        // MISS는 Ball을 제거하지 않는다.
    }

    // =========================================================
    // DESPAWN MY BALL + SPAWN EFFECT
    // =========================================================

    private void DespawnPlayerBall(
    Ball targetBall,
    BallHitEffect.HitResult result
)
    {
        if (!Object.HasStateAuthority)
            return;

        if (targetBall == null)
        {
            Debug.LogWarning(
                "[BaseballPlayer] " +
                "Despawn할 Ball이 null입니다."
            );

            return;
        }

        if (
            targetBall.Object == null ||
            !targetBall.Object.IsValid
        )
        {
            Debug.LogWarning(
                "[BaseballPlayer] " +
                "Ball NetworkObject가 유효하지 않습니다."
            );

            return;
        }

        if (!targetBall.IsFlying)
        {
            Debug.Log(
                "[BaseballPlayer] " +
                "Ball이 이미 종료되었습니다."
            );

            return;
        }

        // =========================================================
        // 최종 확인
        // =========================================================

        if (
            targetBall.BallIndex !=
            PlayerIndex
        )
        {
            Debug.LogWarning(
                $"[BaseballPlayer] " +
                $"잘못된 Ball! " +
                $"PlayerIndex={PlayerIndex}, " +
                $"BallIndex={targetBall.BallIndex}"
            );

            return;
        }

        Vector3 hitDirection =
            targetBall.HitDirection;

        int playerIndex =
            PlayerIndex;

        int throwId =
            targetBall.ThrowId;

        int ballIndex =
            targetBall.BallIndex;

        Debug.Log(
            $"[BaseballPlayer] " +
            $"정확한 Ball 처리 | " +
            $"PlayerIndex={playerIndex}, " +
            $"BallIndex={ballIndex}, " +
            $"ThrowId={throwId}, " +
            $"Result={result}"
        );

        // =========================================================
        // 1. 실제 Ball 제거
        // =========================================================

        targetBall.DespawnForHit();

        // =========================================================
        // 2. Hit Effect 생성
        // =========================================================

        BallSpawnManager spawnManager =
            FindFirstObjectByType<
                BallSpawnManager
            >();

        if (spawnManager == null)
        {
            Debug.LogError(
                "[BaseballPlayer] " +
                "BallSpawnManager를 찾지 못했습니다."
            );

            return;
        }

        spawnManager.SpawnHitEffect(
            playerIndex,
            result,
            hitDirection
        );
    }

    // =========================================================
    // SCORE
    // =========================================================

    private void AddScore(
        int score
    )
    {
        if (playerNetwork == null)
        {
            Debug.LogError(
                "[BaseballPlayer] " +
                "PlayerNetwork가 연결되지 않았습니다."
            );

            return;
        }

        playerNetwork.AddBaseballCount(
            score
        );

        Debug.Log(
            $"[BaseballPlayer] " +
            $"PlayerIndex={PlayerIndex}, " +
            $"BaseballCount +{score}"
        );
    }

    // =========================================================
    // CHARACTER
    // =========================================================

    private void OnCharacterIndexChanged()
    {
        ApplyCharacter();
    }

    private void ApplyCharacter()
    {
        if (characterDatabase == null)
        {
            Debug.LogError(
                "[BaseballPlayer] " +
                "CharacterDatabase가 없습니다."
            );

            return;
        }

        if (
            CharacterIndex < 0 ||
            CharacterIndex >=
            characterDatabase.characters.Length
        )
        {
            return;
        }

        CharacterData characterData =
            characterDatabase.characters[
                CharacterIndex
            ];

        if (characterData == null)
            return;

        if (characterRenderer == null)
            return;

        Material[] materials =
            characterRenderer.materials;

        if (materials.Length < 2)
            return;

        materials[0] =
            characterData.characterMaterial;

        characterRenderer.materials =
            materials;
    }
}