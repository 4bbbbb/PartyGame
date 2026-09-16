using Fusion;
using UnityEngine;

public class BaseballPlayer : NetworkBehaviour
{
    [Header("<< Character >>")]
    [SerializeField] private Renderer characterRenderer;

    [Header("<< Character Database >>")]
    [SerializeField] private CharacterDatabase characterDatabase;

    [Header("<< Animator >>")]
    [SerializeField] private Animator animator;

    [Header("<< Score >>")]
    [SerializeField] private int excellentScore = 3;
    [SerializeField] private int goodScore = 1;

    [Header("<< Timing >>")]
    [SerializeField] private float excellentTiming = 0.1f;
    [SerializeField] private float goodTiming = 0.2f;


    // =========================================================
    // Player Network
    // =========================================================

    private PlayerNetwork playerNetwork;

    public PlayerNetwork PlayerNetwork =>
        playerNetwork;


    // =========================================================
    // Networked
    // =========================================================

    [Networked]
    public int PlayerIndex { get; set; }

    [Networked, OnChangedRender(nameof(OnCharacterIndexChanged))]
    public int CharacterIndex { get; set; } = -1;


    // =========================================================
    // Spawn
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


    public void SetPlayerNetwork(
        PlayerNetwork playerNetwork
    )
    {
        this.playerNetwork = playerNetwork;
    }


    // =========================================================
    // Player Index
    // =========================================================

    public void SetPlayerIndex(int index)
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        PlayerIndex = index;

        Debug.Log(
            $"[BaseballPlayer SetPlayerIndex] " +
            $"Object={Object.Id}, " +
            $"PlayerIndex={PlayerIndex}"
        );
    }


    public void SetCharacterIndex(
        int characterIndex
    )
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        CharacterIndex = characterIndex;

        Debug.Log(
            $"[BaseballPlayer SetCharacterIndex] " +
            $"Object={Object.Id}, " +
            $"CharacterIndex={characterIndex}"
        );
    }


    private void OnCharacterIndexChanged()
    {
        Debug.Log(
            $"[BaseballPlayer OnCharacterIndexChanged] " +
            $"Object={Object.Id}, " +
            $"CharacterIndex={CharacterIndex}"
        );

        ApplyCharacter();
    }


    // =========================================================
    // Input
    // =========================================================

    private void Update()
    {
        if (!Object.HasInputAuthority)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetMouseButtonDown(0))
        {
            // 배트 애니메이션만 실행
            RPC_PlayHit();
        }
    }


    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.All
    )]
    private void RPC_PlayHit()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetTrigger("Hit");
    }


    // =========================================================
    // Character
    // =========================================================

    private void ApplyCharacter()
    {
        Debug.Log(
            $"[ApplyCharacter] " +
            $"PlayerRef = {Object.InputAuthority}, " +
            $"PlayerIndex = {PlayerIndex}, " +
            $"CharacterIndex = {CharacterIndex}"
        );


        int characterIndex =
            CharacterIndex;


        if (characterIndex < 0)
        {
            Debug.LogWarning(
                $"CharacterIndex가 아직 설정되지 않았습니다. " +
                $"PlayerRef = {Object.InputAuthority}"
            );

            return;
        }


        if (characterDatabase == null)
        {
            Debug.LogError(
                "CharacterDatabase가 연결되지 않았습니다."
            );

            return;
        }


        if (characterDatabase.characters == null ||
            characterDatabase.characters.Length == 0)
        {
            Debug.LogError(
                "CharacterDatabase에 캐릭터가 없습니다."
            );

            return;
        }


        if (characterIndex >=
            characterDatabase.characters.Length)
        {
            Debug.LogError(
                $"잘못된 CharacterIndex : {characterIndex}"
            );

            return;
        }


        CharacterData characterData =
            characterDatabase.characters[
                characterIndex
            ];


        if (characterData == null)
        {
            Debug.LogError(
                $"CharacterData가 없습니다. " +
                $"Index = {characterIndex}"
            );

            return;
        }


        if (characterRenderer == null)
        {
            Debug.LogError(
                "Character Renderer가 연결되지 않았습니다."
            );

            return;
        }


        Material[] materials =
            characterRenderer.materials;


        if (materials.Length < 2)
        {
            Debug.LogWarning(
                "Character Renderer의 Material 슬롯이 2개 미만입니다."
            );

            return;
        }


        materials[0] =
            characterData.characterMaterial;

        characterRenderer.materials =
            materials;


        Debug.Log(
            $"게임 캐릭터 설정 완료 : " +
            $"{characterData.characterName}, " +
            $"PlayerRef = {Object.InputAuthority}"
        );
    }


    // =========================================================
    // Hit Request
    // =========================================================

    /// <summary>
    /// BatTrigger에서 호출.
    /// 충돌을 감지한 플레이어가 Host에게
    /// "이 공을 쳤다"라고 요청한다.
    /// </summary>
    public void RequestHit(Ball targetBall)
    {
        if (targetBall == null)
        {
            Debug.LogWarning(
                "[BaseballPlayer] RequestHit 실패 : targetBall == null"
            );

            return;
        }


        if (!Object.HasInputAuthority)
        {
            Debug.LogWarning(
                "[BaseballPlayer] RequestHit 실패 : InputAuthority 아님"
            );

            return;
        }


        if (targetBall.Object == null ||
            !targetBall.Object.IsValid)
        {
            Debug.LogWarning(
                "[BaseballPlayer] RequestHit 실패 : Ball NetworkObject가 유효하지 않음"
            );

            return;
        }


        Debug.Log(
            $"[BaseballPlayer] ★ Hit Request " +
            $"PlayerIndex={PlayerIndex}, " +
            $"Ball={targetBall.Object.Id}"
        );


        RPC_RequestHit(targetBall.Object.Id);
    }


    // =========================================================
    // Hit RPC
    // ========================================================

    [Rpc(
    RpcSources.InputAuthority,
    RpcTargets.StateAuthority
)]
    private void RPC_RequestHit(
    NetworkId ballId,
    RpcInfo info = default
)
    {
        Debug.Log(
            $"[BaseballPlayer] ★★★ RPC_RequestHit 도착 ★★★ " +
            $"PlayerIndex={PlayerIndex}, " +
            $"BallId={ballId}, " +
            $"HasStateAuthority={Object.HasStateAuthority}"
        );


        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning(
                "[BaseballPlayer] RPC_RequestHit가 State Authority가 아닌 곳에서 실행됨"
            );

            return;
        }


        NetworkObject ballObject =
            Runner.FindObject(ballId);


        if (ballObject == null)
        {
            Debug.LogError(
                $"[BaseballPlayer] ❌ Ball을 찾지 못함 " +
                $"BallId={ballId}"
            );

            return;
        }


        Debug.Log(
            $"[BaseballPlayer] ★ Ball 찾기 성공 " +
            $"BallId={ballId}"
        );


        Ball targetBall =
            ballObject.GetComponent<Ball>();


        if (targetBall == null)
        {
            Debug.LogError(
                $"[BaseballPlayer] ❌ Ball 컴포넌트 없음 " +
                $"BallId={ballId}"
            );

            return;
        }


        Debug.Log(
            $"[BaseballPlayer] ★ CheckHitTiming 호출 " +
            $"PlayerIndex={PlayerIndex}"
        );


        CheckHitTiming(targetBall);
    }


    // =========================================================
    // Hit
    // =========================================================

    public void CheckHitTiming(Ball targetBall)
    {
        // -----------------------------------------------------
        // 실제 판정은 State Authority만
        // -----------------------------------------------------

        if (!Object.HasStateAuthority)
        {
            return;
        }


        if (targetBall == null)
        {
            return;
        }


        // -----------------------------------------------------
        // 이미 판정된 공인지 확인
        // -----------------------------------------------------

        if (targetBall.HasScored)
        {
            return;
        }


        // -----------------------------------------------------
        // 현재 날아가는 공인지 확인
        // -----------------------------------------------------

        if (!targetBall.IsFlying)
        {
            return;
        }


        // -----------------------------------------------------
        // 내 공인지 확인
        // -----------------------------------------------------

        if (targetBall.OwnerIndex != PlayerIndex)
        {
            return;
        }


        // -----------------------------------------------------
        // HitPoint 기준 시간 오차
        // -----------------------------------------------------

        float error =
            Mathf.Abs(
                targetBall.GetHitTimeError()
            );


        Debug.Log(
            $"[BaseballPlayer] ★ Hit 판정! " +
            $"PlayerIndex={PlayerIndex}, " +
            $"Error={error:F4}, " +
            $"Excellent={excellentTiming}, " +
            $"Good={goodTiming}"
        );


        // =====================================================
        // Excellent
        // =====================================================

        if (error <= excellentTiming)
        {
            Debug.Log(
                "[BaseballPlayer] ★ EXCELLENT!"
            );


            if (playerNetwork != null)
            {
                playerNetwork.AddBaseballCount(
                    excellentScore
                );

                Debug.Log(
                    $"[BaseballPlayer] " +
                    $"BaseballCount +{excellentScore}"
                );
            }
            else
            {
                Debug.LogError(
                    "[BaseballPlayer] PlayerNetwork가 연결되지 않았습니다."
                );
            }


            targetBall.HasScored = true;

            targetBall.StopFlying();

            targetBall.PlayExcellentMotion();

            return;
        }


        // =====================================================
        // Good
        // =====================================================

        if (error <= goodTiming)
        {
            Debug.Log(
                "[BaseballPlayer] ★ GOOD!"
            );


            if (playerNetwork != null)
            {
                playerNetwork.AddBaseballCount(
                    goodScore
                );

                Debug.Log(
                    $"[BaseballPlayer] " +
                    $"BaseballCount +{goodScore}"
                );
            }
            else
            {
                Debug.LogError(
                    "[BaseballPlayer] PlayerNetwork가 연결되지 않았습니다."
                );
            }


            targetBall.HasScored = true;

            targetBall.StopFlying();

            targetBall.PlayGoodMotion();

            return;
        }


        // =====================================================
        // Miss
        // =====================================================

        Debug.Log(
            "[BaseballPlayer] ★ MISS!"
        );

        // Miss는 아무것도 하지 않는다.
        // 공은 기존 포물선 궤적을 계속 따라간다.
    }
}