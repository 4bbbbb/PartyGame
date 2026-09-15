using Fusion;
using UnityEngine;

public class BaseballUIManager : MonoBehaviour
{
    public static BaseballUIManager Instance { get; private set; }

    [Header("<< Character Database >>")]
    [SerializeField] private CharacterDatabase characterDatabase;

    [Header("<< Profile UI >>")]
    [SerializeField] private BaseballProfileUI[] profileUIs;

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

    private void Start()
    {
        RefreshProfilesWhenReady();
    }

    private void RefreshProfilesWhenReady()
    {
        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();

        if (runner == null)
        {
            Invoke(nameof(RefreshProfilesWhenReady), 0.2f);
            return;
        }

        PlayerNetwork[] players =
            FindObjectsByType<PlayerNetwork>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        if (players.Length == 0)
        {
            Invoke(nameof(RefreshProfilesWhenReady), 0.2f);
            return;
        }

        RefreshProfiles(runner);
    }

    public void RefreshProfiles(NetworkRunner runner)
    {
        if (runner == null)
            return;

        PlayerNetwork[] players =
            FindObjectsByType<PlayerNetwork>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        System.Array.Sort(
            players,
            (a, b) => a.PlayerRef.RawEncoded.CompareTo(b.PlayerRef.RawEncoded)
        );

        for (int i = 0; i < profileUIs.Length; i++)
        {
            if (i >= players.Length)
            {
                profileUIs[i].gameObject.SetActive(false);
                continue;
            }

            PlayerNetwork player = players[i];

            if (player.CharacterIndex < 0 ||
                player.CharacterIndex >= characterDatabase.characters.Length)
            {
                profileUIs[i].gameObject.SetActive(false);
                continue;
            }

            CharacterData characterData =
                characterDatabase.characters[player.CharacterIndex];

            profileUIs[i].gameObject.SetActive(true);

            profileUIs[i].Show(
                player.Nickname.ToString(),
                characterData.characterColor,
                player.BaseballCount
            );
        }
    }
}