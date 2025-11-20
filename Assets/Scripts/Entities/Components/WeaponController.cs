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

        private float cooldownTimer;

        private void Update()
        {
            if (cooldownTimer > 0)
            {
                cooldownTimer -= Time.deltaTime;
            }
            else
            {
                FindTargetAndFire();
            }
        }

        private void FindTargetAndFire()
        {
            if (WeaponStats == null || SpatialGridSystem.Instance == null) return;

            // Determine target team
            Team targetTeam = (OwnerTeam == Team.Player) ? Team.Enemy : Team.Player;

            // Query Spatial Grid
            var targets = SpatialGridSystem.Instance.GetTargetsInRange(transform.position, WeaponStats.Range, targetTeam);

            if (targets.Count > 0)
            {
                // Simple logic: pick first target (can be improved to "Nearest")
                // TODO: Implement "Nearest" sort
                Fire(targets[0]);
            }
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
                projectile.Owner = gameObject; // Assign Owner (Turret)
                
                // If Turret is child of Ship, we might want the Ship to be the owner.
                // Assuming WeaponController is on a child object of the Unit.
                var parentUnit = GetComponentInParent<BaseUnit>();
                if (parentUnit != null)
                {
                    projectile.Owner = parentUnit.gameObject;
                }
            }
            
            cooldownTimer = WeaponStats.Cooldown;
        }
    }
}
