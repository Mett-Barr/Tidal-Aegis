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
    }
}
