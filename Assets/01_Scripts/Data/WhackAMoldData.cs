using UnityEngine;

[CreateAssetMenu(fileName = "WhackAMoleData", menuName = "Game/Whack A Mole Data")]
public class WhackAMoleData : MiniGameData
{
    [Header("<< Whack A Mole >>")]
    [SerializeField] private int tagHP = 3;
    [SerializeField] private int roundCount = 3;
    [SerializeField] private int holeCount = 4;

    [Header("<< Score >>")]
    [SerializeField] private int firstPlaceScore = 3;
    [SerializeField] private int secondPlaceScore = 2;
    [SerializeField] private int thirdPlaceScore = 1;


    public int TagHP => tagHP;
    public int RoundCount => roundCount;
    public int HoleCount => holeCount;

    public int FirstPlaceScore => firstPlaceScore;
    public int SecondPlaceScore => secondPlaceScore;
    public int ThirdPlaceScore => thirdPlaceScore;
}