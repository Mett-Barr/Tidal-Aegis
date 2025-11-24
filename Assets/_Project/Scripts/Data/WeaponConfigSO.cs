using UnityEngine;

namespace NavalCommand.Data
{
    [CreateAssetMenu(fileName = "WeaponConfig", menuName = "Naval Command/Weapon Config")]
    public class WeaponConfigSO : ScriptableObject
    {
        [Header("General")]
        public string WeaponName;
        public WeaponType Type;
        public TargetCapability TargetCapability;

        [Header("Firing")]
        public float Range = 5000f;
        public float Cooldown = 1f;
        public float Spread = 0f;

        [Header("Projectile")]
        public GameObject ProjectilePrefab;
        public float ProjectileSpeed = 100f;
        public float RotationSpeed = 0f; // For guided missiles
        
        [Header("Warhead")]
        public WarheadConfigSO WarheadConfig;

        [Header("Launch Configuration")]
        public string MovementLogicName = "Linear"; // "Ballistic", "GuidedMissile", "Torpedo"
        public float CruiseHeight = 0f;
        public float TerminalHomingDistance = 0f;
        public float VerticalLaunchHeight = 0f;
        public float TurnRate = 0f;
        
        [Header("Visuals")]
        public Color ProjectileColor = Color.white;
        public string ProjectileStyle;
    }
}
