using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("<< Character Info >>")]
    public int characterID;
    public string characterName;
    //public Sprite characterIcon;
    public Color characterColor;

    [Header("<< Character Prefab >>")]
    public Material characterMaterial;   
}
