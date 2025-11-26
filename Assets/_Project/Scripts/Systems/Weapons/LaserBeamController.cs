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
            lineRenderer.numCapVertices = 10;  // 更圆滑的端点
            
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
            
            // Check if target is still valid
            if (target == null || !IsTargetInRange())
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
            var targetComponent = target as Component;
            if (targetComponent == null) return origin.position;
            
            Vector3 targetPos = targetComponent.transform.position;
            Vector3 direction = (targetPos - origin.position).normalized;
            
            // Raycast to find exact hit point
            if (Physics.Raycast(origin.position, direction, out RaycastHit hit, maxRange))
            {
                return hit.point;
            }
            
            // No obstacle, point directly at target (clamped to range)
            float distance = Mathf.Min(Vector3.Distance(origin.position, targetPos), maxRange);
            return origin.position + direction * distance;
        }
        
        private void ApplyDamage(float deltaTime)
        {
            if (target == null)
            {
                Debug.LogWarning("[LaserBeam] ApplyDamage: target is NULL");
                return;
            }
            
            // If we already killed this target, stop attacking
            if (hasKilledCurrentTarget)
            {
                return;  // Don't continue damaging/checking
            }
            
            // CRITICAL: Check BEFORE TakeDamage
            bool wasAlive = !target.IsDead();
            
            float damage = dps * deltaTime;
            target.TakeDamage(damage);
            
            // Check after damage
            bool isDeadNow = target.IsDead();
            
            // NEW: Frame-based VFX trigger (CIWS-style one-time event)
            if (wasAlive && isDeadNow && lastKillFrame != Time.frameCount)
            {
                Debug.Log($"<color=green>[LaserBeam]</color> KILLING BLOW (Frame {Time.frameCount})! Spawning explosion VFX!");
                SpawnExplosionVFX(GetHitPoint());
                
                lastKillFrame = Time.frameCount;     // Mark this frame
                hasKilledCurrentTarget = true;        // Mark this target as killed
                
                // Immediately deactivate beam (like CIWS projectile despawning)
                Deactivate();
            }
            else if (!wasAlive && lastKillFrame != Time.frameCount)
            {
                // Target was already dead when beam started - this should not happen
                Debug.LogWarning($"<color=red>[LaserBeam]</color> Target already dead at beam start (Frame {Time.frameCount})");
                Deactivate();
            }
        }
        
        /// <summary>
        /// Spawn explosion VFX at impact point (attacker-driven pattern, like CIWS projectiles)
        /// </summary>
        private void SpawnExplosionVFX(Vector3 position)
        {
            Debug.Log($"<color=cyan>[LaserBeam]</color> SpawnExplosionVFX called at {position}");
            
            if (NavalCommand.Systems.VFX.VFXManager.Instance == null)
            {
                Debug.LogError("<color=red>[LaserBeam]</color> VFXManager.Instance is NULL!");
                return;
            }
            
            Debug.Log($"<color=cyan>[LaserBeam]</color> VFXManager found, creating payload...");
            
            // Option A: Unified missile explosion - use SAME profile as CIWS (Kinetic, Small)
            // This ensures all missile kills look the same regardless of weapon type
            var impactProfile = new NavalCommand.Systems.VFX.ImpactProfile(
                NavalCommand.Systems.VFX.ImpactCategory.Kinetic,  // Same as CIWS
                NavalCommand.Systems.VFX.ImpactSize.Small
            );
            
            var payload = new NavalCommand.Systems.VFX.ImpactPayload(
                impactProfile,
                NavalCommand.Systems.VFX.SurfaceType.Air,  // Assume air target (missiles/aircraft)
                position,
                Vector3.up
            );
            
            Debug.Log($"<color=cyan>[LaserBeam]</color> Calling VFXManager.SpawnVFX with Kinetic/Small profile");
            NavalCommand.Systems.VFX.VFXManager.Instance.SpawnVFX(payload);
            Debug.Log($"<color=green>[LaserBeam]</color> VFX spawn call completed!");
        }
        
        public void Deactivate()
        {
            isActive = false;
            gameObject.SetActive(false);
        }
    }
}
