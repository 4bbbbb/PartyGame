using UnityEngine;

public class BatTrigger : MonoBehaviour
{
    [Header("<< Player >>")]
    [SerializeField] private BaseballPlayer baseballPlayer;

    private void OnTriggerEnter(Collider other)
    {
        Ball ball = other.GetComponent<Ball>();

        if (ball == null)
            return;

        baseballPlayer.CheckHitTiming(ball);

        Debug.Log("Hit!!!!!!!!!!");
    }
}