using UnityEngine;

public class ColorBlockCamera : MonoBehaviour
{
    [Header("<< Follow >>")]
    [SerializeField] private float followSpeed = 15f;

    private Transform target;

    public void SetTarget(Transform targetTransform)
    {
        target = targetTransform;

        if (target == null)
            return;

        // 처음에는 바로 위치 이동
        transform.position = target.position;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // CameraTarget의 위치만 따라간다.
        // 회전은 절대 따라가지 않는다.
        transform.position = Vector3.Lerp(
            transform.position,
            target.position,
            followSpeed * Time.deltaTime
        );
    }
}