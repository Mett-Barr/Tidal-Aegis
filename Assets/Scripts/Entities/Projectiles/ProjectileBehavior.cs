using UnityEngine;
using NavalCommand.Core;
using NavalCommand.Infrastructure;

namespace NavalCommand.Entities.Projectiles
{
    public enum ProjectileType
    {
        Ballistic,
        Homing,
        Straight
    }

    public class ProjectileBehavior : MonoBehaviour
    {
        [Header("Settings")]
        public ProjectileType BehaviorType;
        public float Speed = 20f;
        public float Damage = 10f;
        public Transform Target;
        public GameObject Owner; // Added Owner field

        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            if (BehaviorType == ProjectileType.Ballistic)
            {
                // Initial impulse for ballistic trajectory
                // rb.AddForce(...)
            }
        }

        private void Update()
        {
            if (BehaviorType == ProjectileType.Homing && Target != null)
            {
                // Homing logic
                Vector3 direction = (Target.position - transform.position).normalized;
                transform.forward = Vector3.RotateTowards(transform.forward, direction, 2f * Time.deltaTime, 0f);
                transform.position += transform.forward * Speed * Time.deltaTime;
            }
            else if (BehaviorType == ProjectileType.Straight)
            {
                transform.position += transform.forward * Speed * Time.deltaTime;
            }
        }

        private bool isDespawning = false;

        private void OnEnable()
        {
            isDespawning = false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (isDespawning) return;

            // Ignore collision with owner
            if (Owner != null && collision.gameObject == Owner) return;

            IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(Damage);
            }
            
            isDespawning = true;

            // Use PoolManager to despawn self
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Despawn(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
