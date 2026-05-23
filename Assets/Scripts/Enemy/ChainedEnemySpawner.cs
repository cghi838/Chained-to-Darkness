using UnityEngine;

public class ChainedEnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Spawner Settings")]
    [SerializeField] private bool spawnOnlyOnce = true;
    [SerializeField] private bool destroySpawnerAfterSpawn = false;

    private bool hasSpawned;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (spawnOnlyOnce && hasSpawned)
            return;

        SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("ChainedEnemySpawner: No enemyPrefab assigned.");
            return;
        }

        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        hasSpawned = true;

        if (destroySpawnerAfterSpawn)
            Destroy(gameObject);
    }
}