using System.Collections;
using TMPro;
using UnityEngine;

public class BallSpawnManager : MonoBehaviour
{
    [Header("<< Ball >>")]
    [SerializeField] private Ball ballPrefab;

    [Header("<< Points >>")]
    [SerializeField] private Transform[] startPoints;
    [SerializeField] private Transform[] hitPoints;

    [Header("<< Round >>")]
    [SerializeField] private int totalThrows = 30;

    [Header("<< UI >>")]
    [SerializeField] private TextMeshProUGUI countText;

    [Header("<< Practice >>")]
    [SerializeField] private int practiceThrows = 3;
    [SerializeField] private float practiceDuration = 1.8f;
    [SerializeField] private float practiceHeight = 4f;
    [SerializeField] private float practiceInterval = 1.5f;

    [Header("<< Normal >>")]
    [SerializeField] private float normalMinDuration = 1.0f;
    [SerializeField] private float normalMaxDuration = 1.8f;
    [SerializeField] private float normalMinHeight = 3f;
    [SerializeField] private float normalMaxHeight = 5f;
    [SerializeField] private float normalMinInterval = 1.5f;
    [SerializeField] private float normalMaxInterval = 2f;

    [Header("<< Fast >>")]
    [SerializeField] private float fastMinDuration = 0.5f;
    [SerializeField] private float fastMaxDuration = 1.8f;
    [SerializeField] private float fastMinHeight = 3f;
    [SerializeField] private float fastMaxHeight = 6f;
    [SerializeField] private float fastMinInterval = 1.5f;
    [SerializeField] private float fastMaxInterval = 1.8f;

    private int currentThrow;

    private void Start()
    {
        StartCoroutine(ThrowRoutine());
    }

    private IEnumerator ThrowRoutine()
    {
        for (currentThrow = 1; currentThrow <= totalThrows; currentThrow++)
        {
            countText.text = $"{totalThrows - currentThrow + 1}";

            // --------------------------------
            // 1~3회 : 연습
            // --------------------------------

            if (currentThrow <= practiceThrows)
            {
                SpawnBalls(
                    practiceDuration,
                    practiceHeight
                );

                yield return new WaitForSeconds(
                    practiceInterval
                );
            }

            // --------------------------------
            // 4~15회 : 일반 난이도
            // --------------------------------

            else if (currentThrow <= 15)
            {
                float duration = Random.Range(
                    normalMinDuration,
                    normalMaxDuration
                );

                float height = Random.Range(
                    normalMinHeight,
                    normalMaxHeight
                );

                float interval = Random.Range(
                    normalMinInterval,
                    normalMaxInterval
                );

                SpawnBalls(duration, height);

                yield return new WaitForSeconds(interval);
            }

            // --------------------------------
            // 16~30회 : 빠른 공 등장
            // --------------------------------

            else
            {
                float duration = Random.Range(
                    fastMinDuration,
                    fastMaxDuration
                );

                float height = Random.Range(
                    fastMinHeight,
                    fastMaxHeight
                );

                float interval = Random.Range(
                    fastMinInterval,
                    fastMaxInterval
                );

                SpawnBalls(duration, height);

                yield return new WaitForSeconds(interval);
            }
        }

        countText.text = "0";

        Debug.Log("===== Baseball 30 Throws Complete =====");
    }


    private void SpawnBalls(float duration, float height)
    {
        if (startPoints.Length < 4 || hitPoints.Length < 4)    
            return;
        

        // 이번 투구에서 사용할 값
        // 4개 공 모두 동일
        for (int i = 0; i < 4; i++)
        {
            Ball ball = Instantiate(ballPrefab);

            ball.Initialize(
                startPoints[i].position,
                hitPoints[i].position,
                duration,
                height
            );
        }

        Debug.Log(
            $"Throw {currentThrow} / {totalThrows} | " +
            $"Duration : {duration:F2} | " +
            $"Height : {height:F2}"
        );
    }
}