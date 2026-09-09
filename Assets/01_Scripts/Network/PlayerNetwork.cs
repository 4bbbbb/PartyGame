using Fusion;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    [Networked]
    public PlayerRef PlayerRef { get; set; }

    [Networked, OnChangedRender(nameof(OnNicknameChanged))]
    public NetworkString<_32> Nickname { get; set; }

    [Networked, OnChangedRender(nameof(OnCharacterIndexChanged))]
    public int CharacterIndex { get; set; } = -1;

    [Networked, OnChangedRender(nameof(OnReadyChanged))]
    public NetworkBool IsReady { get; set; }

    [Networked]
    public int Score { get; set; }


    #region < Spawn >

    public override void Spawned()
    {
        DontDestroyOnLoad(gameObject);

        if (Object.HasStateAuthority)
        {
            PlayerRef = Object.InputAuthority;
            CharacterIndex = -1;
            IsReady = false;
        }

        Debug.Log($"PlayerNetwork Spawned : {Object.InputAuthority}");

        if (Object.HasInputAuthority)
        {           
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


    #region < Character >

    // 캐릭터 선택 요청
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestCharacterSelect(int characterIndex, RpcInfo info = default)
    {
        // 다른 플레이어가 이미 선택했는지 검사
        PlayerNetwork[] players =
            FindObjectsByType<PlayerNetwork>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (PlayerNetwork player in players)
        {
            // 자기 자신은 검사하지 않음
            if (player == this)
                continue;

            if (player.CharacterIndex == characterIndex)
            {
                Debug.Log(
                    $"캐릭터 선택 실패 : 이미 사용 중 " +
                    $"CharacterIndex = {characterIndex}"
                );

                RPC_CharacterSelectResult(false, characterIndex);

                return;
            }
        }

        // 선택 성공
        CharacterIndex = characterIndex;       

        RPC_CharacterSelectResult(true, characterIndex);
    }


    // 선택 결과를 요청한 플레이어에게만 전달
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_CharacterSelectResult(
        bool success,
        int characterIndex,
        RpcInfo info = default)
    {
        if (CharacterSelectUI.Instance == null)
            return;

        if (success)
        {
            CharacterSelectUI.Instance.OnCharacterSelectSuccess(
                characterIndex
            );
        }
        else
        {
            CharacterSelectUI.Instance.OnCharacterSelectFailed(
                characterIndex
            );
        }
    }


    private void OnCharacterIndexChanged()
    {
        Debug.Log(
            $"캐릭터 선택 변경 : " +
            $"Player = {PlayerRef}, " +
            $"CharacterIndex = {CharacterIndex}"
        );

        // 다른 플레이어가 선택한 캐릭터를
        // 현재 선택 화면에서 바로 반영할 수 있게 함
        if (CharacterSelectUI.Instance != null)
        {
            CharacterSelectUI.Instance.RefreshAvailableCharacters();
        }

        // 플레이어 리스트 갱신
        if (LobbyManager.Instance != null && Runner != null)
        {
            LobbyManager.Instance.RefreshPlayerList(Runner);
        }
    }

    #endregion


    #region < Ready >

    public void ToggleReady()
    {
        // 캐릭터 선택을 하지 않았다면 Ready 불가능
        if (CharacterIndex < 0)
        {
            Debug.Log("캐릭터를 먼저 선택해야 Ready할 수 있습니다.");
            return;
        }

        RPC_SetReady(!IsReady);
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetReady(bool ready)
    {
        if (CharacterIndex < 0)
            return;
        
        IsReady = ready;
    }


    private void OnReadyChanged()
    {
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


    #region < Score >
    public void AddScore(int score)
    {
        if (!Object.HasStateAuthority)
            return;

        Score += score;
    }
    #endregion
}