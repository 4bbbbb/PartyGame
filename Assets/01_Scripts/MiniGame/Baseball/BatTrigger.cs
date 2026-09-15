using UnityEngine;

public class BatTrigger : MonoBehaviour
{
    [Header("<< Player >>")]
    [SerializeField] private BaseballPlayer baseballPlayer;


    private void OnTriggerEnter(Collider other)
    {
        Ball ball =
            other.GetComponent<Ball>();

        if (ball == null)
        {
            return;
        }


        // 내 공인지 확인
        if (ball.OwnerIndex !=
            baseballPlayer.PlayerIndex)
        {
            return;
        }


        Debug.Log(
            $"[BatTrigger] ★ 내 공 TriggerEnter 성공! " +
            $"Ball={ball.name}, " +
            $"OwnerIndex={ball.OwnerIndex}"
        );


        // ★ 실제 타격 판정
        baseballPlayer.CheckHitTiming(ball);
    }
}