using TMPro;
using UnityEngine;

/// <summary>
/// 
/// 1. Play 버튼 클릭
/// 2. 닉네임 여부 확인
/// 3. Nickname Panel
/// 4. NetworkManager.Play()
/// 
/// </summary>

public class TitleManager : MonoBehaviour
{
    [Header("<< UI >>")]
    [SerializeField] private GameObject nicknamePanel;
    [SerializeField] private TMP_InputField nicknameInput;

    private const string NicknameKey = "Nickname";

    private void Start()
    {
        PlayerData.Load();
    }

    #region < Play 버튼 >
    public void OnClickPlay()
    {
        Debug.Log(NetworkManager.Instance);

        if (PlayerData.HasNickname())
        {
            NetworkManager.Instance.Play();
        }
        else
        {
            nicknamePanel.SetActive(true);
        }
    }
    #endregion

    #region < Confirm 버튼 >
    public void OnClickConfirm()
    {
        string nickname = nicknameInput.text.Trim();

        if (string.IsNullOrEmpty(nickname))
            return;

        Debug.Log("Confirm 클릭");

        PlayerData.SaveNickname(nickname);

        NetworkManager.Instance.Play();
    }
    #endregion
}
