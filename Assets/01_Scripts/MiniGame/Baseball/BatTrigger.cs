using UnityEngine;

public class BatTrigger : MonoBehaviour
{
    [Header("<< Player >>")]
    [SerializeField] private BaseballPlayer baseballPlayer;


    private void OnTriggerEnter(Collider other)
    {
        Ball ball = other.GetComponent<Ball>();

        if (ball == null)
        {
            return;
        }


        // -----------------------------------------------------
        // 내 공인지 확인
        // -----------------------------------------------------

        if (ball.OwnerIndex != baseballPlayer.PlayerIndex)
        {
            return;
        }


        Debug.Log(
            $"[BatTrigger] HIT REQUEST! " +
            $"PlayerIndex={baseballPlayer.PlayerIndex}, " +
            $"Ball={ball.Object.Id}"
        );


        // -----------------------------------------------------
        // Host에게 타격 판정 요청
        // -----------------------------------------------------

        baseballPlayer.RequestHit(ball);
    }
}