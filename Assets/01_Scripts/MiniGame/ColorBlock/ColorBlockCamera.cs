using UnityEngine;

public class ColorBlockCamera : MonoBehaviour
{
    private Transform target;

    public void SetTarget(Transform targetTransform)
    {
        target = targetTransform;

        if (target == null)
            return;

        transform.position = target.position;
        transform.rotation = target.rotation;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // 플레이어와 완전히 같은 위치/회전으로 따라감
        transform.SetPositionAndRotation(
            target.position,
            target.rotation
        );
    }
}