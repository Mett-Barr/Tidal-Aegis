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

        // ---------------------------------------------------------------------
        // Range
        // ---------------------------------------------------------------------
        [SerializeField] private float _baseRange;
        [SerializeField] private float _overrideRange;
        [SerializeField] private bool _useOverrideRange;
        public float Range => _useOverrideRange ? _overrideRange : _baseRange;
        public void SetBaseRange(float val) => _baseRange = val;

        // ---------------------------------------------------------------------
        // Cooldown
        // ---------------------------------------------------------------------
        [SerializeField] private float _baseCooldown;
        [SerializeField] private float _overrideCooldown;
        [SerializeField] private bool _useOverrideCooldown;
        public float Cooldown => _useOverrideCooldown ? _overrideCooldown : _baseCooldown;
        public void SetBaseCooldown(float val) => _baseCooldown = val;

        // ---------------------------------------------------------------------
        // Damage
        // ---------------------------------------------------------------------
        [SerializeField] private float _baseDamage;
        [SerializeField] private float _overrideDamage;
        [SerializeField] private bool _useOverrideDamage;
        public float Damage => _useOverrideDamage ? _overrideDamage : _baseDamage;
        public void SetBaseDamage(float val) => _baseDamage = val;

        // ---------------------------------------------------------------------
        // Projectile Speed
        // ---------------------------------------------------------------------
        [SerializeField] private float _baseProjectileSpeed = 20f;
        [SerializeField] private float _overrideProjectileSpeed;
        [SerializeField] private bool _useOverrideProjectileSpeed;
        public float ProjectileSpeed => _useOverrideProjectileSpeed ? _overrideProjectileSpeed : _baseProjectileSpeed;
        public void SetBaseProjectileSpeed(float val) => _baseProjectileSpeed = val;

        // ---------------------------------------------------------------------
        // Gravity Multiplier
        // ---------------------------------------------------------------------
        [SerializeField] private float _baseGravityMultiplier = 1f;
        [SerializeField] private float _overrideGravityMultiplier;
        [SerializeField] private bool _useOverrideGravityMultiplier;
        public float GravityMultiplier => _useOverrideGravityMultiplier ? _overrideGravityMultiplier : _baseGravityMultiplier;
        public void SetBaseGravityMultiplier(float val) => _baseGravityMultiplier = val;

        // ---------------------------------------------------------------------
        // Rotation Speed
        // ---------------------------------------------------------------------
        [SerializeField] private float _baseRotationSpeed = 30f;
        [SerializeField] private float _overrideRotationSpeed;
        [SerializeField] private bool _useOverrideRotationSpeed;
        public float RotationSpeed => _useOverrideRotationSpeed ? _overrideRotationSpeed : _baseRotationSpeed;
        public void SetBaseRotationSpeed(float val) => _baseRotationSpeed = val;

        // ---------------------------------------------------------------------
        // Spread
        // ---------------------------------------------------------------------
        [SerializeField] private float _baseSpread = 0f;
        [SerializeField] private float _overrideSpread;
        [SerializeField] private bool _useOverrideSpread;
        public float Spread => _useOverrideSpread ? _overrideSpread : _baseSpread;
        public void SetBaseSpread(float val) => _baseSpread = val;

        [Header("Visuals & Physics")]
        public GameObject ProjectilePrefab;
        public LayerMask TargetMask;

        // Editor Helper to validate/reset overrides if needed
        private void OnValidate()
        {
            // Optional: Ensure overrides are reasonable?
        }
    }
}
