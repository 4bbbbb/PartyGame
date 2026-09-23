using Fusion;
using UnityEngine;
using System.Collections;

public class ColorBlock : NetworkBehaviour
{
    [Header("<< Block Info >>")]
    [SerializeField] private BlockColor blockColor;
    [SerializeField] private BlockLetter blockLetter;

    [Header("<< Fall >>")]
    [SerializeField] private float fallDistance = 5f;
    [SerializeField] private float fallDuration = 0.2f;
    [SerializeField] private float stayDuration = 1f;
    [SerializeField] private float returnDuration = 0.5f;

    public BlockColor BlockColor => blockColor;
    public BlockLetter BlockLetter => blockLetter;

    private Vector3 startPosition;
    private bool isFalling;


    private void Awake()
    {
        startPosition = transform.position;
    }


    public void Fall()
    {
        if (isFalling)
            return;

        StartCoroutine(FallCoroutine());
    }


    private IEnumerator FallCoroutine()
    {
        isFalling = true;

        Vector3 start = transform.position;

        Vector3 fallPosition =
            start + Vector3.down * fallDistance;


        // ========================================
        // 1. 0.2초 동안 아래로 내려가기
        // ========================================

        float time = 0f;

        while (time < fallDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / fallDuration);

            transform.position = Vector3.Lerp(start, fallPosition, t);

            yield return null;
        }

        transform.position = fallPosition;


        // ========================================
        // 2. 내려간 상태로 1초 유지
        // ========================================

        yield return new WaitForSeconds(stayDuration);


        // ========================================
        // 3. 0.5초 동안 원래 위치로 올라오기
        // ========================================

        time = 0f;

        while (time < returnDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / returnDuration);

            transform.position =
                Vector3.Lerp(
                    fallPosition,
                    startPosition,
                    t
                );

            yield return null;
        }

        transform.position = startPosition;

        isFalling = false;
    }
}