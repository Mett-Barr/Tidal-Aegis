using UnityEngine;
using NavalCommand.Core;
using NavalCommand.Infrastructure;

namespace NavalCommand.Systems
{
    public class SpawningSystem : MonoBehaviour
    {
        [Header("Spawn Settings")]
        public GameObject[] EnemyPrefabs; // Array of possible enemies
        public float SpawnRadius = 4000f;
        public float SpawnInterval = 2f;

        private float spawnTimer;

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

            spawnTimer += Time.deltaTime;
            if (spawnTimer >= SpawnInterval)
            {
                SpawnEnemy();
                spawnTimer = 0f;
            }
        }

        private void SpawnEnemy()
        {
            if (EnemyPrefabs == null || EnemyPrefabs.Length == 0) return;

            // Calculate random position on circle
            Vector2 randomCircle = Random.insideUnitCircle.normalized * SpawnRadius;
            Vector3 spawnPos = new Vector3(randomCircle.x, 0, randomCircle.y);

            // Offset by player position if available
            if (GameManager.Instance.PlayerFlagship != null)
            {
                spawnPos += GameManager.Instance.PlayerFlagship.transform.position;
            }

            // Randomly select prefab
            GameObject prefab = EnemyPrefabs[Random.Range(0, EnemyPrefabs.Length)];
            if (prefab != null)
            {
                Instantiate(prefab, spawnPos, Quaternion.LookRotation(-spawnPos.normalized));
            }
        }
    }
}
