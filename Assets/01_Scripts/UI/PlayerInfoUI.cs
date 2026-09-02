using TMPro;
using UnityEngine;
using UnityEngine.UI;


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
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private Image checkImage;
    [SerializeField] private Image characterImage;


    public void SetPlayer(string nickname, CharacterData characterData)
    {
        root.SetActive(true);
        nicknameText.text = nickname;

        if (characterData != null)
        {
            characterNameText.text = characterData.characterName;
            characterImage.color = characterData.characterColor;
        }
        else
        {
            characterNameText.text = "";
            characterImage.color = Color.white;
        }
    }    

    public void SetReady(bool ready)
    {
        checkImage.gameObject.SetActive(ready);
    }

    public void Clear()
    {
        root.SetActive(false);
        nicknameText.text = "";
        characterNameText.text = "";
        characterImage.color = Color.white;
        checkImage.gameObject.SetActive(false);
    }
}
