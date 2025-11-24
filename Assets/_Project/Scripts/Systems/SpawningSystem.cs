using UnityEngine;
using NavalCommand.Core;
using NavalCommand.Infrastructure;

namespace NavalCommand.Systems
{
    public enum SpawnMode
    {
        Random,
        Specific
    }

    public class SpawningSystem : MonoBehaviour
    {
        [Header("Spawn Settings")]
        public GameObject[] EnemyPrefabs; // Array of possible enemies
        public float SpawnRadius = 4000f;
        public float SpawnInterval = 2f;
        public int MaxEnemies = 10; // Limit active enemies

        [Header("Debug Settings")]
        public SpawnMode Mode = SpawnMode.Random; // Default to Random for variety
        public int SpecificEnemyIndex = 0;
        public string SpecificPrefabName = ""; // Clear default to avoid overriding Random if Mode is accidentally Specific

        private float spawnTimer;
        private System.Collections.Generic.List<GameObject> activeEnemies = new System.Collections.Generic.List<GameObject>();

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

            // Cleanup nulls (destroyed enemies)
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] == null)
                {
                    activeEnemies.RemoveAt(i);
                }
            }

            if (activeEnemies.Count >= MaxEnemies) return;

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

            // Select prefab based on mode
            GameObject prefab = null;
            if (Mode == SpawnMode.Specific)
            {
                // Try to find by name first
                if (!string.IsNullOrEmpty(SpecificPrefabName))
                {
                    foreach (var p in EnemyPrefabs)
                    {
                        if (p != null && p.name == SpecificPrefabName)
                        {
                            prefab = p;
                            break;
                        }
                    }
                    
                    if (prefab == null) Debug.LogWarning($"[SpawningSystem] Could not find prefab with name '{SpecificPrefabName}' in list of {EnemyPrefabs.Length} items.");
                }

                // Fallback to index if name not found
                if (prefab == null)
                {
                    int index = Mathf.Clamp(SpecificEnemyIndex, 0, EnemyPrefabs.Length - 1);
                    if (EnemyPrefabs.Length > 0)
                    {
                        prefab = EnemyPrefabs[index];
                        Debug.LogWarning($"[SpawningSystem] Fallback to index {index}: {prefab?.name}");
                    }
                    else
                    {
                        Debug.LogError("[SpawningSystem] EnemyPrefabs list is empty!");
                        return;
                    }
                }
            }
            else
            {
                if (EnemyPrefabs.Length > 0)
                    prefab = EnemyPrefabs[Random.Range(0, EnemyPrefabs.Length)];
            }

            if (prefab != null)
            {
                Debug.Log($"[SpawningSystem] Spawning Enemy: {prefab.name} (Mode: {Mode})");
                GameObject enemyObj = Instantiate(prefab, spawnPos, Quaternion.LookRotation(-spawnPos.normalized));
                activeEnemies.Add(enemyObj);
                
                // Force Team Assignment for Spawned Enemies
                var unit = enemyObj.GetComponent<NavalCommand.Entities.Units.BaseUnit>();
                if (unit != null)
                {
                    unit.UnitTeam = Team.Enemy;
                    
                    // Also update all child WeaponControllers
                    var weapons = enemyObj.GetComponentsInChildren<NavalCommand.Entities.Components.WeaponController>();
                    foreach (var wc in weapons)
                    {
                        wc.OwnerTeam = Team.Enemy;
                    }
                }
            }
        }
    }
}
