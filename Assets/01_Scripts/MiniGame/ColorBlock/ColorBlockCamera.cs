using Unity.Cinemachine;
using UnityEngine;

public class ColorBlockCamera : MonoBehaviour
{
    [Header("<< Cinemachine >>")]
    [SerializeField] private CinemachineCamera cinemachineCamera;

    public void SetTarget(Transform target)
    {
        if (cinemachineCamera == null)
        {
            Debug.LogError(
                "[ColorBlockCamera] Cinemachine Camera가 연결되지 않았습니다."
            );

            return;
        }

        if (target == null)
        {
            Debug.LogError(
                "[ColorBlockCamera] Target이 없습니다."
            );

            return;
        }

        cinemachineCamera.Target.TrackingTarget = target;

        Debug.Log(
            $"[ColorBlockCamera] Cinemachine Tracking Target 연결 완료 : {target.name}"
        );
    }
}