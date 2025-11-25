using UnityEngine;
using UnityEditor;

namespace NavalCommand.Utils
{
    /// <summary>
    /// Runtime VFX debugger - attach to missile trail VFX to see what's happening
    /// </summary>
    public static class VFXRuntimeDebugger
    {
        [MenuItem("Tools/Debug Tools/Debug Active VFX")]
        public static void DebugActiveVFX()
        {
            var allVFX = GameObject.FindObjectsOfType<NavalCommand.VFX.AutoFollowVFX>();
            Debug.Log($"=== FOUND {allVFX.Length} ACTIVE VFX ===\n");
            
            foreach (var vfx in allVFX)
            {
                Debug.Log($"\n--- {vfx.gameObject.name} ---");
                Debug.Log($"Position: {vfx.transform.position}");
                Debug.Log($"Active: {vfx.gameObject.activeSelf}");
                
                var particles = vfx.GetComponentsInChildren<ParticleSystem>(true);
                Debug.Log($"Particle Systems: {particles.Length}");
                
                foreach (var ps in particles)
                {
                    Debug.Log($"  • {ps.name}:");
                    Debug.Log($"    - Active: {ps.gameObject.activeSelf}");
                    Debug.Log($"    - IsPlaying: {ps.isPlaying}");
                    Debug.Log($"    - ParticleCount: {ps.particleCount}");
                    Debug.Log($"    - Emission.enabled: {ps.emission.enabled}");
                    Debug.Log($"    - Main.loop: {ps.main.loop}");
                    Debug.Log($"    - Lifetime: {ps.main.startLifetime.constant}s");
                    
                    var renderer = ps.GetComponent<ParticleSystemRenderer>();
                    Debug.Log($"    - Renderer.enabled: {renderer.enabled}");
                    Debug.Log($"    - RenderMode: {renderer.renderMode}");
                }
                
                var trail = vfx.GetComponent<TrailRenderer>();
                if (trail != null)
                {
                    Debug.Log($"TrailRenderer:");
                    Debug.Log($"  - Enabled: {trail.enabled}");
                    Debug.Log($"  - Emitting: {trail.emitting}");
                    Debug.Log($"  - PositionCount: {trail.positionCount}");
                }
            }
            
            Debug.Log("\n=== DEBUG COMPLETE ===");
        }
    }
}
