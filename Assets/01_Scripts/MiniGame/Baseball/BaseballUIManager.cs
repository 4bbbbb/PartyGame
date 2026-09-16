using Fusion;
using UnityEngine;
using System.Collections;

public class BaseballUIManager : MonoBehaviour
{
    public static BaseballUIManager Instance { get; private set; }


    [Header("<< Character Database >>")]
    [SerializeField] private CharacterDatabase characterDatabase;


    [Header("<< Profile UI >>")]
    [SerializeField] private BaseballProfileUI[] profileUIs;


    [Header("<< Refresh >>")]
    [SerializeField] private float refreshInterval = 0.1f;


    private Coroutine refreshCoroutine;


    // =========================================================
    // Instance
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        refreshCoroutine =
            StartCoroutine(
                RefreshProfilesRoutine()
            );
    }


    // =========================================================
    // Refresh Routine
    // =========================================================

    private IEnumerator RefreshProfilesRoutine()
    {
        while (true)
        {
            NetworkRunner runner =
                FindFirstObjectByType<NetworkRunner>();


            if (runner != null)
            {
                RefreshProfiles(runner);
            }


            yield return new WaitForSeconds(
                refreshInterval
            );
        }
    }


    // =========================================================
    // Refresh Profiles
    // =========================================================

    public void RefreshProfiles(
        NetworkRunner runner
    )
    {
        if (runner == null)
            return;

        if (characterDatabase == null)
            return;

        if (
            characterDatabase.characters == null ||
            characterDatabase.characters.Length == 0
        )
        {
            return;
        }


        PlayerNetwork[] players =
            FindObjectsByType<PlayerNetwork>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );


        // PlayerRef 순서대로 정렬
        System.Array.Sort(
            players,
            (a, b) =>
                a.PlayerRef.RawEncoded.CompareTo(
                    b.PlayerRef.RawEncoded
                )
        );


        // =====================================================
        // Profile UI
        // =====================================================

        for (
            int i = 0;
            i < profileUIs.Length;
            i++
        )
        {
            if (i >= players.Length)
            {
                profileUIs[i].gameObject.SetActive(false);
                continue;
            }


            PlayerNetwork player =
                players[i];


            if (player == null)
            {
                profileUIs[i].gameObject.SetActive(false);
                continue;
            }


            if (
                player.CharacterIndex < 0 ||
                player.CharacterIndex >=
                characterDatabase.characters.Length
            )
            {
                profileUIs[i].gameObject.SetActive(false);
                continue;
            }


            CharacterData characterData =
                characterDatabase.characters[
                    player.CharacterIndex
                ];


            profileUIs[i].gameObject.SetActive(true);


            // =================================================
            // 여기서 BaseballCount를 계속 읽는다.
            // =================================================

            profileUIs[i].Show(
                player.Nickname.ToString(),
                characterData.characterColor,
                player.BaseballCount
            );
        }
    }
}