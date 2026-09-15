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

    [Header("<< Bat >>")]
    [SerializeField] private Collider batCollider;
    [SerializeField] private BatTrigger batTrigger;

    [Header("<< Score >>")]
    [SerializeField] private int excellentScore = 3;
    [SerializeField] private int goodScore = 1;

    [Header("<< Timing >>")]
    [SerializeField] private float excellentTiming = 0.1f;


    // PlayerNetwork 연결
    private PlayerNetwork playerNetwork;

    public PlayerNetwork PlayerNetwork =>
        playerNetwork;


    [Networked]
    public int PlayerIndex { get; set; }


    [Networked, OnChangedRender(nameof(OnCharacterIndexChanged))]
    public int CharacterIndex { get; set; } = -1;


    #region < Player Network >

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
        this.playerNetwork =
            playerNetwork;
    }

    #endregion


    #region < Player Index >

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

        CharacterIndex =
            characterIndex;

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

    #endregion


    #region < Input >

    private void Update()
    {
        if (!Object.HasInputAuthority)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetMouseButtonDown(0))
        {
            // Space는 배트 애니메이션만 실행
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

    #endregion


    #region < Character >

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

    #endregion


    #region < Hit >

    public void CheckHitTiming(Ball targetBall)
    {
        // 실제 TriggerEnter는
        // State Authority에서만 판정
        if (!Object.HasStateAuthority)
        {
            return;
        }

        if (targetBall == null)
        {
            return;
        }

        // 이미 날아가지 않는 공이면 무시
        if (!targetBall.IsFlying)
        {
            return;
        }

        // 내 공인지 확인
        if (targetBall.OwnerIndex != PlayerIndex)
        {
            return;
        }


        // 현재 실제 TriggerEnter가 발생한 순간의
        // HitPoint까지 남은 시간 오차
        float error =
            Mathf.Abs(
                targetBall.HitTimeError
            );


        Debug.Log(
            $"[BaseballPlayer] ★ Hit 판정! " +
            $"PlayerIndex={PlayerIndex}, " +
            $"Error={error:F4}, " +
            $"ExcellentTiming={excellentTiming}"
        );


        // --------------------------------
        // Excellent
        // --------------------------------

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
            }

            targetBall.StopFlying();

            targetBall.PlayHitMotion(
                true
            );

            return;
        }


        // --------------------------------
        // Good
        // --------------------------------

        Debug.Log(
            "[BaseballPlayer] ★ GOOD!"
        );

        if (playerNetwork != null)
        {
            playerNetwork.AddBaseballCount(
                goodScore
            );
        }

        targetBall.StopFlying();

        targetBall.PlayHitMotion(
            false
        );
    }

    #endregion
}