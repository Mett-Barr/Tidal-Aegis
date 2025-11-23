using UnityEngine;
using NavalCommand.Data;
using NavalCommand.Core;
using NavalCommand.Systems;
using NavalCommand.Infrastructure;
using NavalCommand.Entities.Projectiles; // Added for ProjectileBehavior
using NavalCommand.Entities.Units; // Added for BaseUnit

namespace NavalCommand.Entities.Components
{
    public class WeaponController : MonoBehaviour
    {
        [Header("Configuration")]
        public WeaponStatsSO WeaponStats;
        public Transform FirePoint;
        public Team OwnerTeam = Team.Player;

        [Header("Turret Settings")]
        public float RotationSpeed = 5f;
        public bool IsWeaponEnabled = true; // Toggle for testing
        
        private Transform currentTarget;
        private float cooldownTimer;
        
        // Optimization: Staggered Updates
        private float updateInterval = 0.1f; // Run logic 10 times per second instead of every frame
        private float updateTimer;


        
        // Coroutine State
        private Vector3? desiredAimVector;
        private bool isFiring = false;

        private void Start()
        {
            if (WeaponStats == null)
            {
                Debug.LogWarning($"WeaponController on {gameObject.name} has no WeaponStats assigned! Turret will be inactive.");
            }
            else if (WeaponStats.Range <= 0)
            {
                Debug.LogWarning($"WeaponController on {gameObject.name} has invalid Range ({WeaponStats.Range})! Turret may not fire.");
            }

            // Sync Team with Parent Unit
            var parentUnit = GetComponentInParent<BaseUnit>();
            if (parentUnit != null)
            {
                OwnerTeam = parentUnit.UnitTeam;
            }
            
            if (WeaponStats != null)
            {
                RotationSpeed = WeaponStats.RotationSpeed;
            }
            
            // Start the Targeting Coroutine
            StartCoroutine(TargetingRoutine());
        }

        private System.Collections.IEnumerator TargetingRoutine()
        {
            // Initial Random Delay to stagger execution across frames
            yield return new WaitForSeconds(Random.Range(0f, 0.5f));

            // Dynamic Intervals based on Weapon Type
            float targetSearchInterval = 1.0f;
            float aimingInterval = 0.1f;

            if (WeaponStats.Type == WeaponType.CIWS)
            {
                // CIWS needs to be much faster to catch missiles
                targetSearchInterval = 0.2f;
                aimingInterval = 0.05f;
            }

            while (true)
            {
                if (!IsWeaponEnabled) 
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                // 1. Find Target
                FindTarget();
                
                if (currentTarget == null)
                {
                    // No target found, wait before searching again to avoid infinite loop
                    yield return new WaitForSeconds(targetSearchInterval);
                    continue;
                }
                
                // 2. Aiming Loop
                // Run aiming logic for 'targetSearchInterval' duration
                float timeElapsed = 0f;
                while (timeElapsed < targetSearchInterval)
                {
                    if (currentTarget != null)
                    {
                        if (WeaponStats.Type == WeaponType.Missile)
                        {
                            // VLS Exception: Do NOT rotate launcher. Always ready to fire.
                            desiredAimVector = null;
                            isFiring = true;
                        }
                        else
                        {
                            desiredAimVector = RotateTowardsTarget(); // Calculate aim only
                            
                            if (desiredAimVector.HasValue)
                            {
                                // Check Fire Condition
                                float dot = Vector3.Dot(FirePoint.forward, desiredAimVector.Value.normalized);
                                // Only check alignment, let Update handle cooldown
                                if (dot > 0.99f) 
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
                        }
                    }
                    else
                    {
                        isFiring = false;
                        desiredAimVector = null;
                        // If no target, break aiming loop early to search again
                        break; 
                    }
                    
                    yield return new WaitForSeconds(aimingInterval);
                    timeElapsed += aimingInterval;
                }
            }
        }

        private void Update()
        {
            if (!IsWeaponEnabled) return;

            // Optimization: Staggered Updates
            updateTimer += Time.deltaTime;
            if (updateTimer < updateInterval) return;
            updateTimer = 0f;

            // 1. Rotation Logic
            if (desiredAimVector.HasValue && FirePoint != null)
            {
                Quaternion targetRotation = Quaternion.LookRotation(desiredAimVector.Value);
                // Smoothly rotate towards target
                FirePoint.rotation = Quaternion.RotateTowards(FirePoint.rotation, targetRotation, RotationSpeed * Time.deltaTime * 100f); // *100 to make speed comparable to degrees/sec
            }

            // 2. Cooldown & Firing Logic
            if (cooldownTimer > 0)
            {
                cooldownTimer -= Time.deltaTime * 10f; // *10 to match the staggered update interval (0.1s)
            }

            if (isFiring)
            {
                // Handle High ROF (Multiple shots per frame)
                // Note: In Staggered Update, we might need to fire multiple times if cooldown is very low
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
            else
            {
                // Clamp cooldown if not firing to prevent "burst" on next trigger
                if (cooldownTimer < 0) cooldownTimer = 0;
            }
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
            
            if (currentTarget != closestTarget)
            {
                // Target changed
                // Debug.Log($"[WeaponController] {gameObject.name} acquired target: {closestTarget?.name}");
            }
            
            currentTarget = closestTarget;
        }

        private Vector3? RotateTowardsTarget()
        {
            if (currentTarget == null || WeaponStats == null) return null;

            Vector3 myPos = FirePoint.position;
            Vector3 targetVel = Vector3.zero;
            Vector3 targetAccel = Vector3.zero;

            // Get Physics Data
            float scaledSpeed = WorldPhysicsSystem.Instance.GetScaledSpeed(WeaponStats.ProjectileSpeed);
            float scaledRange = WorldPhysicsSystem.Instance.GetScaledRange(WeaponStats.Range);
            
            // Calculate Ballistic Gravity
            // This ensures the projectile can actually reach the max range at 45 degrees
            float gravityY = -WorldPhysicsSystem.Instance.GetBallisticGravity(scaledSpeed, scaledRange);
            
            // Apply Multiplier (e.g. 0 for CIWS/Missiles)
            if (WeaponStats.GravityMultiplier < 0.01f)
            {
                gravityY = 0f;
            }
            else
            {
                gravityY *= WeaponStats.GravityMultiplier;
            }

            Vector3 gravity = new Vector3(0, gravityY, 0);

            // Get Target Physics
            Rigidbody targetRb = currentTarget.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                targetVel = targetRb.velocity;
            }
            
            // Try to get advanced physics state if available
            // Note: IMovementProvider removed as it's not defined. 
            // Relying on ProjectileBehavior or Rigidbody for now.

            // Select Predictor
            ITargetPredictor predictor;
            
            // 1. Check if Target provides its own prediction strategy (e.g. Missiles)
            var predictionProvider = currentTarget.GetComponent<IPredictionProvider>();
            if (predictionProvider != null)
            {
                // Ask the target for its predictor relative to us
                // We pass our parent's velocity (ship velocity) as the observer velocity
                Vector3 myVel = GetComponentInParent<Rigidbody>()?.velocity ?? Vector3.zero;
                predictor = predictionProvider.GetPredictor(myPos, myVel);
            }
            else if (targetAccel.sqrMagnitude > 0.1f)
            {
                // 2. Fallback: Quadratic Prediction for maneuvering targets
                predictor = new QuadraticTargetPredictor(currentTarget.position, targetVel, targetAccel);
            }
            else
            {
                // 3. Fallback: Standard Linear Prediction
                predictor = new LinearTargetPredictor(currentTarget.position, targetVel);
            }

            Vector3 aimVector = Vector3.zero;
            bool hasSolution = false;

            // SIMPLIFIED LOGIC FOR MISSILES/TORPEDOES
            if (WeaponStats.Type == WeaponType.Missile || WeaponStats.Type == WeaponType.Torpedo)
            {
                // For self-guiding weapons, we don't need a perfect ballistic intercept.
                // We just need to point the launcher roughly at the target so the seeker can acquire it.
                // Pure Pursuit (Direct Aim) is usually sufficient and safest.
                aimVector = (currentTarget.position - myPos).normalized * scaledSpeed;
                hasSolution = true;
            }
            else
            {
                // For Guns/CIWS, use the Ballistics Computer
                // Pass gravityY (magnitude) instead of Vector3 gravity
                if (BallisticsComputer.SolveInterception(myPos, scaledSpeed, Mathf.Abs(gravityY), predictor, out Vector3 solution, out float t))
                {
                    aimVector = solution;
                    hasSolution = true;
                }
            }

            // Solve!
            if (hasSolution)
            {
                // Return the calculated aim vector. 
                // Actual rotation is now handled in Update() for smoothness.
                return aimVector;
            }
            
            return null;
        }

        public void Fire(IDamageable target)
        {
            if (WeaponStats == null || WeaponStats.ProjectilePrefab == null || PoolManager.Instance == null || WorldPhysicsSystem.Instance == null) return;

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

                Vector3 initialVelocity = fireRotation * Vector3.forward * scaledSpeed;

                projectile.Owner = ownerObj;
                projectile.Target = ((MonoBehaviour)target).transform;
                projectile.Damage = WeaponStats.Damage;
                projectile.Speed = scaledSpeed;
                projectile.ImpactProfile = WeaponStats.ImpactProfile;

                // Initialize with Velocity, Type, AND Custom Gravity
                projectile.Initialize(initialVelocity, OwnerTeam, WeaponStats.Type, gravityY);
                
                // Note: Gravity and Behavior are now handled by the Projectile's internal MovementLogic
                // which is pre-configured on the Prefab. We don't need to set them here.
            }
            
            cooldownTimer = WeaponStats.Cooldown;
        }
    }
}
