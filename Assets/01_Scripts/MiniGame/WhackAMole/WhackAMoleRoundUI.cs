using TMPro;
using UnityEngine;

public class WhackAMoleRoundUI : MonoBehaviour
{
    [Header("<< Panel >>")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text roundText;

    public void Show(int round)
    {
        if (roundText != null)
            roundText.text = $"Round {round}";

        if (panel != null)
            panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}