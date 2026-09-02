using UnityEngine;

[CreateAssetMenu(fileName = "MiniGameData", menuName = "Game/MiniGame Data")]
public class MiniGameData : ScriptableObject
{
    [Header("<< Game Info >>")]
    [SerializeField] private string gameName;    
    [SerializeField] private string description;
    [SerializeField] private Sprite thumbnail;

    [Header("<< Player >>")]
    [SerializeField] private int minPlayers;
    [SerializeField] private int maxPlayers;


    // 외부에서 읽기 위한 프로퍼티
    public string GameName => gameName;
    public string Description => description;
    public Sprite Thumbnail => thumbnail;

    public int MinPlayers => minPlayers;
    public int MaxPlayers => maxPlayers;
}