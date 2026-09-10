using UnityEngine;

public class BaseballPlayer : MonoBehaviour
{
    [Header("<< Animator >>")]
    [SerializeField] private Animator animator;

    private void Update()
    {
        // Space 또는 마우스 왼쪽 클릭
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            animator.SetTrigger("Hit");
        }
    }
}