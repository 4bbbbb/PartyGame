using Fusion;
using UnityEngine;

public class DodgeItManager : MonoBehaviour
{
    [Header("<< Alien >>")]
    [SerializeField] private GameObject alienPrefab;    
    [SerializeField] private Transform alienSpawnPoint;

    [Header("<< DodgeIt Player >>")]
    [SerializeField] private GameObject dodgeItPlayerPrefab;
    [SerializeField] private Transform[] playerSpawnPoints;


    private void Start()
    {
        SpawnAlien();
        SpawnPlayers();
    }

    private void SpawnAlien()
    {
        if (alienPrefab == null || alienSpawnPoint == null)        
            return;
        
        Instantiate(
            alienPrefab,
            alienSpawnPoint.position,
            alienSpawnPoint.rotation
        );
    }

    private void SpawnPlayers()
    {
        if (dodgeItPlayerPrefab == null || playerSpawnPoints == null)
            return;

        foreach (Transform spawnPoint in playerSpawnPoints)
        {
            if (spawnPoint == null)
                continue;

            Instantiate(
                dodgeItPlayerPrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );
        }
    }
}