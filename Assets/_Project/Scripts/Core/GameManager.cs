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

        private int currentScore = 0;

        private void Awake()
        {
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
