using UnityEngine;
using NavalCommand.Core;

namespace NavalCommand.Systems
{
    public class SpawningSystem : MonoBehaviour
    {
        [Header("Spawn Settings")]
        public GameObject EnemyPrefab;
        public float SpawnRadius = 50f;
        public float SpawnInterval = 5f;

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
            if (EnemyPrefab == null) return;

            // Calculate random position on circle
            Vector2 randomCircle = Random.insideUnitCircle.normalized * SpawnRadius;
            Vector3 spawnPos = new Vector3(randomCircle.x, 0, randomCircle.y);

            // Offset by player position if available
            if (GameManager.Instance.PlayerFlagship != null)
            {
                spawnPos += GameManager.Instance.PlayerFlagship.transform.position;
            }

            Instantiate(EnemyPrefab, spawnPos, Quaternion.identity);
        }
    }
}
