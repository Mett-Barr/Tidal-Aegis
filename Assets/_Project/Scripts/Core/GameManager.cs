using UnityEngine;
using NavalCommand.Entities.Units;
using NavalCommand.Infrastructure;

namespace NavalCommand.Core
{
    public enum GameState
    {
        Playing,
        Paused,
        GameOver
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        public GameState CurrentState;
        public FlagshipController PlayerFlagship;
        public GameObject PlayerPrefab; // Added to support spawning specific prefab

        private int currentScore = 0;

        private void Awake()
        {
            // Ensure Systems exist
            if (FindObjectOfType<Systems.WorldPhysicsSystem>() == null)
            {
                var go = new GameObject("WorldPhysicsSystem");
                go.AddComponent<Systems.WorldPhysicsSystem>();
            }

            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // 1. Check if PlayerFlagship is assigned but is a Prefab (not in scene)
            if (PlayerFlagship != null && PlayerFlagship.gameObject.scene.rootCount == 0)
            {
                Debug.LogWarning("[GameManager] PlayerFlagship references a Prefab asset. Instantiating it.");
                GameObject playerObj = Instantiate(PlayerFlagship.gameObject, Vector3.zero, Quaternion.identity);
                PlayerFlagship = playerObj.GetComponent<FlagshipController>();
            }
            else if (PlayerFlagship != null)
            {
                 Debug.Log($"[GameManager] PlayerFlagship already assigned to: {PlayerFlagship.name}");
            }

            // 2. If null, try to find in scene
            if (PlayerFlagship == null)
            {
                PlayerFlagship = FindObjectOfType<FlagshipController>();
                if (PlayerFlagship != null) Debug.Log($"[GameManager] Found existing Flagship in scene: {PlayerFlagship.name}");
            }

            // 3. If still null, and we have a specific PlayerPrefab, spawn it
            if (PlayerFlagship == null && PlayerPrefab != null)
            {
                Debug.Log($"[GameManager] Spawning PlayerPrefab: {PlayerPrefab.name}");
                GameObject playerObj = Instantiate(PlayerPrefab, Vector3.zero, Quaternion.identity);
                PlayerFlagship = playerObj.GetComponent<FlagshipController>();
                Debug.Log("[GameManager] Spawned Player Flagship from PlayerPrefab.");
            }
            else if (PlayerFlagship == null)
            {
                Debug.LogError("[GameManager] PlayerFlagship is NULL and no PlayerPrefab assigned! Ship will NOT spawn.");
            }

            if (InputReader.Instance != null)
            {
                InputReader.Instance.OnPausePressed += TogglePause;
            }
        }

        private void OnDestroy()
        {
            if (InputReader.Instance != null)
            {
                InputReader.Instance.OnPausePressed -= TogglePause;
            }
        }

        public void RegisterUnit(MonoBehaviour unit)
        {
            // TODO: Add to spatial hash or list
        }

        public void UnregisterUnit(MonoBehaviour unit)
        {
            // TODO: Remove from lists
        }

        public void AddScore(int amount)
        {
            currentScore += amount;
            EventManager.TriggerScoreChanged(currentScore);
        }

        public void TogglePause()
        {
            if (CurrentState == GameState.Playing)
            {
                Debug.Log("[GameManager] Game Paused via TogglePause.");
                CurrentState = GameState.Paused;
                Time.timeScale = 0f;
                EventManager.TriggerPauseToggled(true);
            }
            else if (CurrentState == GameState.Paused)
            {
                Debug.Log("[GameManager] Game Resumed via TogglePause.");
                CurrentState = GameState.Playing;
                Time.timeScale = 1f;
                EventManager.TriggerPauseToggled(false);
            }
        }
    }
}
