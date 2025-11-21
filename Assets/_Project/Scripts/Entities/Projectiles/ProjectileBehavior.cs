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
        
        [Header("Advanced Guidance")]
        public float CruiseHeight = 10f; // For Missiles (Sea-skimming) or Torpedoes (Depth)
        public float TerminalHomingDistance = 50f; // Distance to switch to direct homing
        public float VerticalLaunchHeight = 0f; // If > 0, rises vertically first
        public float TurnRate = 2f; // Turning speed for homing

        public Transform Target;
        public GameObject Owner;

        private Rigidbody rb;
        private float launchTime;
        private bool isVerticalPhaseComplete = false;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true; 
                rb.isKinematic = false; 
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
            
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true; 
        }

        private void Start()
        {
            if (GetComponent<TrailRenderer>() == null)
            {
                var trail = gameObject.AddComponent<TrailRenderer>();
                trail.time = 0.5f;
                trail.startWidth = 0.2f;
                trail.endWidth = 0.0f;
                trail.material = new Material(Shader.Find("Sprites/Default"));
                trail.startColor = new Color(1f, 0.8f, 0f, 1f); 
                trail.endColor = new Color(1f, 0f, 0f, 0f); 
            }
        }

        private void FixedUpdate()
        {
            if (!isInitialized) return;

            if (BehaviorType == ProjectileType.Ballistic)
            {
                if (rb != null && rb.useGravity)
                {
                    rb.AddForce(Physics.gravity * (GravityMultiplier - 1f), ForceMode.Acceleration);
                }
            }
            else if (BehaviorType == ProjectileType.Homing)
            {
                HandleHomingBehavior();
            }
            else if (BehaviorType == ProjectileType.Straight)
            {
                rb.velocity = transform.forward * Speed;
                rb.useGravity = false;
            }
        }

        private void HandleHomingBehavior()
        {
            rb.useGravity = false;
            
            // 1. Vertical Launch Phase
            if (VerticalLaunchHeight > 0 && !isVerticalPhaseComplete)
            {
                if (transform.position.y < VerticalLaunchHeight)
                {
                    // Rise vertically
                    rb.velocity = Vector3.up * (Speed * 0.5f); // Slower launch
                    transform.rotation = Quaternion.LookRotation(Vector3.up);
                    return;
                }
                else
                {
                    isVerticalPhaseComplete = true;
                    // Initial turn towards target
                    if (Target != null)
                    {
                        Vector3 dir = (Target.position - transform.position).normalized;
                        rb.velocity = dir * Speed;
                    }
                }
            }

            if (Target == null)
            {
                // Lost target, fly straight
                rb.velocity = transform.forward * Speed;
                return;
            }

            Vector3 targetPos = Target.position;
            float distToTarget = Vector3.Distance(transform.position, targetPos);
            Vector3 desiredDirection;

            // 2. Terminal Phase (Direct Homing)
            if (distToTarget < TerminalHomingDistance)
            {
                desiredDirection = (targetPos - transform.position).normalized;
            }
            // 3. Cruise Phase (Sea-skimming or Depth keeping)
            else
            {
                // Aim for target XZ, but maintain CruiseHeight Y
                Vector3 cruiseTarget = targetPos;
                cruiseTarget.y = CruiseHeight;
                
                // If we are far from cruise height, prioritize getting there
                float heightError = CruiseHeight - transform.position.y;
                
                // Simple P-controller for height
                cruiseTarget.y = transform.position.y + Mathf.Clamp(heightError, -10f, 10f);
                
                desiredDirection = (cruiseTarget - transform.position).normalized;
            }

            // Apply Rotation
            Quaternion targetRot = Quaternion.LookRotation(desiredDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * TurnRate);
            
            // Apply Velocity
            rb.velocity = transform.forward * Speed;
        }

        private void Update()
        {
            if (!isInitialized) return;

            // Check for water entry (Sea Level = 0)
            // Exception: Torpedoes (CruiseHeight < 0) are allowed underwater
            bool isTorpedo = (BehaviorType == ProjectileType.Homing && CruiseHeight < 0);
            
            if (!isTorpedo && transform.position.y < -1f) // Tolerance
            {
                CreateSplash();
                Despawn();
                return;
            }
            
            // Rotate to face velocity vector for Ballistic
            if (BehaviorType == ProjectileType.Ballistic && rb.velocity.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(rb.velocity);
            }
        }

        private void CreateSplash()
        {
            // TODO: Spawn Splash Particle
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
            isInitialized = false; 
            isVerticalPhaseComplete = false;
            
            TrailRenderer trail = GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.Clear();
            }
        }

        public Team ProjectileTeam; 

        public void Initialize(GameObject owner, Team team)
        {
            Owner = owner;
            ProjectileTeam = team;
            isInitialized = true;
            launchTime = Time.time;
            
            if (Owner != null)
            {
                Collider myCollider = GetComponent<Collider>();
                Collider[] ownerColliders = Owner.GetComponentsInChildren<Collider>();
                foreach (var ownerCol in ownerColliders)
                {
                    Physics.IgnoreCollision(myCollider, ownerCol, true);
                }
            }

            if (rb != null)
            {
                // Initial velocity
                if (BehaviorType == ProjectileType.Ballistic || BehaviorType == ProjectileType.Straight)
                {
                    rb.velocity = transform.forward * Speed;
                }
                else if (BehaviorType == ProjectileType.Homing)
                {
                    // Start slow or vertical
                    rb.velocity = transform.forward * (Speed * 0.1f);
                }
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
