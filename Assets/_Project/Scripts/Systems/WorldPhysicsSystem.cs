using UnityEngine;
using NavalCommand.Data;

namespace NavalCommand.Systems
{
    public class WorldPhysicsSystem : MonoBehaviour
    {
        public static WorldPhysicsSystem Instance { get; private set; }

        [Header("Configuration")]
        public PhysicsConfigSO Config;

        // Fallbacks in case Config is missing
        private float _defaultSpeedScale = 0.05f;
        private float _defaultRangeScale = 1f;
        private float _defaultGravity = 9.81f;

        public float GlobalSpeedScale => Config != null ? Config.GlobalSpeedScale : _defaultSpeedScale;
        public float GlobalRangeScale => Config != null ? Config.GlobalRangeScale : _defaultRangeScale;
        public float StandardGravity => Config != null ? Config.StandardGravity : _defaultGravity;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Optional: Keep across scenes
                
                // Try to load default config if missing
                if (Config == null)
                {
                    Debug.LogWarning("[WorldPhysicsSystem] No PhysicsConfig assigned! Attempting to load default...");
                    // In a real project, you might load from Resources or Addressables.
                    // For now, we rely on the Inspector assignment or fallbacks.
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Converts a real-world reference speed (m/s) to game speed.
        /// </summary>
        public float GetScaledSpeed(float referenceSpeed)
        {
            return referenceSpeed * GlobalSpeedScale;
        }

        /// <summary>
        /// Converts a real-world reference range (m) to game range.
        /// </summary>
        public float GetScaledRange(float referenceRange)
        {
            return referenceRange * GlobalRangeScale;
        }

        /// <summary>
        /// Calculates the gravity required for a ballistic projectile to reach 'scaledRange' 
        /// with an initial 'scaledSpeed' at a 45-degree launch angle (optimal range).
        /// Formula: R = v^2 / g  =>  g = v^2 / R
        /// </summary>
        public float GetBallisticGravity(float scaledSpeed, float scaledRange)
        {
            if (scaledRange <= 0.1f) return StandardGravity;

            // Required gravity to hit max range at 45 degrees
            float requiredGravity = (scaledSpeed * scaledSpeed) / scaledRange;
            
            return requiredGravity;
        }
    }
}
