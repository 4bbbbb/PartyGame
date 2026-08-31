using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Game/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    public CharacterData[] characters;
}