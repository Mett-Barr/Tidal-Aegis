using UnityEngine;

namespace NavalCommand.Data
{
    public enum WeaponType
    {
        FlagshipGun,
        Autocannon,
        CIWS,
        LaserCIWS,  // NEW: Laser-based point defense (Role=PointDefense, Kinematics=Linear+Gravity0, Payload=Energy)
        Missile,
        Torpedo
    }

    [System.Flags]
    public enum TargetCapability
    {
        None = 0,
        Ship = 1,
        Aircraft = 2,
        Missile = 4,
        Torpedo = 8,
        Shell = 16
    }

    [CreateAssetMenu(fileName = "NewWeaponStats", menuName = "NavalCommand/WeaponStats")]
    public class WeaponStatsSO : ScriptableObject
    {
        [Header("Display Info")]
        [Tooltip("The name shown in UI [Traditional Chinese]")]
        public string DisplayName;

        [Header("Basic")]
        public WeaponType Type;
        public FiringMode Mode = FiringMode.Projectile;  // NEW: Projectile vs Beam
        public TargetCapability TargetType = TargetCapability.Ship;
        public NavalCommand.Systems.VFX.ImpactProfile ImpactProfile;

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
        // Rotation Acceleration
        // ---------------------------------------------------------------------
        [SerializeField] private float _baseRotationAcceleration = 1000f; // Default high for instant
        [SerializeField] private float _overrideRotationAcceleration;
        [SerializeField] private bool _useOverrideRotationAcceleration;
        public float RotationAcceleration => _useOverrideRotationAcceleration ? _overrideRotationAcceleration : _baseRotationAcceleration;
        public void SetBaseRotationAcceleration(float val) => _baseRotationAcceleration = val;

        // ---------------------------------------------------------------------
        // Spread
        // ---------------------------------------------------------------------
        [SerializeField] private float _baseSpread = 0f;
        [SerializeField] private float _overrideSpread;
        [SerializeField] private bool _useOverrideSpread;
        public float Spread => _useOverrideSpread ? _overrideSpread : _baseSpread;
        public void SetBaseSpread(float val) => _baseSpread = val;

        // ---------------------------------------------------------------------
        // Firing Angle Tolerance (Degrees)
        // ---------------------------------------------------------------------
        [SerializeField] private float _baseFiringAngleTolerance = 5f;
        [SerializeField] private float _overrideFiringAngleTolerance;
        [SerializeField] private bool _useOverrideFiringAngleTolerance;
        public float FiringAngleTolerance => _useOverrideFiringAngleTolerance ? _overrideFiringAngleTolerance : _baseFiringAngleTolerance;
        public void SetBaseFiringAngleTolerance(float val) => _baseFiringAngleTolerance = val;

        // ---------------------------------------------------------------------
        // Platform Capabilities
        // ---------------------------------------------------------------------
        [SerializeField] private bool _baseCanRotate = true;
        public bool CanRotate => _baseCanRotate;
        public void SetBaseCanRotate(bool val) => _baseCanRotate = val;

        [SerializeField] private bool _baseIsVLS = false;
        public bool IsVLS => _baseIsVLS;
        public void SetBaseIsVLS(bool val) => _baseIsVLS = val;

        [SerializeField] private string _baseAimingLogicName = "Ballistic";
        public string AimingLogicName => _baseAimingLogicName;
        public void SetBaseAimingLogicName(string val) => _baseAimingLogicName = val;

        [Header("Projectile")]
        public GameObject ProjectilePrefab;
        public Color ProjectileColor = Color.white;  // NEW: For beam weapons and projectile tinting
        public GameObject MuzzleFlashPrefab; // New field for Muzzle Flash
        public LayerMask TargetMask;

        // Editor Helper to validate/reset overrides if needed
        private void OnValidate()
        {
            // Optional: Ensure overrides are reasonable?
        }
    }
}
