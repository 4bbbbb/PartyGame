using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TagProfileUI : MonoBehaviour
{   
    [Header("<< Panel >>")]
    [SerializeField] private GameObject panel;

    [Header("<< Profile >>")]
    [SerializeField] private Image profileImage;
    [SerializeField] private TMP_Text nicknameText;

    [Header("<< HP >>")]
    [SerializeField] private GameObject[] filledHearts;

    

    public void Show(string nickname, int hp, Color profileColor)
    {
        nicknameText.text = nickname;

        profileImage.color = profileColor;

        UpdateHP(hp);

        panel.SetActive(true);
    }

    public void UpdateHP(int hp)
    {
        hp = Mathf.Clamp(hp, 0, filledHearts.Length);

        for (int i = 0; i < filledHearts.Length; i++)
        {
            filledHearts[i].SetActive(i < hp);
        }
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}
