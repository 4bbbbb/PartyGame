using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(Rigidbody))]
public class DodgeItObstacle : NetworkBehaviour
{
    private Rigidbody rb;
    private Collider[] obstacleColliders;

    // 투하 상태 네트워크 동기화
    [Networked]
    private NetworkBool IsDropped { get; set; }

    // State Authority에서만 참조하는 대기 위치
    private Transform followTarget;

    #region < Unity >

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        obstacleColliders = GetComponentsInChildren<Collider>(true);

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        if (rb != null && !rb.isKinematic)
        {
            rb.angularVelocity = Vector3.zero;
        }

        SetColliders(false);
    }

    public override void Spawned()
    {
        // 물리 계산은 State Authority에서만 수행
        rb.isKinematic = !Object.HasStateAuthority || !IsDropped;
        rb.useGravity = Object.HasStateAuthority && IsDropped;

        ApplyDropState();

        Debug.Log(
            $"[DodgeItObstacle] Spawned | " +
            $"StateAuthority={Object.HasStateAuthority}"
        );
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (IsDropped)
            return;

        // 투하 전에는 Alien의 스폰 위치를 따라간다.
        if (followTarget != null)
        {
            transform.SetPositionAndRotation(
                followTarget.position,
                followTarget.rotation
            );
        }
    }

    public override void Render()
    {
        ApplyDropState();
    }

    #endregion

    #region < Follow >

    public void SetFollowTarget(Transform target)
    {
        if (Object == null || !Object.HasStateAuthority)
            return;

        followTarget = target;

        if (followTarget != null && !IsDropped)
        {
            transform.SetPositionAndRotation(
                followTarget.position,
                followTarget.rotation
            );
        }
    }

    #endregion

    #region < Drop >

    public void Drop()
    {
        if (Object == null || !Object.HasStateAuthority)
        {
            Debug.LogWarning(
                "[DodgeItObstacle] State Authority가 아니어서 투하할 수 없어."
            );
            return;
        }

        if (IsDropped)
            return;

        // 따라다니기를 중단하고 투하 상태로 전환
        IsDropped = true;
        followTarget = null;

        // Rigidbody 물리 시뮬레이션 시작
        rb.isKinematic = false;
        rb.useGravity = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 모든 클라이언트에서 충돌 활성화
        SetColliders(true);

        Debug.Log("[DodgeItObstacle] 장애물 투하!");
    }

    private void ApplyDropState()
    {
        if (rb == null || Object == null)
            return;

        // State Authority만 물리를 계산한다.
        rb.isKinematic = !Object.HasStateAuthority || !IsDropped;
        rb.useGravity = Object.HasStateAuthority && IsDropped;

        SetColliders(IsDropped);
    }

    private void SetColliders(bool isEnabled)
    {
        if (obstacleColliders == null)
            return;

        foreach (Collider col in obstacleColliders)
        {
            if (col != null)
                col.enabled = isEnabled;
        }
    }

    #endregion
}