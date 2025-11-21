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
                // Simple logic: pick first target (can be improved to "Nearest")
                currentTarget = ((MonoBehaviour)targets[0]).transform;
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
                // Simple first-order intercept
                float distance = Vector3.Distance(myPos, targetPos);
                float timeToHit = distance / speed;
                
                // Predict future position
                targetPos += targetRb.velocity * timeToHit;
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
            // We need to rotate the FirePoint or the Barrel.
            // Since we don't have a direct reference to the Barrel, we'll rotate the FirePoint locally.
            // Note: This assumes FirePoint is a child of the turret.
            
            float? angle = CalculateLaunchAngle(FirePoint.position, targetPos, speed, gravity);
            
            if (angle.HasValue)
            {
                // Apply pitch to FirePoint
                // Negative angle because X-axis rotation up is negative in some conventions, 
                // but usually positive X is down? Let's test.
                // Unity: Positive X is "down" (pitch down), Negative X is "up" (pitch up).
                // Wait, standard: Thumb right (X), fingers curl Y to Z. 
                // If we look forward (Z), rotating around X:
                // +X rotates Z towards -Y (Down).
                // -X rotates Z towards +Y (Up).
                // So we want -angle.
                
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
            
            // Trajectory equation: y = x * tan(theta) - (g * x^2) / (2 * v^2 * cos^2(theta))
            // Solved for theta: theta = atan( (v^2 +/- sqrt(v^4 - g(g*x^2 + 2*y*v^2))) / (g*x) )
            
            float root = v4 - g * (g * x * x + 2 * y * v2);
            
            if (root < 0)
            {
                // Target out of range
                return null;
            }

            // We want the lower angle (direct fire) usually, or high arc?
            // Let's use the lower angle for naval guns (direct).
            // theta = atan( (v^2 - sqrt(...)) / (g*x) )
            
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
