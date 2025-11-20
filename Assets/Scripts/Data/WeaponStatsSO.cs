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

    [CreateAssetMenu(fileName = "NewWeaponStats", menuName = "NavalCommand/WeaponStats")]
    public class WeaponStatsSO : ScriptableObject
    {
        [Header("Display Info")]
        [Tooltip("The name shown in UI [Traditional Chinese]")]
        public string DisplayName;

        [Header("Combat Stats")]
        public WeaponType Type;
        public float Range;
        public float Cooldown;
        public float Damage;
        public float ProjectileSpeed = 20f;

        [Header("Visuals & Physics")]
        public GameObject ProjectilePrefab;
        public LayerMask TargetMask;
    }
}
