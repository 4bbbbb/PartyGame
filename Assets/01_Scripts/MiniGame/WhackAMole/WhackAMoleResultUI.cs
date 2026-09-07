using UnityEngine;

public class WhackAMoleResultUI : MonoBehaviour
{
    [Header("<< Panel >>")]
    [SerializeField] private GameObject panel;

    public void Show()
    {
        if (panel != null)
            panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}