using UnityEditor.PackageManager;
using UnityEngine;

public class BaseballPlayer : MonoBehaviour
{
    [Header("<< Animator >>")]
    [SerializeField] private Animator animator;

    [Header("<< Bat >>")]
    [SerializeField] private Collider batCollider;

    [Header("<< Score >>")]
    [SerializeField] private int excellentScore = 3;
    [SerializeField] private int goodScore = 1;

    [Header("<< Timing >>")]
    [SerializeField] private float excellentTiming = 0.011f;


    private void Update()
    {
        // Space 또는 마우스 왼쪽 클릭
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            animator.SetTrigger("Hit");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Ball ball = other.GetComponent<Ball>();

        if (ball == null)
            return;

        CheckHitTiming(ball);
    }

    public void CheckHitTiming(Ball ball)
    {
        if (ball == null)
            return;

        float error = Mathf.Abs(ball.HitTimeError);

        if (error <= excellentTiming)
        {
            Debug.Log("Excellent");

            ball.StopFlying();

            BallHitMotion ballHitMotion = ball.GetComponent<BallHitMotion>();

            if (ballHitMotion != null)
            {
                ballHitMotion.PlayExcellentMotion();
            }
        }
        else
        {
            Debug.Log("Good");

            ball.StopFlying();

            BallHitMotion ballHitMotion = ball.GetComponent<BallHitMotion>();

            if (ballHitMotion != null)
            {
                ballHitMotion.PlayGoodMotion();
            }
        }
    }
}