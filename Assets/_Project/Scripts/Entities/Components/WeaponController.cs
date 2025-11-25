using UnityEngine;
using NavalCommand.Data;
using NavalCommand.Core;
using NavalCommand.Systems;
using NavalCommand.Infrastructure;
using NavalCommand.Entities.Projectiles;
using NavalCommand.Entities.Units;

namespace NavalCommand.Entities.Components
{
    public class WeaponController : MonoBehaviour
    {
        [Header("Configuration")]
        public WeaponStatsSO WeaponStats;
        public Team OwnerTeam = Team.Player;

        [Header("Components")]
        public Transform FirePoint;
        private TurretRotator _rotator;
        
        [Header("State")]
        public bool IsWeaponEnabled = true;

        // State
        private float cooldownTimer;
        private Transform currentTarget;
        private bool isFiring;

        private void Awake()
        {
            // Ensure TurretRotator exists
            _rotator = GetComponent<TurretRotator>();
            if (_rotator == null)
            {
                _rotator = gameObject.AddComponent<TurretRotator>();
            }
        }

        private void Start()
        {
            if (WeaponStats == null)
            {
                Debug.LogWarning($"WeaponController on {gameObject.name} has no WeaponStats assigned! Turret will be inactive.");
            }

            // Initialize Rotator
            if (_rotator != null && FirePoint != null)
            {
                _rotator.Initialize(transform, FirePoint);
            }

            // Sync Team from Parent Unit if possible
            var parentUnit = GetComponentInParent<BaseUnit>();
            if (parentUnit != null)
            {
                OwnerTeam = parentUnit.UnitTeam;
            }
            
            // Randomize initial cooldown to prevent synchronized firing
            if (WeaponStats != null)
            {
                cooldownTimer = Random.Range(0f, WeaponStats.Cooldown);
            }
        }

        private void Update()
        {
            if (!IsWeaponEnabled || WeaponStats == null || FirePoint == null) return;

            // 0. Find Target
            FindTarget();

            // 1. Calculate Fire Solution (FCS Negotiation)
            Vector3? desiredAimVector = CalculateFireSolution();

            // 2. Aim & Fire Logic
            if (desiredAimVector.HasValue)
            {
                // A. Aim (Delegate to Rotator)
                // The Rotator component knows if it can rotate or not (e.g. VLS vs Turret)
                float rotSpeed = WeaponStats.RotationSpeed;
                
                // Legacy fallback: If stats are default (30), boost them for CIWS
                if (WeaponStats.Type == WeaponType.CIWS && rotSpeed < 100f) rotSpeed = 600f;
                else if (rotSpeed < 50f) rotSpeed = 300f;

                Vector3 targetPos = FirePoint.position + desiredAimVector.Value;
                _rotator.AimAt(targetPos, rotSpeed);

                // B. Check Alignment (Delegate to Rotator)
                // The Rotator component knows if it needs alignment (e.g. VLS always returns true)
                float tolerance = WeaponStats.FiringAngleTolerance;
                if (tolerance < 0.1f) tolerance = 5f;

                if (_rotator.IsAligned(targetPos, tolerance))
                {
                    isFiring = true;
                }
                else
                {
                    isFiring = false;
                }
            }
            else
            {
                isFiring = false;
            }

            // 3. Firing Execution
            if (isFiring)
            {
                // Handle High ROF (Multiple shots per frame)
                while (cooldownTimer <= 0)
                {
                    if (currentTarget != null)
                    {
                        Fire(currentTarget.GetComponent<IDamageable>());
                    }
                    else
                    {
                        isFiring = false;
                        break;
                    }

                    cooldownTimer += WeaponStats.Cooldown; // Accumulate debt
                    
                    // Safety break for extremely low cooldowns to prevent freeze
                    if (WeaponStats.Cooldown < 0.001f) 
                    {
                        cooldownTimer = 0;
                        break;
                    }
                }
            }

            cooldownTimer -= Time.deltaTime;
        }

        private void FindTarget()
        {
            if (WeaponStats == null || SpatialGridSystem.Instance == null || WorldPhysicsSystem.Instance == null) return;

            // Get Scaled Range from System
            float scaledRange = WorldPhysicsSystem.Instance.GetScaledRange(WeaponStats.Range);

            // Determine target team
            Team targetTeam = (OwnerTeam == Team.Player) ? Team.Enemy : Team.Player;

            // Query Spatial Grid directly for closest target (Optimized)
            Transform closestTarget = SpatialGridSystem.Instance.GetClosestTarget(transform.position, scaledRange, targetTeam, WeaponStats.TargetType);
            
            currentTarget = closestTarget;
        }

        private Vector3? CalculateFireSolution()
        {
            if (currentTarget == null || WeaponStats == null) return null;

            Vector3 myPos = FirePoint.position;
            
            // ---------------------------------------------------------------------
            // 1. AMMO LAYER: Ask the Ammo for its Ideal Trajectory (The "Wish")
            // ---------------------------------------------------------------------
            // Select Predictor (Standard FCS component)
            ITargetPredictor predictor = GetPredictorForTarget();

            // Resolve Ammo's Strategy (Higher-Order Function)
            var aimingLogic = NavalCommand.Systems.Aiming.AimingFunctions.Resolve(WeaponStats.AimingLogicName);
            Vector3? idealVector = aimingLogic(myPos, WeaponStats, currentTarget, predictor);

            if (!idealVector.HasValue) return null;

            // ---------------------------------------------------------------------
            // 2. PLATFORM LAYER: Apply Physical Constraints (The "Reality")
            // ---------------------------------------------------------------------
            if (WeaponStats.IsVLS)
            {
                // CONSTRAINT: VLS Platform cannot rotate to aim. It must fire UP.
                // The "Ideal Vector" is ignored for the *Launch*, but the Ammo (Missile) 
                // accepts this because it has guidance.
                
                // We return UP as the required platform orientation (relative to ship, but usually global UP for VLS)
                // Actually, VLS cells are fixed relative to the ship. 
                // If the ship rolls, VLS points sideways. 
                // For this simplified model, we assume VLS points along the FirePoint's local Y or Z.
                // But _rotator usually rotates the whole object.
                
                // If IsVLS, we simply DO NOT ROTATE. The FirePoint is already fixed.
                // We return the FirePoint's current forward as the "Solution" to allow firing.
                return FirePoint.forward * WeaponStats.ProjectileSpeed; 
            }
            else if (!WeaponStats.CanRotate)
            {
                // CONSTRAINT: Fixed Mount (e.g. Spinal Mount).
                // We cannot rotate. We can only fire if the target happens to be in front.
                // Ideally, the Ship Controller would steer the ship.
                // For the WeaponController, we just check if we are aligned.
                return idealVector;
            }
            else
            {
                // CONSTRAINT: Turret. We can rotate to match the Ideal Vector.
                return idealVector;
            }
        }

        private ITargetPredictor GetPredictorForTarget()
        {
            Vector3 targetVel = Vector3.zero;
            Vector3 targetAccel = Vector3.zero;
            Rigidbody targetRb = currentTarget.GetComponent<Rigidbody>();
            if (targetRb != null) targetVel = targetRb.velocity;

            var predictionProvider = currentTarget.GetComponent<IPredictionProvider>();
            if (predictionProvider != null)
            {
                Vector3 myVel = GetComponentInParent<Rigidbody>()?.velocity ?? Vector3.zero;
                return predictionProvider.GetPredictor(FirePoint.position, myVel);
            }
            else if (targetAccel.sqrMagnitude > 0.1f)
            {
                return new QuadraticTargetPredictor(currentTarget.position, targetVel, targetAccel);
            }
            else
            {
                return new LinearTargetPredictor(currentTarget.position, targetVel);
            }
        }

        public void Fire(IDamageable target)
        {
            if (WeaponStats == null || WeaponStats.ProjectilePrefab == null || PoolManager.Instance == null || WorldPhysicsSystem.Instance == null) 
            {
                return;
            }

            // Calculate Spread
            Quaternion fireRotation = FirePoint.rotation;
            if (WeaponStats.Spread > 0.01f)
            {
                float xSpread = Random.Range(-WeaponStats.Spread, WeaponStats.Spread);
                float ySpread = Random.Range(-WeaponStats.Spread, WeaponStats.Spread);
                fireRotation = Quaternion.Euler(fireRotation.eulerAngles.x + xSpread, fireRotation.eulerAngles.y + ySpread, fireRotation.eulerAngles.z);
            }

            // Use PoolManager
            GameObject projectileObj = PoolManager.Instance.Spawn(WeaponStats.ProjectilePrefab, FirePoint.position, fireRotation);
            
            // Spawn Muzzle Flash via VFXManager
            if (NavalCommand.Systems.VFX.VFXManager.Instance != null)
            {
                Debug.Log($"[VFX_DEBUG] Weapon firing: Spawning muzzle flash at {FirePoint.position}");
                var flash = NavalCommand.Systems.VFX.VFXManager.Instance.SpawnMuzzleFlashVFX(FirePoint.position, FirePoint.rotation);
                
                if (flash == null)
                {
                    Debug.LogError("[VFX_DEBUG] ERROR: Failed to spawn muzzle flash!");
                }
            }
            else
            {
                Debug.LogError("[VFX_DEBUG] ERROR: VFXManager.Instance is null!");
            }
            
            if (projectileObj == null) return;

            ProjectileBehavior projectile = projectileObj.GetComponent<ProjectileBehavior>();
            if (projectile != null)
            {
                // Initialize properly
                GameObject ownerObj = gameObject;
                var parentUnit = GetComponentInParent<BaseUnit>();
                if (parentUnit != null) ownerObj = parentUnit.gameObject;
                
                // Get Scaled Physics Values
                float scaledSpeed = WorldPhysicsSystem.Instance.GetScaledSpeed(WeaponStats.ProjectileSpeed);
                float scaledRange = WorldPhysicsSystem.Instance.GetScaledRange(WeaponStats.Range);
                
                // Calculate Ballistic Gravity
        float gravityY = -WorldPhysicsSystem.Instance.GetBallisticGravity(scaledSpeed, scaledRange);
        
        // Apply Multiplier
        if (WeaponStats.GravityMultiplier < 0.01f)
        {
            gravityY = 0f;
        }
        else
        {
            gravityY *= WeaponStats.GravityMultiplier;
        }

        Vector3 initialVelocity = fireRotation * Vector3.forward * scaledSpeed;

                projectile.Owner = ownerObj;
                projectile.Target = ((MonoBehaviour)target).transform;
                projectile.Damage = WeaponStats.Damage;
                projectile.Speed = scaledSpeed;
                projectile.ImpactProfile = WeaponStats.ImpactProfile;

                // Initialize with Velocity, Type, AND Custom Gravity
                projectile.Initialize(initialVelocity, OwnerTeam, WeaponStats.Type, gravityY);
            }
            
            cooldownTimer = WeaponStats.Cooldown;
        }
    }
}
