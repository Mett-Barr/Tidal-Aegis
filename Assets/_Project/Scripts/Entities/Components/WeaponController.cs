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
        private Transform currentTarget;
        private float cooldownTimer;

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
        }

        private void Update()
        {
            if (cooldownTimer > 0)
            {
                cooldownTimer -= Time.deltaTime;
            }

            FindTarget();

            if (currentTarget != null)
            {
                RotateTowardsTarget();
                
                // Only fire if aiming roughly at target
                Vector3 dirToTarget = (currentTarget.position - transform.position).normalized;
                float dot = Vector3.Dot(transform.forward, dirToTarget);
                
                if (dot > 0.95f && cooldownTimer <= 0)
                {
                    Debug.Log($"[WeaponController] Firing {WeaponStats.name} at {currentTarget.name}!");
                    Fire(currentTarget.GetComponent<IDamageable>());
                }
            }
        }

        private void FindTarget()
        {
            if (WeaponStats == null || SpatialGridSystem.Instance == null) return;

            // Determine target team
            Team targetTeam = (OwnerTeam == Team.Player) ? Team.Enemy : Team.Player;

            // Query Spatial Grid
            var targets = SpatialGridSystem.Instance.GetTargetsInRange(transform.position, WeaponStats.Range, targetTeam, WeaponStats.TargetType);
            
            // Debug Log for Target Search
            if (Time.frameCount % 120 == 0) // Log every 2 seconds
            {
                 Debug.Log($"[WeaponController] {gameObject.name} (Team: {OwnerTeam}) searching for {targetTeam} in range {WeaponStats.Range}. Found {targets.Count} targets.");
            }

            if (targets.Count > 0)
            {
                // Find nearest target
                float closestDistSqr = float.MaxValue;
                Transform closestTarget = null;

                foreach (var target in targets)
                {
                    Transform tTrans = ((MonoBehaviour)target).transform;
                    float distSqr = (tTrans.position - transform.position).sqrMagnitude;
                    if (distSqr < closestDistSqr)
                    {
                        closestDistSqr = distSqr;
                        closestTarget = tTrans;
                    }
                }
                currentTarget = closestTarget;
            }
            else
            {
                currentTarget = null;
            }
        }

        private void RotateTowardsTarget()
        {
            if (currentTarget == null) return;

            Vector3 targetPos = currentTarget.position;
            Vector3 myPos = FirePoint.position; // Use FirePoint for accuracy
            float speed = WeaponStats.ProjectileSpeed;
            float gravity = Mathf.Abs(Physics.gravity.y) * WeaponStats.GravityMultiplier;

            // Iterative Solver for Aiming
            // We need to find a firing angle (and thus a direction) that hits the moving target.
            // Since flight time depends on the angle (which depends on the predicted position),
            // and predicted position depends on flight time, we iterate.

            float t = (targetPos - myPos).magnitude / speed; // Initial guess: Linear time
            Vector3 predictedPos = targetPos;
            Vector3? aimVector = null;

            Rigidbody targetRb = currentTarget.GetComponent<Rigidbody>();
            Vector3 targetVel = (targetRb != null) ? targetRb.velocity : Vector3.zero;

            for (int i = 0; i < 5; i++) // 5 iterations is usually enough
            {
                predictedPos = targetPos + targetVel * t;
                
                // Calculate ballistic velocity vector needed to hit predictedPos
                // This solves for the high/low arc. We usually want the low arc.
                Vector3? v0 = CalculateBallisticVelocity(myPos, predictedPos, speed, gravity);

                if (v0.HasValue)
                {
                    aimVector = v0.Value;
                    // Refine time estimate based on the actual arc length/speed
                    // For ballistic, horizontal speed is constant: t = dist_xz / v_xz
                    Vector3 horizontalDist = new Vector3(predictedPos.x - myPos.x, 0, predictedPos.z - myPos.z);
                    Vector3 horizontalVel = new Vector3(v0.Value.x, 0, v0.Value.z);
                    
                    if (horizontalVel.magnitude > 0.001f)
                    {
                        float newT = horizontalDist.magnitude / horizontalVel.magnitude;
                        t = Mathf.Lerp(t, newT, 0.5f); // Smooth convergence
                    }
                    else
                    {
                        break; // Vertical shot?
                    }
                }
                else
                {
                    // Unreachable
                    break;
                }
            }

            if (aimVector.HasValue && aimVector.Value.sqrMagnitude > 0.001f)
            {
                // Apply Rotation
                // Validate aimVector before LookRotation
                if (float.IsNaN(aimVector.Value.x) || float.IsNaN(aimVector.Value.y) || float.IsNaN(aimVector.Value.z))
                {
                    return;
                }

                // 1. Rotate Base (Yaw) - Only Y axis
                Vector3 yawDir = aimVector.Value;
                yawDir.y = 0;
                if (yawDir.sqrMagnitude > 0.001f)
                {
                    Quaternion yawRot = Quaternion.LookRotation(yawDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, yawRot, Time.deltaTime * RotationSpeed);
                }

                // 2. Rotate FirePoint (Pitch) - Local X axis
                // We need to convert the world aim vector to local space relative to the turret base
                Vector3 localAim = transform.InverseTransformDirection(aimVector.Value);
                
                // Check for zero vector in local space (shouldn't happen if world is valid, but safe to check)
                if (new Vector2(localAim.x, localAim.z).sqrMagnitude > 0.0001f)
                {
                    float pitchAngle = Mathf.Atan2(localAim.y, new Vector2(localAim.x, localAim.z).magnitude) * Mathf.Rad2Deg;
                    
                    if (!float.IsNaN(pitchAngle))
                    {
                        Quaternion pitchRot = Quaternion.Euler(-pitchAngle, 0, 0); // Negative because X-up is usually pitch up
                        FirePoint.localRotation = Quaternion.Slerp(FirePoint.localRotation, pitchRot, Time.deltaTime * RotationSpeed);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the initial velocity vector needed to hit 'target' from 'start' with speed 'speed' and gravity 'gravity'.
        /// Returns null if out of range.
        /// </summary>
        private Vector3? CalculateBallisticVelocity(Vector3 start, Vector3 target, float speed, float gravity)
        {
            Vector3 dir = target - start;
            
            // Handle Zero Gravity (Missiles/Torpedoes)
            if (gravity < 0.001f)
            {
                return dir.normalized * speed;
            }

            Vector3 dirXZ = new Vector3(dir.x, 0, dir.z);
            float x = dirXZ.magnitude;
            float y = dir.y;

            float v2 = speed * speed;
            float v4 = speed * speed * speed * speed;
            float g = gravity;

            float root = v4 - g * (g * x * x + 2 * y * v2);

            if (root < 0) return null; // Out of range

            // Low arc solution (minus sqrt)
            float angle = Mathf.Atan((v2 - Mathf.Sqrt(root)) / (g * x));
            
            if (float.IsNaN(angle)) return null;

            Vector3 jumpDir = dirXZ.normalized;
            Vector3 v0 = jumpDir * Mathf.Cos(angle) * speed + Vector3.up * Mathf.Sin(angle) * speed;
            return v0;
        }



        public void Fire(IDamageable target)
        {
            if (WeaponStats == null || WeaponStats.ProjectilePrefab == null || PoolManager.Instance == null) return;

            // Use PoolManager
            GameObject projectileObj = PoolManager.Instance.Spawn(WeaponStats.ProjectilePrefab, FirePoint.position, FirePoint.rotation);
            
            ProjectileBehavior projectile = projectileObj.GetComponent<ProjectileBehavior>();
            if (projectile != null)
            {
                projectile.Damage = WeaponStats.Damage;
                projectile.Target = ((MonoBehaviour)target).transform;
                
                GameObject ownerObj = gameObject; // Default to Turret
                
                // If Turret is child of Ship, we might want the Ship to be the owner.
                var parentUnit = GetComponentInParent<BaseUnit>();
                if (parentUnit != null)
                {
                    ownerObj = parentUnit.gameObject;
                }
                
            // Initialize properly to handle collision ignore and team assignment
            projectile.Initialize(ownerObj, OwnerTeam);
            
            // Apply Velocity
            if (projectile.GetComponent<Rigidbody>() is Rigidbody pRb)
            {
                pRb.velocity = FirePoint.forward * WeaponStats.ProjectileSpeed;
            }
            
            // Sync Gravity Multiplier
            projectile.GravityMultiplier = WeaponStats.GravityMultiplier;
            
            }
            
            cooldownTimer = WeaponStats.Cooldown;
        }
    }
}
