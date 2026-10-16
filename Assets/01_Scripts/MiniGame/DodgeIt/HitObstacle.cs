
using UnityEngine;

public class HitObstacle : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        DodgeItPlayer player = other.GetComponent<DodgeItPlayer>();

        if (player == null)
            return;

        Destroy(player.gameObject);
    }
}
