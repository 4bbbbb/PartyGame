using UnityEngine;

public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    #region < Home >
    public void OnClickHome()
    {
        Debug.Log("===== HOME BUTTON CLICK =====");

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.LeaveRoom();
        }
        else
        {
            Debug.LogError("NetworkManager가 없습니다.");
        }
    }
    #endregion


    #region < Ready >
    public void OnClickReady()
    {
        Debug.Log("Ready 버튼 클릭");
    }
    #endregion


    #region < Start >
    public void OnClickStart()
    {
        Debug.Log("Start 버튼 클릭");
    }
    #endregion
}
