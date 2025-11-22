using UnityEngine;

namespace NavalCommand.Data
{
    public enum WeaponType
    {
        FlagshipGun,
        Missile,
        Torpedo,
        Autocannon,
        CIWS
    }

    [System.Flags]
    public enum TargetCapability
    {
        None = 0,
        Surface = 1,
        Air = 2,
        Missile = 4
    }

    [CreateAssetMenu(fileName = "NewWeaponStats", menuName = "NavalCommand/WeaponStats")]
    public class WeaponStatsSO : ScriptableObject
    {
        [Header("Display Info")]
        [Tooltip("The name shown in UI [Traditional Chinese]")]
        public string DisplayName;

        [Header("Combat Stats")]
        public WeaponType Type;
        public TargetCapability TargetType = TargetCapability.Surface;
        public float Range;
        public float Cooldown;
        public float Damage;
        public float ProjectileSpeed = 20f;
        public float GravityMultiplier = 1f;
        public float RotationSpeed = 30f; // Degrees per second

        [Header("Visuals & Physics")]
        public GameObject ProjectilePrefab;
        public LayerMask TargetMask;
    }
}
