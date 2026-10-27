
using System.Collections;
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

    [Header("<< Highlight >>")]
    [SerializeField] private Outline outline;

    [Header("<< Animation >>")]
    [SerializeField] private float animationDuration = 1.5f;

    private const int MIN_VALUE = 5;
    private const int MAX_SCORE = 10;

    private Coroutine scoreRoutine;

    private int currentScore = 0;

    public void SetPlayer(
        PlayerNetwork player,
        CharacterDatabase characterDatabase,
        bool isLocalPlayer)
    {
        nicknameText.text = player.Nickname.ToString();

        if (player.CharacterIndex >= 0 &&
            player.CharacterIndex < characterDatabase.characters.Length)
        {
            CharacterData characterData =
                characterDatabase.characters[player.CharacterIndex];

            fillImage.color = characterData.characterColor;
        }

        if (outline != null)
        {
            outline.enabled = isLocalPlayer;
        }
    }


    public void SetScore(int score)
    {
        if (scoreRoutine != null)
        {
            StopCoroutine(scoreRoutine);
        }

        score = Mathf.Clamp(score, 0, MAX_SCORE);

        // 이전 점수에서 새 누적 점수까지 애니메이션
        scoreRoutine = StartCoroutine(
            AnimateScore(currentScore, score)
        );
    }

    private IEnumerator AnimateScore(int startScore, int targetScore)
    {
        float elapsed = 0f;

        slider.value = startScore + MIN_VALUE;

        scoreText.gameObject.SetActive(targetScore > 0);
        scoreText.text = startScore.ToString();

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / animationDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            float scoreValue = Mathf.Lerp(
                startScore,
                targetScore,
                smoothT
            );

            slider.value = scoreValue + MIN_VALUE;
            scoreText.text = Mathf.RoundToInt(scoreValue).ToString();

            yield return null;
        }

        slider.value = targetScore + MIN_VALUE;
        scoreText.text = targetScore.ToString();

        currentScore = targetScore;
        scoreRoutine = null;
    }

}
