
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DodgeItObstacle : MonoBehaviour
{
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void Drop()
    {
        rb.useGravity = true;
    }
}
