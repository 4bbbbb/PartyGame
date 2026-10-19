
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ShowCharacterController : MonoBehaviour
{
    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void OnDrawGizmos()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (controller == null)
            return;

        Gizmos.color = Color.green;

        Vector3 center = transform.TransformPoint(controller.center);
        float radius = controller.radius;
        float height = Mathf.Max(controller.height, radius * 2f);

        Vector3 top = center + Vector3.up * (height / 2f - radius);
        Vector3 bottom = center - Vector3.up * (height / 2f - radius);

        Gizmos.DrawWireSphere(top, radius);
        Gizmos.DrawWireSphere(bottom, radius);

        Gizmos.DrawLine(top + Vector3.forward * radius, bottom + Vector3.forward * radius);
        Gizmos.DrawLine(top - Vector3.forward * radius, bottom - Vector3.forward * radius);
        Gizmos.DrawLine(top + Vector3.right * radius, bottom + Vector3.right * radius);
        Gizmos.DrawLine(top - Vector3.right * radius, bottom - Vector3.right * radius);
    }
}
