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

            if (cooldownTimer > 0)
            {
                cooldownTimer -= Time.deltaTime;
            }

            // Visual Rotation (Smooth, every frame)
            if (desiredAimVector.HasValue && desiredAimVector.Value.sqrMagnitude > 0.001f)
            {
                Vector3 aimVector = desiredAimVector.Value;

                // 1. Rotate Base (Yaw) - Only Y axis
                Vector3 yawDir = aimVector;
                yawDir.y = 0;
                if (yawDir.sqrMagnitude > 0.001f)
                {
                    Quaternion yawRot = Quaternion.LookRotation(yawDir);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, yawRot, Time.deltaTime * RotationSpeed);
                }

                // 2. Rotate FirePoint (Pitch) - Local X axis
                Vector3 localAim = transform.InverseTransformDirection(aimVector);
                
                if (new Vector2(localAim.x, localAim.z).sqrMagnitude > 0.0001f)
                {
                    float pitchAngle = Mathf.Atan2(localAim.y, new Vector2(localAim.x, localAim.z).magnitude) * Mathf.Rad2Deg;
                    
                    if (!float.IsNaN(pitchAngle))
                    {
                        Quaternion pitchRot = Quaternion.Euler(-pitchAngle, 0, 0);
                        FirePoint.localRotation = Quaternion.RotateTowards(FirePoint.localRotation, pitchRot, Time.deltaTime * RotationSpeed);
                    }
                }
                
                // Debug Visualization
                if (WeaponStats.Type == WeaponType.CIWS)
                {
                    Debug.DrawRay(FirePoint.position, FirePoint.forward * 100f, Color.red);
                    Debug.DrawRay(FirePoint.position, aimVector.normalized * 100f, Color.green);
                }
            }

            // Firing (Synced with Update for frame-perfect spawning)
            if (isFiring && currentTarget != null)
            {
                // Handle High ROF (Multiple shots per frame)
                while (cooldownTimer <= 0)
                {
                    Fire(currentTarget.GetComponent<IDamageable>());
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
            if (currentTarget == null || WorldPhysicsSystem.Instance == null) return null;

            Vector3 myPos = FirePoint.position;
            
            // Get Scaled Physics Values
            float scaledSpeed = WorldPhysicsSystem.Instance.GetScaledSpeed(WeaponStats.ProjectileSpeed);
            float scaledRange = WorldPhysicsSystem.Instance.GetScaledRange(WeaponStats.Range);
            
            // Determine Gravity
            float gravity = 0f;
            if (WeaponStats.Type == WeaponType.Missile || WeaponStats.Type == WeaponType.Torpedo)
            {
                gravity = 0f;
            }
            else
            {
                // Check explicit override or default
                if (WeaponStats.GravityMultiplier < 0.01f) gravity = 0f;
                else gravity = WorldPhysicsSystem.Instance.GetBallisticGravity(scaledSpeed, scaledRange);
            }

            // Create Target Predictor
            Vector3 targetVel = Vector3.zero;
            Vector3 targetAccel = Vector3.zero;
            
            var targetProjectile = currentTarget.GetComponent<ProjectileBehavior>();
            Rigidbody targetRb = currentTarget.GetComponent<Rigidbody>();

            if (targetProjectile != null)
            {
                targetVel = targetProjectile.Velocity;
                targetAccel = targetProjectile.Acceleration;
            }
            else if (targetRb != null)
            {
                targetVel = targetRb.velocity;
            }

            ITargetPredictor predictor;
            
            // Advanced Prediction: If it's a missile targeting US, simulate its ProNav logic
            if (targetProjectile != null && targetProjectile.Target != null && 
                (targetProjectile.Target == transform || targetProjectile.Target.IsChildOf(transform.root)))
            {
                // Create a predictor for OURSELVES (The thing the missile is chasing)
                // Assume we move linearly for the short interception window
                Rigidbody myRb = GetComponentInParent<Rigidbody>();
                Vector3 myVel = (myRb != null) ? myRb.velocity : Vector3.zero;
                ITargetPredictor myPredictor = new LinearTargetPredictor(transform.position, myVel);
                
                // Create the ProNav Simulator
                predictor = new ProNavTargetPredictor(currentTarget.position, targetVel, myPredictor);
            }
            else if (targetAccel.sqrMagnitude > 0.1f)
            {
                // Fallback: Quadratic Prediction for maneuvering targets not targeting us
                predictor = new QuadraticTargetPredictor(currentTarget.position, targetVel, targetAccel);
            }
            else
            {
                // Standard Linear Prediction
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
                if (BallisticsComputer.SolveInterception(myPos, scaledSpeed, gravity, predictor, out Vector3 solution, out float t))
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
                Vector3 initialVelocity = fireRotation * Vector3.forward * scaledSpeed;

                projectile.Owner = ownerObj;
                projectile.Target = ((MonoBehaviour)target).transform;
                projectile.Damage = WeaponStats.Damage;
                projectile.Speed = scaledSpeed;

                // Initialize with Velocity and Type
                projectile.Initialize(initialVelocity, OwnerTeam, WeaponStats.Type);
                
                // Note: Gravity and Behavior are now handled by the Projectile's internal MovementLogic
                // which is pre-configured on the Prefab. We don't need to set them here.
            }
            
            cooldownTimer = WeaponStats.Cooldown;
        }
    }
}
