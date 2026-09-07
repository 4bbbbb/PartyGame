using DG.Tweening;
using UnityEngine;

public class Dome : MonoBehaviour
{
    [Header("<< Character Point >>")]
    [SerializeField] private Transform characterPoint;

    [Header("<< Open >>")]
    [SerializeField] private float moveHeight = 3.5f;
    [SerializeField] private float duration = 1.5f;

    private Vector3 startPosition;
    private Vector3 startScale;
    private Renderer[] renderers;

    public Transform CharacterPoint => characterPoint;

    private void Awake()
    {
        startPosition = transform.localPosition;
        startScale = transform.localScale;
        renderers = GetComponentsInChildren<Renderer>();
    }

    public void Open()
    {
        transform.DOKill();

        transform.DOLocalMoveY(startPosition.y + moveHeight, duration).SetEase(Ease.OutCubic);

        foreach (Renderer renderer in renderers)
        {
            Material material = renderer.material;
            material.DOFade(0f, duration * 0.8f);
        }

        DOVirtual.DelayedCall(duration * 0.8f, () => gameObject.SetActive(false));
    }

    public void Reset()
    {
        transform.DOKill();

        gameObject.SetActive(true);

        transform.localPosition = startPosition;
        transform.localScale = startScale;

        foreach (Renderer renderer in renderers)
        {
            Material material = renderer.material;
            Color color = material.color;
            color.a = 1f;
            material.color = color;
        }
    }

    public void MovePlayer(WhackAMolePlayer player)
    {
        if (player == null || characterPoint == null)
            return;        

        player.transform.position = characterPoint.position;
        player.transform.rotation = characterPoint.rotation;
        player.transform.localScale = characterPoint.lossyScale;
        
    }
}