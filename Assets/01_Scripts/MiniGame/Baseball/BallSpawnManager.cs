using System.Collections;
using TMPro;
using Fusion;
using UnityEngine;

public class BallSpawnManager : NetworkBehaviour
{
    [Header("<< Ball >>")]
    [SerializeField] private NetworkPrefabRef ballPrefab;

    [Header("<< Points >>")]
    [SerializeField] private Transform[] startPoints;
    [SerializeField] private Transform[] hitPoints;

    [Header("<< Practice >>")]
    [SerializeField] private int practiceThrows = 3;
    [SerializeField] private float practiceDuration = 1.8f;
    [SerializeField] private float practiceHeight = 4f;
    [SerializeField] private float practiceInterval = 1.5f;

    [Header("<< Game >>")]
    [SerializeField] private int totalThrows = 30;

    [Header("<< Normal Ball >>")]
    [SerializeField] private float normalMinDuration = 1.0f;
    [SerializeField] private float normalMaxDuration = 1.8f;
    [SerializeField] private float normalMinHeight = 3f;
    [SerializeField] private float normalMaxHeight = 5f;
    [SerializeField] private float normalMinInterval = 1.5f;
    [SerializeField] private float normalMaxInterval = 2.0f;

    [Header("<< Fast Ball >>")]
    [SerializeField] private float fastMinDuration = 0.5f;
    [SerializeField] private float fastMaxDuration = 1.8f;
    [SerializeField] private float fastMinHeight = 3f;
    [SerializeField] private float fastMaxHeight = 6f;
    [SerializeField] private float fastMinInterval = 1.5f;
    [SerializeField] private float fastMaxInterval = 1.8f;

    [Header("<< UI >>")]
    [SerializeField] private TextMeshProUGUI ballCountText;

    [Header("<< Practice UI >>")]
    [SerializeField] private GameObject practiceHitPointUI;

    #region < Local Data >

    private Coroutine practiceCoroutine;
    private Coroutine gameCoroutine;

    private int currentThrow;

    private bool isPracticeRunning;
    private bool isGameRunning;

    public bool IsPracticeRunning => isPracticeRunning;
    public bool IsGameRunning => isGameRunning;
    public int CurrentThrow => currentThrow;

    #endregion

    #region < Networked UI >

    [Networked, OnChangedRender(nameof(OnBallCountUIChanged))]
    private NetworkBool IsBallCountVisible { get; set; }

    [Networked, OnChangedRender(nameof(OnBallCountUIChanged))]
    private int DisplayBallCount { get; set; }

    [Networked, OnChangedRender(nameof(OnPracticeHitPointUIChanged))]
    private NetworkBool IsPracticeHitPointVisible { get; set; }

    #endregion

    #region < Network >

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            IsBallCountVisible = false;
            DisplayBallCount = 0;
            IsPracticeHitPointVisible = false;
        }

        // 각 클라이언트에서 현재 네트워크 상태를 UI에 적용
        UpdateBallCountUI();
        UpdatePracticeHitPointUI();
    }

    #endregion

    #region < Begin >

    public void BeginPractice()
    {
        if (!Object.HasStateAuthority)        
            return;
        

        if (isPracticeRunning || isGameRunning)       
            return;
        

        practiceCoroutine = StartCoroutine(PracticeThrowRoutine());
    }

    public void BeginGame()
    {
        if (!Object.HasStateAuthority)        
            return;
        

        if (isPracticeRunning || isGameRunning)        
            return;
        

        gameCoroutine = StartCoroutine(GameThrowRoutine());
    }

    #endregion

    #region < Practice >

    private IEnumerator PracticeThrowRoutine()
    {
        isPracticeRunning = true;

        currentThrow = 0;

        SetBallCountUI(false, 0);

        // 모든 클라이언트에서 HitPoint 안내 UI 표시
        IsPracticeHitPointVisible = true;

        for (int i = 0; i < practiceThrows; i++)
        {
            SpawnBalls(practiceDuration, practiceHeight);

            currentThrow++;

            yield return new WaitForSeconds(practiceInterval);
        }

        // 모든 클라이언트에서 HitPoint 안내 UI 숨김
        IsPracticeHitPointVisible = false;

        isPracticeRunning = false;
        practiceCoroutine = null;
    }

    #endregion

    #region < Game >

    private IEnumerator GameThrowRoutine()
    {
        isGameRunning = true;

        currentThrow = 0;

        SetBallCountUI(true, totalThrows);

        for (int i = 0; i < totalThrows; i++)
        {
            currentThrow++;

            SetBallCountUI(true, totalThrows - i);

            float duration;
            float height;
            float interval;

            // 1 ~ 3번째 공
            if (i < 3)
            {
                duration = normalMaxDuration;
                height = normalMinHeight;
                interval = normalMaxInterval;
            }
            // 4 ~ 15번째 공
            else if (i < 15)
            {
                duration = Random.Range(normalMinDuration, normalMaxDuration);

                height = Random.Range(normalMinHeight, normalMaxHeight);

                interval = Random.Range(normalMinInterval, normalMaxInterval);
            }
            // 16 ~ 30번째 공
            else
            {
                duration = Random.Range(fastMinDuration, fastMaxDuration);

                height = Random.Range(fastMinHeight, fastMaxHeight);

                interval = Random.Range(fastMinInterval, fastMaxInterval);
            }

            SpawnBalls(duration, height);

            yield return new WaitForSeconds(interval);
        }

        // 네트워크 변수로 모든 클라이언트의 카운트 변경
        SetBallCountUI(true, 0);

        isGameRunning = false;
        gameCoroutine = null;
    }

    #endregion

    #region < Spawn >

    private void SpawnBalls(float duration, float height)
    {
        if (!Object.HasStateAuthority)        
            return;        

        if (!ballPrefab.IsValid)        
            return;
        
        if (startPoints == null || startPoints.Length < 4)       
            return;
        

        if (hitPoints == null || hitPoints.Length < 4)        
            return;
        

        for (int i = 0; i < 4; i++)
        {
            NetworkObject ballObject = Runner.Spawn(
                ballPrefab,
                startPoints[i].position,
                Quaternion.identity
            );

            if (ballObject == null)
            {
                Debug.LogError($"Ball Spawn 실패. OwnerIndex = {i}");

                continue;
            }

            Ball ball = ballObject.GetComponent<Ball>();

            if (ball == null)
            {
                Runner.Despawn(ballObject);
                continue;
            }

            ball.Initialize(
                startPoints[i].position,
                hitPoints[i].position,
                duration,
                height,
                i
            );
        }
    }

    #endregion

    #region < UI >

    private void OnBallCountUIChanged()
    {
        UpdateBallCountUI();
    }

    private void UpdateBallCountUI()
    {
        if (ballCountText == null)        
            return;
        

        ballCountText.gameObject.SetActive(IsBallCountVisible);

        ballCountText.text = DisplayBallCount.ToString();
    }

    private void SetBallCountUI(bool isVisible, int count)
    {
        if (!Object.HasStateAuthority)        
            return;
        

        IsBallCountVisible = isVisible;
        DisplayBallCount = count;

        // 호스트 화면도 즉시 갱신
        UpdateBallCountUI();
    }

    private void OnPracticeHitPointUIChanged()
    {
        UpdatePracticeHitPointUI();
    }

    private void UpdatePracticeHitPointUI()
    {
        if (practiceHitPointUI == null)       
            return;
        

        practiceHitPointUI.SetActive(IsPracticeHitPointVisible);
    }

    #endregion
}