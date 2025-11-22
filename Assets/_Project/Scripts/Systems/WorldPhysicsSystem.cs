using UnityEngine;
using NavalCommand.Data;

namespace NavalCommand.Systems
{
    public class WorldPhysicsSystem : MonoBehaviour
    {
        public static WorldPhysicsSystem Instance { get; private set; }

        [Header("Global Scaling")]
        [Tooltip("Scale factor for all projectile speeds (0.05 = 5% of real speed)")]
        public float GlobalSpeedScale = 0.3f;

        [Tooltip("Scale factor for all weapon ranges (0.1 = 10% of real range)")]
        public float GlobalRangeScale = 1f;

        [Header("Physics Constants")]
        public float StandardGravity = 9.81f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Optional: Keep across scenes
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
            
            // We clamp it to be at most StandardGravity to avoid "floaty" physics if speed is high,
            // but usually with 0.05 speed scale, required gravity will be very low.
            // Actually, we should return the EXACT gravity needed for the arc.
            return requiredGravity;
        }
    }
}
