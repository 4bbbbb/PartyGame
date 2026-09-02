using Fusion;
using UnityEngine;

public class WhackAMolePlayer : NetworkBehaviour
{
    [Header("<< Character >>")]
    [SerializeField] private Renderer characterRenderer;

    [SerializeField] private CharacterDatabase characterDatabase;

    [Networked, OnChangedRender(nameof(OnCharacterIndexChanged))]
    public int CharacterIndex { get; private set; }


    public override void Spawned()
    {
        Debug.Log(
        $"[WhackAMolePlayer Spawned] " +
        $"PlayerRef = {Object.InputAuthority}, " +
        $"StateAuthority = {Object.HasStateAuthority}, " +
        $"Position = {transform.position}, " +
        $"Rotation = {transform.rotation.eulerAngles}, " +
        $"Scale = {transform.localScale}"
    );


        ApplyCharacter();
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
            Debug.LogError(
                "CharacterDatabase가 연결되지 않았습니다."
            );

            return;
        }

        if (CharacterIndex >= characterDatabase.characters.Length)
        {
            Debug.LogError(
                $"잘못된 CharacterIndex : {CharacterIndex}"
            );

            return;
        }

        CharacterData characterData =
            characterDatabase.characters[CharacterIndex];

        if (characterData == null)
        {
            Debug.LogError(
                $"CharacterData가 없습니다. " +
                $"Index = {CharacterIndex}"
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

        characterRenderer.material =
            characterData.characterMaterial;

        Debug.Log(
            $"게임 캐릭터 설정 : " +
            $"{characterData.characterName}"
        );
    }
}