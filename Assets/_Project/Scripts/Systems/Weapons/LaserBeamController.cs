using UnityEngine;
using NavalCommand.Core;  // Fixed: IDamageable is in Core, not Entities.Interfaces

namespace NavalCommand.Systems.Weapons
{
    /// <summary>
    /// Controls a single laser beam lifecycle.
    /// Manages LineRenderer, raycast hit detection, and continuous DOT damage.
    /// </summary>
    public class LaserBeamController : MonoBehaviour
    {
        private LineRenderer lineRenderer;
        private Transform origin;           // Muzzle/FirePoint
        private IDamageable target;         // Current target
        private float dps;                  // Damage per second
        private float maxRange;
        private Color beamColor;
        
        private bool isActive = false;
        private bool hasKilledCurrentTarget = false;  // Track if we already killed this target
        private int lastKillFrame = -1;     // NEW: Frame counter to ensure one-time VFX trigger
        
        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }
        }
        
        /// <summary>
        /// Initialize beam with target and fire parameters
        /// </summary>
        public void Initialize(Transform muzzle, IDamageable target, float dps, float range, Color color)
        {
            this.origin = muzzle;
            this.target = target;
            this.dps = dps;
            this.maxRange = range;
            this.beamColor = color;
            
            ConfigureLineRenderer();
            isActive = true;
            hasKilledCurrentTarget = false;  // NEW: Reset kill flag for new target
        }
        
        private void ConfigureLineRenderer()
        {
            // 超粗光束（从 0.3m 增加到 0.5m）
            lineRenderer.startWidth = 0.5f;
            lineRenderer.endWidth = 0.45f;
            lineRenderer.positionCount = 2;
            
            // 使用 HDR 青色（超亮）
            Color hdrCyan = new Color(0f, 2f, 2f, 1f);  // HDR 青色（超过1.0的值）
            lineRenderer.startColor = hdrCyan;
            lineRenderer.endColor = hdrCyan;
            lineRenderer.numCapVertices = 0;  // Flat ends to match lens surface
            
            // 创建高发光材质
            Material beamMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            
            // 设置基础颜色和发光
            beamMat.SetColor("_BaseColor", beamColor);
            beamMat.SetColor("_EmissionColor", beamColor * 10f);  // 10倍发光强度
            
            // 启用发光和透明度
            beamMat.EnableKeyword("_EMISSION");
            beamMat.SetFloat("_Surface", 1);  // Transparent
            beamMat.renderQueue = 3000;       // Transparent queue
            
            lineRenderer.material = beamMat;
            
            Debug.Log($"[LaserBeam] Configured: Width={lineRenderer.startWidth}, Color={beamColor}, Emission={beamColor * 10f}");
        }
        
        private void Update()
        {
            if (!isActive) return;
            
            // CRITICAL: Validate target liveness (Aegis CEC Principle)
            // Check if target is still valid, in range, AND ALIVE
            // This prevents wasting energy on targets killed by other weapons (e.g. CIWS)
            // Follows real-world Cooperative Engagement Capability doctrine
            if (target == null || target.IsDead() || !IsTargetInRange())
            {
                Deactivate();
                return;
            }
            
            // Update beam visual position
            UpdateBeamPosition();
            
            // Apply continuous damage
            ApplyDamage(Time.deltaTime);
        }
        
        private bool IsTargetInRange()
        {
            if (target == null || origin == null) return false;
            
            // Get target position (assuming target is a Component)
            var targetComponent = target as Component;
            if (targetComponent == null) return false;
            
            float distance = Vector3.Distance(origin.position, targetComponent.transform.position);
            return distance <= maxRange;
        }
        
        private void UpdateBeamPosition()
        {
            if (origin == null) return;
            
            lineRenderer.SetPosition(0, origin.position);
            lineRenderer.SetPosition(1, GetHitPoint());
        }
        
        private Vector3 GetHitPoint()
        {
            if (origin == null) return Vector3.zero;
            
            // CRITICAL FIX: Beam MUST follow turret's actual forward direction
            // NOT force-aim at target center (which violates physics realism)
            // This ensures "point-to-hit" accuracy: if turret points at target, beam hits; if not, beam misses
            Vector3 beamDirection = origin.forward;
            
            // Raycast along the beam direction to find actual hit point
            Ray ray = new Ray(origin.position, beamDirection);
            
            // Try to hit anything in the beam path (prefer specific target, but accept any hit)
            if (Physics.Raycast(ray, out RaycastHit hit, maxRange))
            {
                // Verify if we hit the intended target
                var targetComponent = target as Component;
                if (targetComponent != null && hit.collider.transform.IsChildOf(targetComponent.transform))
                {
                    // Success: Beam is aligned and hitting the target
                    return hit.point;
                }
                else
                {
                    // Beam hit something else (obstacle or different target)
                    // Still return hit point for visual feedback
                    return hit.point;
                }
            }
            
            // Beam didn't hit anything - extend to max range
            return origin.position + beamDirection * maxRange;
        }
        
        private void ApplyDamage(float deltaTime)
        {
            if (target == null) return;
            
            if (hasKilledCurrentTarget) return;

            // CRITICAL FIX: Calculate hit point BEFORE dealing damage
            // AND verify that beam is actually hitting the target
            Vector3 potentialHitPoint = GetHitPoint();
            
            // Verify beam is hitting the intended target
            var targetComponent = target as Component;
            if (targetComponent != null)
            {
                Vector3 beamDirection = origin.forward;
                Ray ray = new Ray(origin.position, beamDirection);
                
                bool isHittingTarget = false;
                
                // Check if raycast hits the target
                if (Physics.Raycast(ray, out RaycastHit hit, maxRange))
                {
                    // Verify the hit object is part of the target hierarchy
                    if (hit.collider.transform.IsChildOf(targetComponent.transform) || 
                        hit.collider.transform == targetComponent.transform)
                    {
                        isHittingTarget = true;
                        potentialHitPoint = hit.point;
                    }
                }
                
                // Only apply damage if beam is actually hitting the target
                if (!isHittingTarget)
                {
                    // Beam is not aligned with target - no damage
                    // This enforces "point-to-hit" principle
                    return;
                }
            }
            
            bool wasAlive = !target.IsDead();
            float damage = dps * deltaTime;
            target.TakeDamage(damage);
            bool isDeadNow = target.IsDead();
            
            if (wasAlive && isDeadNow && lastKillFrame != Time.frameCount)
            {
                // Spawn explosion at the exact hit point (Surface)
                SpawnExplosionVFX(potentialHitPoint);
                
                lastKillFrame = Time.frameCount;
                hasKilledCurrentTarget = true;
                
                // CRITICAL: Immediately deactivate beam on kill for clean visual transition
                // WeaponController will handle retargeting after cooldown period
                // This follows game design pattern from X4/Stellaris: beam shuts off -> cooldown -> retarget
                Deactivate();
            }
        }
        
        /// <summary>
        /// Spawn explosion VFX at impact point (attacker-driven pattern, like CIWS projectiles)
        /// </summary>
        private void SpawnExplosionVFX(Vector3 position)
        {
            if (NavalCommand.Systems.VFX.VFXManager.Instance == null) return;

            // Use Explosive/Medium to ensure the explosion is clearly visible
            // We use the surface position (from GetHitPoint) to ensure it's not obscured
            var payload = new NavalCommand.Systems.VFX.ImpactPayload(
                new NavalCommand.Systems.VFX.ImpactProfile(NavalCommand.Systems.VFX.ImpactCategory.Explosive, NavalCommand.Systems.VFX.ImpactSize.Medium),
                NavalCommand.Systems.VFX.SurfaceType.Armor_Metal, // Assume Metal for missiles
                position,
                -transform.forward
            );
            
            NavalCommand.Systems.VFX.VFXManager.Instance.SpawnVFX(payload);
        }
        
        public void Deactivate()
        {
            isActive = false;
            gameObject.SetActive(false);
        }
    }
}
