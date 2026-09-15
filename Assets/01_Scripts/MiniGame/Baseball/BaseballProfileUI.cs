using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BaseballProfileUI : MonoBehaviour
{ 
    [Header("<< Profile >>")]
    [SerializeField] private Image profileImage;
    [SerializeField] private TMP_Text nicknameText;

    [Header("<< Count >>")]
    [SerializeField] private TMP_Text playerCountText;

    public void Show(string nickname, Color profileColor, int count)
    {
        nicknameText.text = nickname;
        profileImage.color = profileColor;

        UpdateCount(count);
    }

    public void UpdateCount(int count)
    {
        playerCountText.text = count.ToString();
    }
}