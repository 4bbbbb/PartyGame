using TMPro;
using UnityEngine;


/// <summary>
/// 
/// 1. 캐릭터 이미지
/// 2. 닉네임
/// 3. Ready 체크
/// 4. Host 왕관
/// </summary>

public class PlayerInfoUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text nicknameText;

    public void SetPlayer(string nickname)
    {
        Debug.Log($"SetPlayer : {nickname}");

        root.SetActive(true);
        nicknameText.text = nickname;
    }

    public void Clear()
    {
        root.SetActive(false);
        nicknameText.text = "";
    }
}
