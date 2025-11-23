using UnityEngine;

namespace NavalCommand.Data
{
    [CreateAssetMenu(fileName = "PhysicsConfig", menuName = "NavalCommand/PhysicsConfig")]
    public class PhysicsConfigSO : ScriptableObject
    {
        [Header("Global Scaling")]
        [Tooltip("Scale factor for all projectile speeds (0.05 = 5% of real speed)")]
        public float GlobalSpeedScale = 0.05f;

        [Tooltip("Scale factor for all weapon ranges (0.1 = 10% of real range)")]
        public float GlobalRangeScale = 1f;

        [Header("Physics Constants")]
        public float StandardGravity = 9.81f;

        [ContextMenu("Calculate Realistic Gravity")]
        public void CalculateGravity()
        {
            // Formula: g' = g * (S^2 / R)
            // S = SpeedScale, R = RangeScale
            // Real Gravity approx 9.81
            
            if (GlobalRangeScale <= 0.001f) return;

            float realGravity = 9.81f;
            float factor = (GlobalSpeedScale * GlobalSpeedScale) / GlobalRangeScale;
            
            StandardGravity = realGravity * factor;
            
            Debug.Log($"[PhysicsConfig] Calculated Gravity: {StandardGravity} (based on SpeedScale {GlobalSpeedScale} and RangeScale {GlobalRangeScale})");
        }
    }
}
