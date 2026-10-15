using UnityEngine;

public class DodgeItManager : MonoBehaviour
{
    [Header("<< Alien >>")]
    [SerializeField] private GameObject alienPrefab;

    [Header("<< Alien Spawn Point >>")]
    [SerializeField] private Transform alienSpawnPoint;




    private void Start()
    {
        SpawnAlien();
    }

    private void SpawnAlien()
    {
        if (alienPrefab == null || alienSpawnPoint == null)
        {
            Debug.LogError("[DodgeItManager] Alien Prefab 또는 SpawnPoint가 연결되지 않았습니다.");
            return;
        }

        Instantiate(
            alienPrefab,
            alienSpawnPoint.position,
            alienSpawnPoint.rotation
        );
    }
}