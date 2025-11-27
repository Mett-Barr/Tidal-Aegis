using UnityEngine;
using System.Collections.Generic;
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
        private NavalCommand.Systems.Weapons.LaserBeamController activeBeam;  // NEW: For beam weapons

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
                // Try to find Turret parts
                Transform turretBase = transform.Find("TurretBase");
                Transform turretGun = turretBase != null ? turretBase.Find("TurretGun") : null;

                // Fallback for simple objects or VLS
                if (turretBase == null) turretBase = transform;
                if (turretGun == null) turretGun = transform;

                _rotator.Initialize(turretBase, turretGun, FirePoint);
                if (WeaponStats != null)
                {
                    _rotator.RotationAcceleration = WeaponStats.RotationAcceleration;
                }
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
                
                // REMOVED Legacy fallback: It was overriding our tuned values (e.g. 15 -> 300)
                // if (WeaponStats.Type == WeaponType.CIWS && rotSpeed < 100f) rotSpeed = 600f;
                // else if (rotSpeed < 50f) rotSpeed = 300f;

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
                    // Debug Log for CIWS/Autocannon
                    if (WeaponStats.Type == WeaponType.CIWS || WeaponStats.Type == WeaponType.Autocannon)
                    {
                        // Only log occasionally to avoid spam
                        if (Time.frameCount % 60 == 0)
                        {
                            Debug.Log($"[{gameObject.name}] Aiming... Not Aligned. Target: {targetPos}");
                        }
                    }
                }
            }
            else
            {
                isFiring = false;
            }

            // 3. Firing Execution
            if (isFiring)
            {
                // BEAM WEAPONS: Different handling from projectile weapons
                if (WeaponStats.Mode == FiringMode.Beam)
                {
                    // Beam weapons fire once and maintain until target is killed/lost
                    // Only fire if cooldown is ready AND beam is not already active
                    if (cooldownTimer <= 0)
                    {
                        if (currentTarget != null)
                        {
                            // Check if beam is already active on this target
                            bool beamAlreadyActive = (activeBeam != null && activeBeam.gameObject.activeSelf);
                            
                            if (!beamAlreadyActive)
                            {
                                Fire(currentTarget.GetComponent<IDamageable>());
                                cooldownTimer = WeaponStats.Cooldown;  // Set cooldown after activation
                            }
                            // If beam is already active, let it continue (it's handling damage in its own Update())
                        }
                        else
                        {
                            isFiring = false;
                        }
                    }
                }
                else
                {
                    // PROJECTILE WEAPONS: Original high ROF handling
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
            Vector3? idealVector = aimingLogic(myPos, FirePoint.forward, WeaponStats, currentTarget, predictor);

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
        
        /// <summary>
        /// Check if turret can effectively track the target to maintain "dwell time" for beam weapons.
        /// Based on real-world laser weapon doctrine: beam must remain on target for sustained damage.
        /// </summary>
        private bool CanEffectivelyTrack(Transform target)
        {
            if (target == null || FirePoint == null) return false;
            
            // Calculate target's angular velocity relative to turret
            Vector3 toTarget = target.position - FirePoint.position;
            float distance = toTarget.magnitude;
            
            if (distance < 0.1f) return true;  // Too close to miss
            
            // Get target's velocity
            Rigidbody targetRb = target.GetComponent<Rigidbody>();
            if (targetRb == null) return true;  // Static target, always trackable
            
            Vector3 targetVelocity = targetRb.velocity;
            
            // Calculate lateral velocity (perpendicular to line of sight)
            Vector3 lateralVelocity = Vector3.ProjectOnPlane(targetVelocity, toTarget.normalized);
            
            // Calculate required angular velocity (rad/s -> deg/s)
            float requiredAngularVel = Mathf.Rad2Deg * (lateralVelocity.magnitude / distance);
            
            // Check if turret can keep up (with 20% safety margin)
            // If target requires faster tracking than turret can provide, beam will "sweep" rather than "dwell"
            float maxTrackingSpeed = WeaponStats.RotationSpeed;
            
            if (requiredAngularVel > maxTrackingSpeed * 1.2f)
            {
                // Target moving too fast - beam cannot maintain effective dwell time
                // This follows real-world laser weapon doctrine (LaWS/HELIOS)
                if (Time.frameCount % 120 == 0)  // Log every 2 seconds
                {
                    Debug.Log($"[{gameObject.name}] Target too agile: Requires {requiredAngularVel:F1}°/s, Max tracking {maxTrackingSpeed:F1}°/s. Ceasing fire.");
                }
                return false;
            }
            
            return true;
        }

        // Multi-Muzzle Support
        private List<Transform> _muzzles = new List<Transform>();

        private void InitializeMuzzles()
        {
            _muzzles.Clear();
            
            // 1. Add the primary FirePoint (used for aiming)
            if (FirePoint != null) _muzzles.Add(FirePoint);

            // 2. Find other FirePoints (e.g. FirePoint_L, FirePoint_R)
            // We look recursively in case they are nested
            var allTransforms = GetComponentsInChildren<Transform>();
            foreach (var t in allTransforms)
            {
                if (t != FirePoint && t.name.Contains("FirePoint"))
                {
                    _muzzles.Add(t);
                }
            }
        }

        public void Fire(IDamageable target)
        {
            if (WeaponStats == null) return;
            
            // Branch based on firing mode
            if (WeaponStats.Mode == FiringMode.Beam)
            {
                FireBeam(target);
            }
            else
            {
                FireProjectile(target);
            }
        }
        
        private void FireBeam(IDamageable target)
        {
            // Ensure LaserBeamPool exists
            if (NavalCommand.Systems.Weapons.LaserBeamPool.Instance == null)
            {
                Debug.LogWarning("[WeaponController] LaserBeamPool not found! Cannot fire beam.");
                return;
            }
            
            // Ensure muzzles are initialized
            if (_muzzles.Count == 0) InitializeMuzzles();
            if (_muzzles.Count == 0) return;
            
            // Get first muzzle (beam weapons typically have single emitter)
            Transform muzzle = _muzzles[0];
            
            // CRITICAL: Check if turret can effectively track the target
            // Real-world laser weapons (LaWS/HELIOS) require sustained "dwell time" on target
            // If target angular velocity exceeds turret tracking speed, beam will "sweep" rather than "dwell"
            // This wastes energy without causing effective damage
            Transform targetTransform = ((MonoBehaviour)target).transform;
            if (!CanEffectivelyTrack(targetTransform))
            {
                // Target moving too fast - cease fire to avoid energy waste
                if (activeBeam != null && activeBeam.gameObject.activeSelf)
                {
                    activeBeam.Deactivate();  // Stop current beam
                }
                return;
            }
            
            // Get or reuse beam
            if (activeBeam == null || !activeBeam.gameObject.activeSelf)
            {
                activeBeam = NavalCommand.Systems.Weapons.LaserBeamPool.Instance.GetBeam();
            }
            
            // Initialize beam
            activeBeam.Initialize(
                muzzle,
                target,
                WeaponStats.Damage,  // DPS
                WeaponStats.Range,
                WeaponStats.ProjectileColor
            );
            
            // NOTE: Cooldown is now set in Update() after calling this function
            // This ensures proper cooldown enforcement when beam is deactivated
        }
        
        private void FireProjectile(IDamageable target)
        {
            // Projectile mode validation
            if (WeaponStats.ProjectilePrefab == null || PoolManager.Instance == null || WorldPhysicsSystem.Instance == null) 
            {
                return;
            }

            // Ensure muzzles are initialized
            if (_muzzles.Count == 0) InitializeMuzzles();

            // Fire from ALL muzzles
            foreach (var muzzle in _muzzles)
            {
                FireSingleProjectile(muzzle, target);
            }
            
            cooldownTimer = WeaponStats.Cooldown;
        }

        private void FireSingleProjectile(Transform muzzle, IDamageable target)
        {
            // Calculate Spread
            Quaternion fireRotation = muzzle.rotation;
            if (WeaponStats.Spread > 0.01f)
            {
                float xSpread = Random.Range(-WeaponStats.Spread, WeaponStats.Spread);
                float ySpread = Random.Range(-WeaponStats.Spread, WeaponStats.Spread);
                fireRotation = Quaternion.Euler(fireRotation.eulerAngles.x + xSpread, fireRotation.eulerAngles.y + ySpread, fireRotation.eulerAngles.z);
            }

            // Use PoolManager
            GameObject projectileObj = PoolManager.Instance.Spawn(WeaponStats.ProjectilePrefab, muzzle.position, fireRotation);
            
            // Spawn Muzzle Flash via VFXManager
            if (NavalCommand.Systems.VFX.VFXManager.Instance != null)
            {
                NavalCommand.Systems.VFX.VFXManager.Instance.SpawnMuzzleFlashVFX(muzzle.position, muzzle.rotation);
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
                if (WeaponStats.GravityMultiplier < 0.01f) gravityY = 0f;
                else gravityY *= WeaponStats.GravityMultiplier;

                Vector3 initialVelocity = fireRotation * Vector3.forward * scaledSpeed;

                projectile.Owner = ownerObj;
                projectile.Target = ((MonoBehaviour)target).transform;
                projectile.Damage = WeaponStats.Damage;
                projectile.Speed = scaledSpeed;
                projectile.ImpactProfile = WeaponStats.ImpactProfile;

                // Initialize with Velocity, Type, AND Custom Gravity
                projectile.Initialize(initialVelocity, OwnerTeam, WeaponStats.Type, gravityY);
            }
        }
    }
}
