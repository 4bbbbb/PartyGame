using Fusion;
using UnityEngine;

public class WhackAMolePlayer : NetworkBehaviour
{
    [Header("<< Character >>")]
    [SerializeField] private Renderer characterRenderer;

    [Header("<< Face >>")]
    [SerializeField] private Material face1Material;
    [SerializeField] private Material face2Material;

    [Header("<< Animation >>")]
    [SerializeField] private Animator animator; 

    [SerializeField] private CharacterDatabase characterDatabase;

    [Networked, OnChangedRender(nameof(OnCharacterIndexChanged))]
    public int CharacterIndex { get; private set; }

      

    public override void Spawned()
    {        
        ApplyCharacter();
        SetFace(face1Material);
    }


    public void SetCharacterIndex(int characterIndex)
    {
        if (!Object.HasStateAuthority)
            return;

        CharacterIndex = characterIndex;

        ApplyCharacter();
    }


    private void OnCharacterIndexChanged()
    {
        ApplyCharacter();
    }


    private void ApplyCharacter()
    {
        Debug.Log(
            $"[ApplyCharacter] " +
            $"PlayerRef = {Object.InputAuthority}, " +
            $"CharacterIndex = {CharacterIndex}"
        );

        if (CharacterIndex < 0)
            return;

        if (characterDatabase == null)
        {
            Debug.LogError("CharacterDatabase가 연결되지 않았습니다.");
            return;
        }

        if (CharacterIndex >= characterDatabase.characters.Length)
        {
            Debug.LogError($"잘못된 CharacterIndex : {CharacterIndex}");
            return;
        }

        CharacterData characterData = characterDatabase.characters[CharacterIndex];

        if (characterData == null)
        {
            Debug.LogError($"CharacterData가 없습니다. Index = {CharacterIndex}");
            return;
        }

        if (characterRenderer == null)
        {
            Debug.LogError("Character Renderer가 연결되지 않았습니다.");
            return;
        }

        Material[] materials = characterRenderer.materials;

        if (materials.Length < 2)
            return;

        materials[0] = characterData.characterMaterial;
        characterRenderer.materials = materials;

        Debug.Log($"게임 캐릭터 설정 : {characterData.characterName}");
    }

    #region < Animation >

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayGreetingAnimation()
    {
        if (animator == null)
            return;        

        animator.SetTrigger("Greeting");       
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayButtonAnimation()
    {
        if (animator == null)
            return;

        animator.SetTrigger("Button");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayHappyAnimation()
    {
        if (animator == null)
            return;

        animator.SetTrigger("Happy");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlaySadAnimation()
    {
        if (animator == null)
            return;

        animator.SetTrigger("Sad");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayHitAnimation()
    {
        SetFace(face2Material);

        if (animator == null)
            return;

        animator.SetTrigger("Hit");
    }

    private void SetFace(Material faceMaterial)
    {
        if (characterRenderer == null)
            return;

        Material[] materials = characterRenderer.materials;

        if (materials.Length < 2)
            return;

        materials[1] = faceMaterial;
        characterRenderer.materials = materials;
    }

    public void ResetFace()
    {
        SetFace(face1Material);
    }

    #endregion
}