using UnityEngine;
using NavalCommand.Core;
using NavalCommand.Infrastructure;
using NavalCommand.Systems;
using NavalCommand.Systems.Movement;
using NavalCommand.Data;

namespace NavalCommand.Entities.Projectiles
{
    public class ProjectileBehavior : MonoBehaviour, IDamageable, IPredictionProvider
    {
        [Header("Identity")]
        public Team ProjectileTeam;
        public WeaponType SourceWeaponType;
        
        [Header("Movement Logic")]
        public string MovementLogicName; // Set by ContentGenerator/WeaponConfig
        private MovementLogic _movementLogic;
        private MovementState _currentState;
        
        [Header("Settings")]
        public float Speed = 20f;
        public float Damage = 10f;
        
        // Custom Data for Logic (packed into State)
        // CustomData for Logic (packed into State)
        public float CruiseHeight = 40f; // Higher cruise for Top Attack
        public float TerminalHomingDistance = 50f; // Start dive earlier for smoother intercept
        public float VerticalLaunchHeight = 15f; // Distinct VLS phase
        public float TurnRate = 2f; // Unused by functional logic (hardcoded there)

        public Transform Target;
        public GameObject Owner;
        public NavalCommand.Systems.VFX.ImpactProfile ImpactProfile;

        private Rigidbody rb;
        private bool isInitialized = false;
        private bool isDespawning = false;

        // IDamageable Implementation
        public void TakeDamage(float amount)
        {
            Despawn();
        }

        public bool IsDead() => isDespawning;
        public Team GetTeam() => ProjectileTeam;
        
        public UnitType GetUnitType()
        {
            switch (SourceWeaponType)
            {
                case WeaponType.Missile:
                    return UnitType.Missile;
                case WeaponType.Torpedo:
                    return UnitType.Torpedo;
                default:
                    return UnitType.Shell; // Unguided Projectiles
            }
        }

        public Vector3 Acceleration => _currentState.Acceleration; // Expose for Prediction
        public Vector3 Velocity => _currentState.Velocity; // Expose for Prediction

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false; // Logic handles gravity
                rb.isKinematic = true; // Logic handles movement
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
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

        private void OnEnable()
        {
            isDespawning = false;
            isInitialized = false; 
            
            TrailRenderer trail = GetComponent<TrailRenderer>();
            if (trail != null) trail.Clear();

            if (SpatialGridSystem.Instance != null)
            {
                SpatialGridSystem.Instance.Register(this, transform.position);
            }
        }

        private void OnDisable()
        {
            if (SpatialGridSystem.Instance != null)
            {
                SpatialGridSystem.Instance.Unregister(this);
            }
        }

        // Custom Gravity for Ballistics
        private Vector3? _customGravity;

        public void Initialize(Vector3 velocity, Team team, WeaponType weaponType, float? customGravityY = null)
        {
            ProjectileTeam = team;
            SourceWeaponType = weaponType;
            
            if (customGravityY.HasValue)
            {
                _customGravity = new Vector3(0, customGravityY.Value, 0);
            }

            // Resolve Logic Delegate
            _movementLogic = ResolveLogic(MovementLogicName);
            
            // Pack Initial State
            _currentState = MovementState.Create(transform.position, velocity, transform.rotation);
            
            // Pack Custom Data (X: Cruise, Y: Terminal, Z: VLS)
            _currentState.CustomData = new Vector3(CruiseHeight, TerminalHomingDistance, VerticalLaunchHeight);

            if (MovementLogicName == "GuidedMissile")
            {
                if (CruiseHeight < 1f) _currentState.CustomData.x = 15f; // Fallback
                if (TerminalHomingDistance < 1f) _currentState.CustomData.y = 50f; // Fallback
                if (VerticalLaunchHeight < 1f) _currentState.CustomData.z = 20f; // Fallback
                
                // Enforce Cruise >= VLS to prevent "Diving" behavior
                if (_currentState.CustomData.x < _currentState.CustomData.z)
                {
                    _currentState.CustomData.x = _currentState.CustomData.z;
                }
            }

            // VLS Override: If this is a VLS missile, force it to launch UP regardless of turret aim
            // DISABLED: User requested platform-driven logic. FirePoint rotation determines launch vector.
            // if (MovementLogicName == "GuidedMissile" && VerticalLaunchHeight > 1.0f)
            // {
            //     _currentState.Velocity = Vector3.up * velocity.magnitude;
            //     _currentState.Rotation = Quaternion.LookRotation(Vector3.up);
            // }

            // Clear Trail Renderer to prevent "streaks" from pooling
            var trail = GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.Clear();
            }

            _lifetimeTimer = 0f;
            _spawnTime = Time.time; // Track spawn time
            isInitialized = true;
        }

        private float _spawnTime;

        private MovementLogic ResolveLogic(string name)
        {
            switch (name)
            {
                case "Ballistic": return MovementFunctions.Ballistic;
                case "GuidedMissile": return MovementFunctions.GuidedMissile;
                case "Torpedo": return MovementFunctions.Torpedo;
                case "Linear": 
                    // ARCHITECTURAL FIX: "Linear" is just Ballistic with 0 Gravity.
                    // We map it to Ballistic so it respects the GravityMultiplier passed from WeaponStats.
                    // This ensures SSOT: GravityMultiplier controls the trajectory, not the Logic Name.
                    return MovementFunctions.Ballistic;
                default:
                    Debug.LogWarning($"[ProjectileBehavior] Unknown logic '{name}', defaulting to Ballistic.");
                    return MovementFunctions.Ballistic;
            }
        }

        private void FixedUpdate()
        {
            if (!isInitialized || isDespawning) return;

            float dt = Time.fixedDeltaTime;

            // 1. Build Context
            MovementContext ctx = new MovementContext
            {
                Gravity = _customGravity ?? Physics.gravity,
                TargetState = null,
                TargetPrediction = null
            };

            if (Target != null)
            {
                // In a real system, we'd get the target's Velocity/Rotation too.
                // For now, we just get Position.
                // Ideally, Target should implement an interface like IMovementProvider
                var targetRb = Target.GetComponent<Rigidbody>();
                Vector3 targetVel = targetRb != null ? targetRb.velocity : Vector3.zero;

                ctx.TargetState = new MovementState
                {
                    Position = Target.position,
                    Velocity = targetVel,
                    Rotation = Target.rotation
                };
                
                // TODO: Attach Prediction Function from PredictionEngine if needed
                // ctx.TargetPrediction = ...
            }

            // 2. Execute Logic (Pure Function)
            if (_movementLogic != null)
            {
                _currentState = _movementLogic(_currentState, ctx, dt);
            }

            // 3. Apply State to World
            transform.position = _currentState.Position;
            transform.rotation = _currentState.Rotation;
            
            // Update Spatial Grid
            if (SpatialGridSystem.Instance != null)
            {
                SpatialGridSystem.Instance.UpdatePosition(this, transform.position);
            }
            
            // Check Collision (Raycast for high speed)
            CheckCollision(_currentState.Velocity.magnitude * dt);
            
            // Water Check
            CheckWaterEntry();
            
            // Lifetime & Distance Check
            CheckLifetime(dt);
            CheckDistance();
        }

        private float _lifetimeTimer;
        private const float MAX_LIFETIME = 30f;
        private const float MAX_DISTANCE = 20000f; // 20km

        private void CheckLifetime(float dt)
        {
            _lifetimeTimer += dt;
            if (_lifetimeTimer > MAX_LIFETIME)
            {
                Despawn();
            }
        }

        private void CheckDistance()
        {
            if (transform.position.magnitude > MAX_DISTANCE)
            {
                Despawn();
            }
        }

        private void CheckCollision(float distance)
        {
            // Use SphereCast for "Thick" bullets - better for hitting thin missiles
            // Reduced radius from 2.0f to 0.5f to prevent floor/self collisions
            float radius = 0.5f; 
            
            // Increase check distance slightly to account for target moving TOWARDS us
            float checkDistance = distance + 1.0f;

            if (Physics.SphereCast(transform.position, radius, _currentState.Velocity.normalized, out RaycastHit hit, checkDistance))
            {
                // Ignore Friendly Projectiles to prevent self-detonation/log spam
                var otherProjectile = hit.collider.GetComponent<ProjectileBehavior>();
                if (otherProjectile != null && otherProjectile.ProjectileTeam == ProjectileTeam)
                {
                    return;
                }

                HandleImpact(hit);
            }
        }
        
        private void CheckWaterEntry()
        {
            // Exception: Torpedoes are allowed underwater
            bool isTorpedo = (MovementLogicName == "Torpedo");
            
            if (!isTorpedo && transform.position.y < -1f)
            {
                // VFX
                if (NavalCommand.Systems.VFX.VFXManager.Instance != null)
                {
                    var context = new NavalCommand.Systems.VFX.ImpactPayload(
                        ImpactProfile,
                        NavalCommand.Systems.VFX.SurfaceType.Water,
                        transform.position,
                        Vector3.up
                    );
                    NavalCommand.Systems.VFX.VFXManager.Instance.SpawnVFX(context);
                    // Debug.Log($"[ProjectileBehavior] Spawning Water Splash at {transform.position}");
                }

                Despawn();
            }
        }

        private void HandleImpact(RaycastHit hit)
        {
            // Ignore Self/Owner collisions if needed (though SphereCast starts at origin so usually fine)
            if (Owner != null && (hit.collider.gameObject == Owner || hit.collider.transform.IsChildOf(Owner.transform))) 
            {
                // Debug.Log($"[Projectile] Ignored collision with Owner: {hit.collider.name}");
                return;
            }

            // ARCHITECTURAL FIX: Use GetComponentInParent because Colliders are now on children (Hull/Turrets)
            // while the UnitController (IDamageable) is on the Root.
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                if (damageable.GetTeam() != ProjectileTeam)
                {
                    damageable.TakeDamage(Damage);
                    
                    if (damageable.GetUnitType() == UnitType.Missile)
                    {
                        Debug.Log($"<color=green>[INTERCEPTION]</color> CIWS Intercepted {hit.collider.name} at {hit.point}");
                    }
                }
                else
                {
                    return; // Friendly Fire ignored
                }
            }
            
            // VFX
            if (NavalCommand.Systems.VFX.VFXManager.Instance != null)
            {
                var surface = NavalCommand.Systems.VFX.SurfaceResolver.Resolve(hit.collider);
                var context = new NavalCommand.Systems.VFX.ImpactPayload(
                    ImpactProfile,
                    surface,
                    hit.point,
                    hit.normal
                );
                
                Debug.Log($"[ProjectileBehavior] Spawning Impact VFX ({ImpactProfile.Category}, {ImpactProfile.Size} on {surface}) at {hit.point}");
                NavalCommand.Systems.VFX.VFXManager.Instance.SpawnVFX(context);
            }

            Despawn($"Impact with {hit.collider.name}");
        }

        private void Despawn(string reason = "Unknown")
        {
            if (isDespawning) return;
            
            // Diagnostic: Check for instant death
            if (Time.time - _spawnTime < 0.1f)
            {
                Debug.LogWarning($"[Projectile DIAGNOSTIC] Projectile {name} (Team: {ProjectileTeam}) DIED INSTANTLY (<0.1s). Reason: {reason}. Pos: {transform.position}");
            }

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

        // IPredictionProvider Implementation
        public ITargetPredictor GetPredictor(Vector3 observerPos, Vector3 observerVel)
        {
            // Check if we are targeting the observer (or their ship root)
            bool isTargetingObserver = false;
            if (Target != null)
            {
                // Simple check: Is the target root the same as the observer's root?
                // Note: observerPos might be a turret, so we can't just compare positions.
                // Ideally, we'd compare root transforms, but we only have position here.
                // However, for this context, checking distance to target is a reasonable heuristic 
                // if we don't have the observer's transform.
                // BETTER: The observer should pass their root transform if possible, but the interface takes pos/vel.
                // Let's assume if the target is close to the observerPos, it's the target.
                
                float dist = Vector3.Distance(Target.position, observerPos);
                if (dist < 50f) // Heuristic: If target is within 50m of observer, assume it's targeting observer
                {
                    isTargetingObserver = true;
                }
            }

            if (isTargetingObserver && (MovementLogicName == "GuidedMissile" || MovementLogicName == "Torpedo"))
            {
                // We are a guided weapon targeting the observer.
                // Return our specific guidance logic predictor.
                
                // 1. Construct Observer's Predictor (Linear assumption for short term)
                ITargetPredictor observerPredictor = new LinearTargetPredictor(observerPos, observerVel);
                
                // 2. Calculate Scaled Turn Rate
                float scaleFactor = 1.0f;
                if (WorldPhysicsSystem.Instance != null)
                {
                    scaleFactor = WorldPhysicsSystem.Instance.GlobalSpeedScale / WorldPhysicsSystem.Instance.GlobalRangeScale;
                }
                
                // Use Terminal Turn Rate (60f) as that's what matters for interception
                float terminalTurnRate = 60f * scaleFactor;

                return new AugmentedPursuitPredictor(transform.position, Velocity, observerPredictor, terminalTurnRate);
            }
            
            // Default: Linear Prediction
            return new LinearTargetPredictor(transform.position, Velocity);
        }
    }
}
