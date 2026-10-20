using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(Rigidbody))]
public class DodgeItObstacle : NetworkBehaviour
{
    private Rigidbody rb;
    private Collider[] obstacleColliders;

    [Networked]
    private NetworkBool IsDropped { get; set; }

    private Transform followTarget;


    #region < Unity >
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        obstacleColliders = GetComponentsInChildren<Collider>(true);

        rb.useGravity = false;
        rb.isKinematic = true;

        SetColliders(false);
    }


    public override void Spawned()
    {
        rb.isKinematic = !Object.HasStateAuthority || !IsDropped;
        rb.useGravity = Object.HasStateAuthority && IsDropped;

        ApplyDropState();     
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (IsDropped)
            return;

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

        IsDropped = true;
        followTarget = null;

        rb.isKinematic = false;
        rb.useGravity = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        SetColliders(true);

        Debug.Log("[DodgeItObstacle] 장애물 투하!");
    }

    private void ApplyDropState()
    {
        if (rb == null || Object == null)
            return;

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