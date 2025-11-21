using UnityEngine;
using NavalCommand.Core;
using System.Collections.Generic;

namespace NavalCommand.Utils
{
    public class DebugSpawner : MonoBehaviour
    {
        [Header("Test Units")]
        public List<GameObject> UnitPrefabs;
        public float SpawnRadius = 50f;

        private void Start()
        {
            SpawnTestUnits();
        }

        [ContextMenu("Spawn Test Units")]
        public void SpawnTestUnits()
        {
            if (GameManager.Instance == null || GameManager.Instance.PlayerFlagship == null)
            {
                Debug.LogWarning("GameManager or PlayerFlagship not found!");
                return;
            }

            Vector3 center = GameManager.Instance.PlayerFlagship.transform.position;
            float angleStep = 360f / UnitPrefabs.Count;

            for (int i = 0; i < UnitPrefabs.Count; i++)
            {
                if (UnitPrefabs[i] == null) continue;

                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * SpawnRadius;
                Vector3 spawnPos = center + offset;

                Instantiate(UnitPrefabs[i], spawnPos, Quaternion.LookRotation(center - spawnPos));
            }
        }
    }
}
