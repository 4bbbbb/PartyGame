using UnityEngine;

/// <summary>
/// 
/// 1. 닉네임
/// 2. 캐릭터
/// 3. Ready
/// 4. 점수
/// 
/// </summary>

public static class PlayerData
{
    #region < Nickname >

    private const bool SAVE_NICKNAME = false;

    private const string NicknameKey = "Nickname";

    public static string Nickname { get; private set; }

    // 게임 시작 시 저장된 데이터 불러오기
    public static void Load()
    {
        if (!SAVE_NICKNAME)
        {
            Nickname = "";
            return;
        }

        Nickname = PlayerPrefs.GetString(NicknameKey, "");
    }

    // 닉네임 저장
    public static void SaveNickname(string nickname)
    {
        Nickname = nickname;

        if (SAVE_NICKNAME)
        {
            PlayerPrefs.SetString(NicknameKey, nickname);
            PlayerPrefs.Save();
        }
    }

    // 닉네임 저장 여부
    public static bool HasNickname()
    {
        return !string.IsNullOrEmpty(Nickname);
    }
    #endregion
}
