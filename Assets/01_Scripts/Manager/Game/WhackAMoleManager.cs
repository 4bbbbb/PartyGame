using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class WhackAMoleManager : NetworkBehaviour
{
    #region < Data >

    [Header("<< Game Data >>")]
    [SerializeField] private WhackAMoleData gameData;

    [Header("<< Player >>")]
    [SerializeField] private NetworkObject playerPrefab;

    [Header("<< Character >>")]
    [SerializeField] private CharacterDatabase characterDatabase;

    [Header("<< Spawn Points >>")]
    [SerializeField] private Transform tagSpawnPoint;
    [SerializeField] private Transform[] playerSpawnPoints;

    [Header("<< UI >>")]
    [SerializeField] private TagProfileUI tagProfileUI;
    [SerializeField] private TagHoleSelectUI tagHoleSelectUI;
    [SerializeField] private PlayerHoleSelectUI playerHoleSelectUI;
    [SerializeField] private WhackAMoleRoundUI roundUI;
    [SerializeField] private WhackAMoleResultUI resultUI;
    [SerializeField] private WhackAMoleEndUI endUI;

    [Header("<< Tag Announcement >>")]
    [SerializeField] private TMP_Text tagAnnouncementText;

    [Header("<< Dome >>")]
    [SerializeField] private Dome[] domes;

    [Header("<< Result VFX >>")]
    [SerializeField] private GameObject resultVFX;
    [SerializeField] private float resultVFXDuration = 1f;

    #endregion


    #region < Hole Enum >

    public enum HoleType
    {
        Hole1 = 1,
        Hole2 = 2,
        Hole3 = 3,
        Hole4 = 4
    }

    #endregion


    #region < Networked Data >

    [Networked] public int CurrentRound { get; private set; }

    [Networked] public int TagHP { get; private set; }

    [Networked] public PlayerRef TagPlayer { get; private set; }

    [Networked] public WhackAMoleState State { get; private set; }

    [Networked] private HoleType TagHole { get; set; }

    [Networked, Capacity(4)]
    private NetworkArray<HoleType> PlayerChoices => default;

    [Networked, Capacity(4)]
    private NetworkArray<NetworkBool> PlayerChoiceCompleted => default;

    [Networked, Capacity(4)]
    private NetworkArray<int> PlayerHitCounts => default;

    #endregion


    #region < Local Data >

    // 실제 게임에 Spawn된 캐릭터
    private readonly Dictionary<PlayerRef, WhackAMolePlayer> spawnedPlayers = new();

    private ScoreManager GetScoreManager()
    {
        return FindFirstObjectByType<ScoreManager>();
    }

    #endregion


    #region < Network >

    private List<PlayerNetwork> GetActivePlayers()
    {
        List<PlayerNetwork> players = new();

        foreach (PlayerRef playerRef in Runner.ActivePlayers)
        {
            NetworkObject playerObject = Runner.GetPlayerObject(playerRef);

            if (playerObject == null)
            {
                Debug.LogWarning($"PlayerObject를 찾을 수 없습니다. PlayerRef = {playerRef}");
                continue;
            }

            PlayerNetwork playerNetwork = playerObject.GetComponent<PlayerNetwork>();

            if (playerNetwork == null)
            {
                Debug.LogError($"PlayerObject에 PlayerNetwork가 없습니다. PlayerRef = {playerRef}");
                continue;
            }

            players.Add(playerNetwork);
        }

        return players
            .OrderBy(player => player.PlayerRef.RawEncoded)
            .ToList();
    }

    #endregion


    #region < Spawn >

    public override void Spawned()
    {
        if (!Object.HasStateAuthority)
            return;

        InitializeGame();
    }


    private void SpawnPlayers()
    {
        if (Runner == null)
        {
            Debug.LogError("Runner가 없습니다.");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("WhackAMole Player Prefab이 연결되지 않았습니다.");
            return;
        }

        if (characterDatabase == null)
        {
            Debug.LogError("CharacterDatabase가 연결되지 않았습니다.");
            return;
        }

        List<PlayerNetwork> players = GetActivePlayers();

        if (players.Count == 0)
        {
            Debug.LogError("PlayerNetwork를 하나도 찾지 못했습니다.");
            return;
        }

        for (int i = 0; i < players.Count; i++)
        {
            PlayerNetwork player = players[i];

            if (spawnedPlayers.ContainsKey(player.PlayerRef))
                continue;

            Transform spawnPoint;

            if (player.PlayerRef == TagPlayer)
            {
                spawnPoint = tagSpawnPoint;
            }
            else
            {
                int normalPlayerIndex = GetNormalPlayerIndex(players, player);

                if (normalPlayerIndex < 0 ||
                    normalPlayerIndex >= playerSpawnPoints.Length)
                {
                    Debug.LogError($"일반 플레이어 Spawn Point가 부족합니다. Player = {player.PlayerRef}");

                    continue;
                }

                spawnPoint = playerSpawnPoints[normalPlayerIndex];
            }

            NetworkObject playerObject = Runner.Spawn(
                playerPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                player.PlayerRef
            );

            if (playerObject == null)
            {
                Debug.LogError($"게임 플레이어 Spawn 실패 : {player.PlayerRef}");
                continue;
            }

            playerObject.transform.localScale = spawnPoint.lossyScale;

            WhackAMolePlayer whackAMolePlayer = playerObject.GetComponent<WhackAMolePlayer>();

            if (whackAMolePlayer == null)
            {
                Debug.LogError("WhackAMolePlayer 컴포넌트를 찾을 수 없습니다.");
                continue;
            }

            whackAMolePlayer.SetCharacterIndex(player.CharacterIndex);
            spawnedPlayers.Add(player.PlayerRef, whackAMolePlayer);
        }
    }


    private int GetNormalPlayerIndex(List<PlayerNetwork> players, PlayerNetwork targetPlayer)
    {
        int index = 0;

        foreach (PlayerNetwork player in players)
        {
            if (player.PlayerRef == TagPlayer)
                continue;

            if (player.PlayerRef == targetPlayer.PlayerRef)
                return index;

            index++;
        }

        return -1;
    }

    #endregion


    #region < Initialize >

    private void InitializeGame()
    {
        if (gameData == null)
        {
            Debug.LogError("WhackAMoleData가 연결되지 않았습니다.");
            return;
        }

        CurrentRound = 1;
        TagHP = gameData.TagHP;
        State = WhackAMoleState.Waiting;

        for (int i = 0; i < 4; i++)
        {
            PlayerHitCounts.Set(i, 0);

        }
    }

    #endregion


    #region < Start >

    public void SetTagPlayer(PlayerRef tagPlayer)
    {
        if (!Object.HasStateAuthority)
            return;

        TagPlayer = tagPlayer;
    }


    public void StartGame()
    {
        if (!Object.HasStateAuthority)
            return;

        if (TagPlayer == default)
        {
            Debug.LogWarning("TagPlayer가 설정되지 않았습니다.");
            return;
        }

        State = WhackAMoleState.Waiting;

        SpawnPlayers();
        StartCoroutine(StartGameIntroSequence());
    }


    private IEnumerator StartGameIntroSequence()
    {
        if (!Object.HasStateAuthority)
            yield break;

        yield return new WaitForSeconds(0.5f);

        PlayTagGreeting();
        RPC_ShowTagAnnouncement();

        yield return new WaitForSeconds(1f);

        RPC_HideTagAnnouncement();    

        StartCoroutine(StartRoundSequence());

    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowTagAnnouncement()
    {
        if (tagAnnouncementText == null)
            return;

        NetworkObject playerObject = Runner.GetPlayerObject(TagPlayer);

        if (playerObject == null)
        {
            Debug.LogWarning($"TAG PlayerObject를 찾을 수 없습니다. PlayerRef = {TagPlayer}");
            return;
        }

        PlayerNetwork tagPlayer = playerObject.GetComponent<PlayerNetwork>();

        if (tagPlayer == null)
        {
            Debug.LogWarning("TAG PlayerObject에 PlayerNetwork가 없습니다.");
            return;
        }

        tagAnnouncementText.text = $"두더지는 {tagPlayer.Nickname} 입니다";
        tagAnnouncementText.gameObject.SetActive(true);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HideTagAnnouncement()
    {
        if (tagAnnouncementText == null)
            return;

        tagAnnouncementText.gameObject.SetActive(false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowTagProfile()
    {
        if (tagProfileUI == null)
        {
            Debug.LogWarning("TagProfileUI가 연결되지 않았습니다.");
            return;
        }

        NetworkObject playerObject = Runner.GetPlayerObject(TagPlayer);

        if (playerObject == null)
        {
            Debug.LogWarning($"TAG PlayerObject를 찾을 수 없습니다. PlayerRef = {TagPlayer}");
            return;
        }

        PlayerNetwork tagPlayer = playerObject.GetComponent<PlayerNetwork>();

        if (tagPlayer == null)
        {
            Debug.LogWarning("TAG PlayerObject에 PlayerNetwork가 없습니다.");
            return;
        }

        int characterIndex = tagPlayer.CharacterIndex;

        if (characterIndex < 0 || characterIndex >= characterDatabase.characters.Length)
        {
            Debug.LogWarning($"잘못된 CharacterIndex : {characterIndex}");
            return;
        }

        CharacterData characterData = characterDatabase.characters[characterIndex];

        tagProfileUI.Show(
            tagPlayer.Nickname.ToString(),
            TagHP,
            characterData.characterColor
        );
    }

    private void PlayTagGreeting()
    {
        if (!Object.HasStateAuthority)
            return;

        if (!spawnedPlayers.TryGetValue(TagPlayer, out WhackAMolePlayer tagPlayer))
        {
            Debug.LogWarning( $"TAG 캐릭터를 찾을 수 없습니다. PlayerRef = {TagPlayer}");

            return;
        }

        tagPlayer.RPC_PlayGreetingAnimation();
    }

    #endregion


    #region < Round >

    private IEnumerator StartRoundSequence()
    {
        if (!Object.HasStateAuthority)
            yield break;

        ResetRoundData();
        ResetRoundPositions();

        RPC_HideResultUI();

        yield return new WaitForSeconds(0.5f);

        RPC_ShowRoundUI(CurrentRound);

        yield return new WaitForSeconds(1.5f);

        RPC_HideRoundUI();

        yield return new WaitForSeconds(0.5f);

        State = WhackAMoleState.TagSelecting;

        RPC_ShowTagProfile();
        RPC_ShowTagHoleSelectUI();
    }


    private IEnumerator EndRoundSequence()
    {
        if (!Object.HasStateAuthority)
            yield break;

        if (TagHP <= 0)
        {
            GiveGameScores();
            StartGameResult();
            yield break;
        }

        if (CurrentRound >= 3)
        {
            GiveGameScores();
            StartGameResult();
            yield break;
        }

        CurrentRound++;

        yield return new WaitForSeconds(1f);

        StartCoroutine(StartRoundSequence());
    }


    private void ResetRoundData()
    {
        TagHole = default;

        for (int i = 0; i < 4; i++)
        {
            PlayerChoices.Set(i, HoleType.Hole1);
            PlayerChoiceCompleted.Set(i, false);
        }
    }


    private void ResetRoundPositions()
    {
        List<PlayerNetwork> players = GetActivePlayers();
        int normalPlayerIndex = 0;

        foreach (PlayerNetwork player in players)
        {
            if (!spawnedPlayers.TryGetValue(
                    player.PlayerRef,
                    out WhackAMolePlayer character))
            {
                continue;
            }

            Transform spawnPoint;

            if (player.PlayerRef == TagPlayer)
            {
                spawnPoint = tagSpawnPoint;
            }
            else
            {
                if (normalPlayerIndex >= playerSpawnPoints.Length)
                    continue;

                spawnPoint = playerSpawnPoints[normalPlayerIndex];
                normalPlayerIndex++;
            }

            character.transform.position = spawnPoint.position;
            character.transform.rotation = spawnPoint.rotation;
            character.transform.localScale = spawnPoint.lossyScale;
        }

        RPC_ResetPlayerFaces();
        RPC_ResetDomes();
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ResetDomes()
    {
        if (domes == null)
            return;

        foreach (Dome dome in domes)
        {
            if (dome != null)
                dome.Reset();
        }
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowRoundUI(int round)
    {
        if (roundUI == null)
            return;

        roundUI.Show(round);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HideRoundUI()
    {
        if (roundUI == null)
            return;

        roundUI.Hide();
    }

    #endregion


    #region < Tag Select >

    public void SelectTagHole(HoleType hole)
    {
        if (Runner == null)
            return;

        if (Runner.LocalPlayer != TagPlayer)
            return;

        if (State != WhackAMoleState.TagSelecting)
            return;

        RPC_SelectTagHole(hole);
    }


    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    private void RPC_SelectTagHole(HoleType hole, RpcInfo info = default)
    {
        if (State != WhackAMoleState.TagSelecting)
            return;

        if (info.Source != TagPlayer)
            return;

        TagHole = hole;

        MoveTagToSelectedHole();

        StartCoroutine(TagSelectionCompleteSequence());
    }


    private void MoveTagToSelectedHole()
    {
        if (!spawnedPlayers.TryGetValue(TagPlayer, out WhackAMolePlayer tagPlayer))
        {
            Debug.LogWarning($"TAG 캐릭터를 찾을 수 없습니다. PlayerRef = {TagPlayer}");
            return;
        }

        int domeIndex = (int)TagHole - 1;

        if (domes == null || domes.Length == 0)
        {
            Debug.LogError("Dome이 없습니다.");
            return;
        }

        if (domeIndex < 0 || domeIndex >= domes.Length)
        {
            Debug.LogError($"잘못된 Dome Index : {domeIndex}");
            return;
        }

        Dome dome = domes[domeIndex];

        if (dome == null)
        {
            Debug.LogError($"Dome[{domeIndex}]가 null입니다.");
            return;
        }

        if (dome.CharacterPoint == null)
        {
            Debug.LogError($"Dome[{domeIndex}]의 CharacterPoint가 없습니다.");
            return;
        }        

        dome.MovePlayer(tagPlayer);    
        
    }


    private IEnumerator TagSelectionCompleteSequence()
    {
        RPC_ShowTagSelectComplete();

        yield return new WaitForSeconds(1.5f);

        RPC_HideTagSelectUI();

        State = WhackAMoleState.PlayerSelecting;

        RPC_ShowPlayerSelectUI();
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowTagHoleSelectUI()
    {
        if (tagHoleSelectUI == null)
        {
            Debug.LogWarning("TagHoleSelectUI가 연결되지 않았습니다.");
            return;
        }

        tagHoleSelectUI.Show();
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HideTagSelectUI()
    {
        if (tagHoleSelectUI == null)
            return;

        tagHoleSelectUI.Hide();
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowTagSelectComplete()
    {
        if (tagHoleSelectUI == null)
            return;

        tagHoleSelectUI.ShowComplete();
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowPlayerSelectUI()
    {
        if (playerHoleSelectUI == null)
            return;

        playerHoleSelectUI.Show();
    }

    #endregion


    #region < Player Select >

    public void SelectPlayerHole(HoleType hole)
    {
        if (Runner == null)
            return;

        if (State != WhackAMoleState.PlayerSelecting)
            return;

        if (Runner.LocalPlayer == TagPlayer)
            return;

        RPC_SelectPlayerHole(hole);
    }


    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    private void RPC_SelectPlayerHole(HoleType hole, RpcInfo info = default)
    {
        if (State != WhackAMoleState.PlayerSelecting)
            return;

        if (info.Source == TagPlayer)
            return;

        List<PlayerNetwork> players = GetActivePlayers();

        int playerIndex = GetPlayerIndex(info.Source);

        if (playerIndex < 0)
            return;

        if (PlayerChoiceCompleted[playerIndex])
        {
            Debug.Log($"이미 선택을 완료한 플레이어입니다. Player = {info.Source}");

            return;
        }

        PlayerChoices.Set(playerIndex, hole);
        PlayerChoiceCompleted.Set(playerIndex, true);

        int normalPlayerIndex = 0;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].PlayerRef == TagPlayer)
                continue;

            if (players[i].PlayerRef == info.Source)
                break;

            normalPlayerIndex++;
        }

        RPC_ShowPlayerSelected(normalPlayerIndex);

        if (spawnedPlayers.TryGetValue(info.Source, out WhackAMolePlayer playerCharacter))
        {
            playerCharacter.RPC_PlayButtonAnimation();
        }
        else
        {
            Debug.LogWarning($"선택한 플레이어의 캐릭터를 찾을 수 없습니다. Player = {info.Source}");
        }

        CheckPlayerSelectionComplete();
    }


    private void CheckPlayerSelectionComplete()
    {
        List<PlayerNetwork> players = GetActivePlayers();

        int normalPlayerCount = 0;
        int completedCount = 0;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].PlayerRef == TagPlayer)
                continue;

            normalPlayerCount++;

            if (PlayerChoiceCompleted[i])
                completedCount++;
        }

        if (completedCount >= normalPlayerCount)
            StartCoroutine(PlayerSelectionCompleteSequence());
    }


    private IEnumerator PlayerSelectionCompleteSequence()
    {
        RPC_ShowPlayerSelectComplete();

        yield return new WaitForSeconds(1.5f);

        RPC_HidePlayerSelectComplete();

        RPC_ShowResultUI();
        RPC_OpenDomes();

        yield return new WaitForSeconds(1.5f);

        yield return StartCoroutine(StartResultSequence());
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowPlayerSelected(int playerIndex)
    {
        if (playerHoleSelectUI == null)
            return;

        playerHoleSelectUI.ShowPlayerSelected(playerIndex);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowPlayerSelectComplete()
    {
        if (playerHoleSelectUI == null)
            return;

        playerHoleSelectUI.ShowComplete();
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HidePlayerSelectComplete()
    {
        if (playerHoleSelectUI == null)
            return;

        playerHoleSelectUI.HideComplete();
    }

    #endregion


    #region < Result >

    private List<PlayerNetwork> GetNormalPlayers()
    {
        return GetActivePlayers()
            .Where(player => player.PlayerRef != TagPlayer)
            .ToList();
    }


    private IEnumerator StartResultSequence()
    {
        if (!Object.HasStateAuthority)
            yield break;

        yield return new WaitForSeconds(1f);

        List<PlayerNetwork> players = GetNormalPlayers();

        for (int i = 0; i < players.Count; i++)
        {
            PlayerNetwork player = players[i];

            int playerIndex = GetPlayerIndex(player.PlayerRef);

            if (playerIndex < 0)
                continue;

            bool isHit = PlayerChoices[playerIndex] == TagHole;

            RPC_PlayResultVFX(PlayerChoices[playerIndex]);

            if (spawnedPlayers.TryGetValue(player.PlayerRef, out WhackAMolePlayer playerCharacter))
            {
                if (isHit)
                {
                    playerCharacter.RPC_PlayHappyAnimation();

                    if (spawnedPlayers.TryGetValue(TagPlayer, out WhackAMolePlayer tagCharacter))
                    {
                        tagCharacter.RPC_PlayHitAnimation();
                    }
                }
                else
                {
                    playerCharacter.RPC_PlaySadAnimation();

                    if (spawnedPlayers.TryGetValue(TagPlayer, out WhackAMolePlayer tagCharacter))
                    {
                        tagCharacter.RPC_PlayHappyAnimation();
                    }
                }
            }

            if (isHit)
            {
                PlayerHitCounts.Set(playerIndex, PlayerHitCounts[playerIndex] + 1);
                Debug.Log($"Hit : {player.PlayerRef} / 누적 Hit : {PlayerHitCounts[playerIndex]}");

                TagHP--;
                RPC_UpdateTagProfileHP(TagHP);
            }

            yield return new WaitForSeconds(3f);
        }

        StartCoroutine(EndRoundSequence());
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateTagProfileHP(int hp)
    {
        if (tagProfileUI == null)
        {
            Debug.LogWarning("TagProfileUI가 연결되지 않았습니다.");
            return;
        }

        tagProfileUI.UpdateHP(hp);
    }


    private int GetPlayerIndex(PlayerRef playerRef)
    {
        List<PlayerNetwork> players = GetActivePlayers();

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].PlayerRef == playerRef)
                return i;
        }

        return -1;
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OpenDomes()
    {
        if (domes == null)
            return;

        foreach (Dome dome in domes)
        {
            if (dome != null)
                dome.Open();
        }
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ResetPlayerFaces()
    {
        foreach (WhackAMolePlayer player in spawnedPlayers.Values)
        {
            if (player != null)
                player.ResetFace();
        }
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayResultVFX(HoleType hole)
    {
        if (resultVFX == null)
        {
            Debug.LogWarning("Result VFX가 연결되지 않았습니다.");
            return;
        }

        int domeIndex = (int)hole - 1;

        if (domeIndex < 0 || domeIndex >= domes.Length)
        {
            Debug.LogWarning($"잘못된 VFX Dome Index : {domeIndex}");
            return;
        }

        Transform vfxPoint = domes[domeIndex].CharacterPoint;

        if (vfxPoint == null)
        {
            Debug.LogWarning($"Dome {domeIndex + 1}의 CharacterPoint가 연결되지 않았습니다.");

            return;
        }

        GameObject vfx = Instantiate(resultVFX, vfxPoint.position, vfxPoint.rotation);

        Destroy(vfx, resultVFXDuration);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowResultUI()
    {
        if (resultUI == null)
            return;

        resultUI.Show();
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HideResultUI()
    {
        if (resultUI == null)
            return;

        resultUI.Hide();
    }

    #endregion


    #region < End >

    private void StartGameResult()
    {
        State = WhackAMoleState.GameResult;

        RPC_HideResultUI();
        RPC_ShowEndUI();
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowEndUI()
    {
        if (endUI == null)
            return;

        endUI.Show();
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HideEndUI()
    {
        if (endUI == null)
            return;

        endUI.Hide();
    }

    #endregion


    #region < Score >

    private Dictionary<PlayerRef, int> CalculateGameScores()
    {
        Dictionary<PlayerRef, int> scores = new();

        if (TagHP > 0)
        {
            scores[TagPlayer] = 3;
            return scores;
        }

        List<PlayerNetwork> players = GetNormalPlayers();

        players = players
            .OrderByDescending(player => PlayerHitCounts[GetPlayerIndex(player.PlayerRef)])
            .ToList();

        if (players.Count == 0)
            return scores;

        int firstHitCount = PlayerHitCounts[GetPlayerIndex(players[0].PlayerRef)];
        int secondHitCount = players.Count > 1 ? PlayerHitCounts[GetPlayerIndex(players[1].PlayerRef)] : -1;

        bool firstPlaceTie = firstHitCount == secondHitCount;

        if (firstPlaceTie)
        {
            foreach (PlayerNetwork player in players)
            {
                int hitCount = PlayerHitCounts[GetPlayerIndex(player.PlayerRef)];

                if (hitCount == firstHitCount)
                    scores[player.PlayerRef] = 2;
                else
                    scores[player.PlayerRef] = 1;
            }

            return scores;
        }

        scores[players[0].PlayerRef] = 3;

        for (int i = 1; i < players.Count; i++)
            scores[players[i].PlayerRef] = 2;

        if (players.Count >= 3)
            scores[players[2].PlayerRef] = 1;

        return scores;
    }

    private void GiveGameScores()
    {
        ScoreManager scoreManager = GetScoreManager();

        if (scoreManager == null)
        {
            Debug.LogWarning("ScoreManager를 찾을 수 없습니다.");
            return;
        }

        Dictionary<PlayerRef, int> scores = CalculateGameScores();

        foreach (KeyValuePair<PlayerRef, int> score in scores)
        {
            scoreManager.AddScore(score.Key, score.Value);

            Debug.Log($"===== WhackAMole 점수 지급 =====\nPlayer : {score.Key}\n점수 : +{score.Value}");
        }
    }

    #endregion
}
