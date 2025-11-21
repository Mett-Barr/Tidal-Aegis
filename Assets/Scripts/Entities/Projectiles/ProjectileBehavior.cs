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
        public float GravityMultiplier = 2f; // Extra gravity for better arc at low speeds
        public Transform Target;
        public GameObject Owner; // Added Owner field

        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true; // Enable gravity for ballistic arc
                rb.isKinematic = false; // Allow physics to move it
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
            
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true; // Keep trigger for hit detection
        }

        private void Start()
        {
            // No initial impulse here, handled in Initialize
            
            // Add Trail Renderer if missing
            if (GetComponent<TrailRenderer>() == null)
            {
                var trail = gameObject.AddComponent<TrailRenderer>();
                trail.time = 0.5f;
                trail.startWidth = 0.2f;
                trail.endWidth = 0.0f;
                trail.material = new Material(Shader.Find("Sprites/Default"));
                trail.startColor = new Color(1f, 0.8f, 0f, 1f); // Gold/Fire
                trail.endColor = new Color(1f, 0f, 0f, 0f); // Fade to red transparent
            }
        }

        private void FixedUpdate()
        {
            if (rb != null && rb.useGravity)
            {
                // Apply extra gravity (GravityMultiplier - 1 because 1 is already applied by Physics)
                rb.AddForce(Physics.gravity * (GravityMultiplier - 1f), ForceMode.Acceleration);
            }
        }

        private void Update()
        {
            if (!isInitialized) return;

            // Check for water entry (Sea Level = 0)
            if (transform.position.y < 0)
            {
                CreateSplash();
                Despawn();
                return;
            }
            
            if (BehaviorType == ProjectileType.Homing && Target != null)
            {
                // Homing logic override (if needed later)
                // For now, let physics handle ballistic
            }
            
            // Rotate to face velocity vector for visual realism
            if (rb.velocity.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(rb.velocity);
            }
        }

        private void CreateSplash()
        {
            // TODO: Spawn Splash Particle
            // Debug.Log("Splash!");
        }

        private void Despawn()
        {
            if (isDespawning) return;
            isDespawning = true;

            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Despawn(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private bool isDespawning = false;
        private bool isInitialized = false;

        private void OnEnable()
        {
            isDespawning = false;
            isInitialized = false; // Wait for Initialize() to be called
            
            // Clear Trail Renderer to prevent "teleporting" trails from previous use
            TrailRenderer trail = GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.Clear();
            }
        }

        public Team ProjectileTeam; // Added Team field

        public void Initialize(GameObject owner, Team team)
        {
            Owner = owner;
            ProjectileTeam = team;
            isInitialized = true;
            
            // Ignore collision with owner's colliders immediately
            if (Owner != null)
            {
                Collider myCollider = GetComponent<Collider>();
                Collider[] ownerColliders = Owner.GetComponentsInChildren<Collider>();
                foreach (var ownerCol in ownerColliders)
                {
                    Physics.IgnoreCollision(myCollider, ownerCol, true);
                }
            }

            // Apply initial velocity if Rigidbody exists
            if (rb != null)
            {
                rb.velocity = transform.forward * Speed;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isDespawning || !isInitialized) return;

            // Ignore collision with owner AND its children
            if (Owner != null)
            {
                if (other.gameObject == Owner || other.transform.IsChildOf(Owner.transform))
                {
                    return;
                }
            }

            // Ignore collision with other projectiles
            if (other.GetComponent<ProjectileBehavior>() != null) return;

            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Friendly Fire Check
                if (damageable.GetTeam() == ProjectileTeam)
                {
                    return; // Ignore friendly units
                }

                Debug.Log($"Projectile hit Enemy: {other.name}");
                damageable.TakeDamage(Damage);
            }
            else
            {
                // Hit something non-damageable (like environment?)
                // For now, destroy on any impact except friendly
                Debug.Log($"Projectile hit Environment: {other.name}");
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
