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
        }
        
        private void ConfigureLineRenderer()
        {
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.08f;
            lineRenderer.positionCount = 2;
            lineRenderer.startColor = beamColor;
            lineRenderer.endColor = beamColor;
            lineRenderer.numCapVertices = 5;
            
            // Use emissive material for glow
            if (lineRenderer.material == null || lineRenderer.material.name == "Default-Material")
            {
                Material beamMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                beamMat.SetColor("_BaseColor", beamColor);
                beamMat.SetColor("_EmissionColor", beamColor * 2f);
                beamMat.EnableKeyword("_EMISSION");
                lineRenderer.material = beamMat;
            }
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
            if (target == null) return;
            
            float damage = dps * deltaTime;
            target.TakeDamage(damage);
        }
        
        public void Deactivate()
        {
            isActive = false;
            gameObject.SetActive(false);
        }
    }
}
