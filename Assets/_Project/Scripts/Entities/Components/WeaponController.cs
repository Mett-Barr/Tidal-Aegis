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
                if (Vector3.Dot(transform.forward, dirToTarget) > 0.95f && cooldownTimer <= 0)
                {
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
            var targets = SpatialGridSystem.Instance.GetTargetsInRange(transform.position, WeaponStats.Range, targetTeam);

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

            if (aimVector.HasValue)
            {
                // Apply Rotation
                Quaternion lookRotation = Quaternion.LookRotation(aimVector.Value);
                
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
                float pitchAngle = Mathf.Atan2(localAim.y, new Vector2(localAim.x, localAim.z).magnitude) * Mathf.Rad2Deg;
                
                // Clamp pitch if needed (e.g. -10 to +85)
                // pitchAngle = Mathf.Clamp(pitchAngle, -10f, 85f);

                Quaternion pitchRot = Quaternion.Euler(-pitchAngle, 0, 0); // Negative because X-up is usually pitch up
                FirePoint.localRotation = Quaternion.Slerp(FirePoint.localRotation, pitchRot, Time.deltaTime * RotationSpeed);
            }
        }

        /// <summary>
        /// Calculates the initial velocity vector needed to hit 'target' from 'start' with speed 'speed' and gravity 'gravity'.
        /// Returns null if out of range.
        /// </summary>
        private Vector3? CalculateBallisticVelocity(Vector3 start, Vector3 target, float speed, float gravity)
        {
            Vector3 dir = target - start;
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
