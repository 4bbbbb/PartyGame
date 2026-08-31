using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("<< Character Info >>")]
    public int characterID;
    public string characterName;

    [Header("<< Character Prefab >>")]
    public Material characterMaterial;
    public Sprite icon;
}
