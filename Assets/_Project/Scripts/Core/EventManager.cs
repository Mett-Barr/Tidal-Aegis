using System;
using UnityEngine;

namespace NavalCommand.Core
{
    public static class EventManager
    {
        // Gameplay Events
        public static event Action<int> OnScoreChanged;
        public static event Action<float, float> OnFlagshipHPUpdate; // Current, Max
        public static event Action<int, float> OnXPGained; // Level, Progress%
        public static event Action OnLevelUpAvailable;
        
        // System Events
        public static event Action<bool> OnPauseToggled;

        // Triggers
        public static void TriggerScoreChanged(int score) => OnScoreChanged?.Invoke(score);
        public static void TriggerFlagshipHPUpdate(float current, float max) => OnFlagshipHPUpdate?.Invoke(current, max);
        public static void TriggerXPGained(int level, float progress) => OnXPGained?.Invoke(level, progress);
        public static void TriggerLevelUpAvailable() => OnLevelUpAvailable?.Invoke();
        public static void TriggerPauseToggled(bool isPaused) => OnPauseToggled?.Invoke(isPaused);
    }
}
