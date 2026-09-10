using UnityEngine;

public class Ball : MonoBehaviour
{
    [Header("<< Move >>")]
    [SerializeField] private float moveDuration = 2f;

    [Header("<< Peak >>")]
    [SerializeField, Range(0.5f, 0.6f)]
    private float peakPosition = 0.55f;

    [Header("<< Scale >>")]
    [SerializeField] private Vector3 startScale = new Vector3(4.5f, 4.5f, 4.5f);
    [SerializeField] private Vector3 hitScale = new Vector3(6.8f, 6.8f, 6.8f);

    [Header("<< Rotation >>")]
    [SerializeField] private float minRotationSpeed = 500f;
    [SerializeField] private float maxRotationSpeed = 1000f;

    // Start / Hit
    private Vector3 startPosition;
    private Vector3 hitPosition;
    private Vector3 horizontalDirection;

    // 포물선
    // y = ax² + bx + c
    private float a;
    private float b;
    private float c;

    // Start → Hit 수평 거리
    private float hitDistance;

    // Start → Ground까지의 수평 거리
    private float groundDistance;

    // 현재 시간
    private float moveTime;

    // Hit에 도달하는 시간
    private float hitTime;

    // 회전
    private float currentRotationSpeed;
    private Vector3 rotationAxis;

    private bool isFlying;


    public void Initialize(
        Vector3 startPosition,
        Vector3 hitPosition,
        float duration,
        float height)
    {
        this.startPosition = startPosition;
        this.hitPosition = hitPosition;

        hitTime = duration;

        // --------------------------------
        // Start → Hit 수평 방향
        // --------------------------------

        Vector3 horizontal =
            hitPosition - startPosition;

        horizontal.y = 0f;

        hitDistance = horizontal.magnitude;

        if (hitDistance <= 0.01f)
        {
            Debug.LogWarning("Ball : Start와 Hit의 거리가 너무 짧습니다.");
            return;
        }

        horizontalDirection =
            horizontal.normalized;


        // --------------------------------
        // 포물선 계산
        // --------------------------------

        CalculateParabola(height);


        // --------------------------------
        // Ground 위치 계산
        // --------------------------------

        CalculateGroundDistance();


        // --------------------------------
        // 초기화
        // --------------------------------

        transform.position = startPosition;
        transform.localScale = startScale;

        moveTime = 0f;
        isFlying = true;


        // --------------------------------
        // 회전
        // --------------------------------

        currentRotationSpeed =
            Random.Range(
                minRotationSpeed,
                maxRotationSpeed);

        rotationAxis =
            Random.onUnitSphere;
    }


    private void Update()
    {
        if (!isFlying)
            return;

        MoveBall();

        // Hit 이후에도 같은 회전 계속
        transform.Rotate(
            rotationAxis,
            currentRotationSpeed * Time.deltaTime,
            Space.Self);
    }


    private void CalculateParabola(float height)
    {
        /*
         * y = ax² + bx + c
         *
         * Start
         * x = 0
         * y = startPosition.y
         *
         * Peak
         * x = hitDistance * peakPosition
         * y = height
         *
         * Hit
         * x = hitDistance
         * y = hitPosition.y
         *
         * 이 세 점을 이용해서
         * 하나의 포물선을 만든다.
         */

        float startY = startPosition.y;

        float hitX = hitDistance;
        float hitY = hitPosition.y;

        float peakX = hitDistance * peakPosition;

        float peakY = height;


        // Start가 x = 0이므로
        // c = Start Y
        c = startY;


        float hitDeltaY = hitY - startY;

        float peakDeltaY = peakY - startY;


        // a 계산
        a =(hitDeltaY / hitX - peakDeltaY / peakX) / (hitX - peakX);


        // b 계산
        b = hitDeltaY / hitX - a * hitX;
    }


    private void CalculateGroundDistance()
    {
        /*
         * y = ax² + bx + c
         *
         * 바닥에서는 y = 0
         *
         * 따라서
         *
         * ax² + bx + c = 0
         *
         * 을 풀어서 Ground의 X 위치를 구한다.
         */

        float discriminant =
            b * b - 4f * a * c;

        if (discriminant < 0f)
        {
            Debug.LogWarning("Ball : 포물선이 y=0에 도달하지 않습니다.");

            groundDistance = hitDistance * 2f;
            return;
        }


        float sqrt = Mathf.Sqrt(discriminant);


        float root1 = (-b + sqrt) / (2f * a);

        float root2 = (-b - sqrt) / (2f * a);


        // Hit 이후의 양수 root를 Ground로 사용
        groundDistance = -1f;

        if (root1 > hitDistance)
            groundDistance = root1;

        if (root2 > hitDistance &&
            (groundDistance < 0f ||
             root2 < groundDistance))
        {
            groundDistance = root2;
        }


        if (groundDistance < 0f)
        {
            Debug.LogWarning("Ball : Hit 이후에 y=0이 되는 지점을 찾지 못했습니다.");

            groundDistance = hitDistance * 2f;
        }
    }


    private void MoveBall()
    {
        moveTime += Time.deltaTime;


        // --------------------------------
        // Hit 이후에도 계속 이동
        // --------------------------------

        /*
         * hitTime은 Start → Hit까지 걸리는 시간.
         *
         * Hit 이후에는 계속해서
         * 같은 속도로 수평 이동한다.
         */

        float horizontalSpeed = hitDistance / hitTime;


        float horizontalDistance = horizontalSpeed * moveTime;


        // --------------------------------
        // 포물선 Y 계산
        // --------------------------------

        float y = a * horizontalDistance * horizontalDistance + b * horizontalDistance + c;


        // --------------------------------
        // 위치 계산
        // --------------------------------

        Vector3 position = startPosition + horizontalDirection * horizontalDistance;

        position.y = y;

        transform.position = position;


        // --------------------------------
        // Scale
        // --------------------------------

        float scaleT = Mathf.Clamp01( horizontalDistance / hitDistance);

        transform.localScale = Vector3.Lerp(startScale, hitScale, scaleT);


        // --------------------------------
        // 바닥 도착
        // --------------------------------

        if (position.y <= 0f)
        {
            isFlying = false;

            Destroy(gameObject);
        }
    }
}