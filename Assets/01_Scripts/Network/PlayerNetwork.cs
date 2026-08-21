using Fusion;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    [Networked]
    public PlayerRef PlayerRef { get; set; }

    [Networked, OnChangedRender(nameof(OnNicknameChanged))]
    public NetworkString<_16> Nickname { get; set; }


    [Networked, OnChangedRender(nameof(OnReadyChanged))]
    public NetworkBool IsReady { get; set; }



    #region < Spawn >

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            PlayerRef = Object.InputAuthority;
        }

        Debug.Log($"PlayerNetwork Spawned : {Object.InputAuthority}");

        if (Object.HasInputAuthority)
        {
            Debug.Log($"내 닉네임 전달 : {PlayerData.Nickname}");

            RPC_SetNickname(PlayerData.Nickname);
        }
    }

    #endregion


    #region < Nickname >

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetNickname(string nickname)
    {
        Nickname = nickname;

        Debug.Log($"닉네임 네트워크 설정 완료 : {Nickname}");
    }


    private void OnNicknameChanged()
    {
        Debug.Log("===== 닉네임 변경 감지 =====");
        Debug.Log($"Player : {PlayerRef}");
        Debug.Log($"Nickname : {Nickname}");

        if (LobbyManager.Instance != null && Runner != null)
        {
            LobbyManager.Instance.RefreshPlayerList(Runner);
        }

        if (TagManager.Instance != null)
        {
            TagManager.Instance.SetupTagUI();
        }
    }

    #endregion


    #region < Ready >

    public void ToggleReady()
    {
        RPC_SetReady(!IsReady);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetReady(bool ready)
    {
        IsReady = ready;

        Debug.Log(
            $"Ready 상태 변경 : " +
            $"Player = {PlayerRef}, " +
            $"Ready = {IsReady}"
        );
    }

    private void OnReadyChanged()
    {
        Debug.Log(
            $"Ready 변경 : {PlayerRef} / {IsReady}"
        );

        if (LobbyManager.Instance != null && Runner != null)
        {
            LobbyManager.Instance.RefreshPlayerList(Runner);
        }

        if (LobbyUIManager.Instance != null)
        {
            LobbyUIManager.Instance.UpdateStartButton();
        }
    }

    #endregion
}