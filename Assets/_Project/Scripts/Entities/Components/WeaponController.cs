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
            Vector3 myPos = transform.position;
            float speed = WeaponStats.ProjectileSpeed;
            float gravity = Mathf.Abs(Physics.gravity.y) * WeaponStats.GravityMultiplier;

            // Predictive Aiming (Leading)
            Rigidbody targetRb = currentTarget.GetComponent<Rigidbody>();
            if (targetRb != null && targetRb.velocity.sqrMagnitude > 0.1f)
            {
                // Calculate Intercept
                Vector3 targetVel = targetRb.velocity;
                Vector3 relativePos = targetPos - myPos;
                
                // Quadratic equation: (Vt^2 - S^2)t^2 + 2(D.Vt)t + D^2 = 0
                float a = Vector3.Dot(targetVel, targetVel) - speed * speed;
                float b = 2f * Vector3.Dot(relativePos, targetVel);
                float c = Vector3.Dot(relativePos, relativePos);

                float t = -1f;

                // Check for valid solutions
                if (Mathf.Abs(a) < 0.001f)
                {
                    // Linear case (target speed approx projectile speed? rare)
                    if (Mathf.Abs(b) > 0.001f) t = -c / b;
                }
                else
                {
                    float discriminant = b * b - 4f * a * c;
                    if (discriminant >= 0)
                    {
                        float sqrtDisc = Mathf.Sqrt(discriminant);
                        float t1 = (-b - sqrtDisc) / (2f * a);
                        float t2 = (-b + sqrtDisc) / (2f * a);

                        if (t1 > 0 && t2 > 0) t = Mathf.Min(t1, t2);
                        else if (t1 > 0) t = t1;
                        else if (t2 > 0) t = t2;
                    }
                }

                if (t > 0)
                {
                    targetPos += targetVel * t;
                }
            }

            // 1. Rotate Base (Yaw)
            Vector3 direction = (targetPos - myPos).normalized;
            direction.y = 0; // Keep base flat
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * RotationSpeed);
            }

            // 2. Calculate Elevation (Pitch)
            float? angle = CalculateLaunchAngle(FirePoint.position, targetPos, speed, gravity);
            
            if (angle.HasValue)
            {
                Quaternion targetPitch = Quaternion.Euler(-angle.Value, 0, 0);
                FirePoint.localRotation = Quaternion.Slerp(FirePoint.localRotation, targetPitch, Time.deltaTime * RotationSpeed);
            }
        }

        /// <summary>
        /// Calculates the ballistic launch angle (elevation) needed to hit a target.
        /// Returns null if out of range.
        /// </summary>
        private float? CalculateLaunchAngle(Vector3 start, Vector3 end, float v, float g)
        {
            Vector3 dir = end - start;
            float x = new Vector3(dir.x, 0, dir.z).magnitude; // Horizontal distance
            float y = dir.y; // Vertical difference

            float v2 = v * v;
            float v4 = v * v * v * v;
            
            float root = v4 - g * (g * x * x + 2 * y * v2);
            
            if (root < 0)
            {
                // Target out of range
                return null;
            }

            float angleRad = Mathf.Atan((v2 - Mathf.Sqrt(root)) / (g * x));
            return angleRad * Mathf.Rad2Deg;
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
