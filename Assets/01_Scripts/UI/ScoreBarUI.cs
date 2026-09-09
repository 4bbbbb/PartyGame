using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreBarUI : MonoBehaviour
{
    [Header("<< Score >>")]
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text scoreText;

    [Header("<< Fill >>")]
    [SerializeField] private Image fillImage;

    private const int MIN_VALUE = 5;
    private const int MAX_SCORE = 10;


    public void SetPlayer(PlayerNetwork player, CharacterDatabase characterDatabase)
    {
        nicknameText.text = player.Nickname.ToString();

        if (player.CharacterIndex >= 0 &&
            player.CharacterIndex < characterDatabase.characters.Length)
        {
            CharacterData characterData = characterDatabase.characters[player.CharacterIndex];

            fillImage.color = characterData.characterColor;
        }
    }


    public void SetScore(int score)
    {
        slider.value = score + MIN_VALUE;

        scoreText.text = score.ToString();

        scoreText.gameObject.SetActive(score > 0);
    }
}