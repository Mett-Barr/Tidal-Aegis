using UnityEngine;
using UnityEditor;

namespace NavalCommand.Utils
{
    /// <summary>
    /// Enhanced VFX checker with TrailRenderer diagnostics
    /// </summary>
    public static class VFXQuickCheck
    {
        [MenuItem("Tools/Debug Tools/Quick VFX Check")]
        public static void CheckVFX()
        {
            Debug.Log("=== QUICK VFX CHECK ===\n");
            
            CheckPrefab("Assets/_Project/Prefabs/VFX/Projectile/VFX_MissileTrail.prefab");
            CheckPrefab("Assets/_Project/Prefabs/VFX/Projectile/VFX_TorpedoBubbles.prefab");
            CheckPrefab("Assets/_Project/Prefabs/VFX/Projectile/VFX_TracerGlow.prefab");
            
            Debug.Log("\n=== CHECK COMPLETE ===");
        }
        
        private static void CheckPrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"❌ Not found: {path}");
                return;
            }
            
            Debug.Log($"\n✓ {prefab.name}:");
            
            // Check TrailRenderer
            TrailRenderer trail = prefab.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                Debug.Log($"  TrailRenderer: ✓ (time={trail.time}s)");
                Debug.Log($"    - Width: {trail.startWidth} → {trail.endWidth}");
                Debug.Log($"    - Material: {trail.sharedMaterial?.name ?? "NULL"}");
                Debug.Log($"    - Emitting: {trail.emitting}");
                Debug.Log($"    - MinVertexDistance: {trail.minVertexDistance}");
                
                // Check gradient
                var gradient = trail.colorGradient;
                Debug.Log($"    - Gradient alpha keys: {gradient.alphaKeys.Length}");
                foreach (var key in gradient.alphaKeys)
                {
                    Debug.Log($"      • Alpha {key.alpha:F2} @ position {key.time:F2}");
                }
            }
            else
            {
                Debug.Log($"  TrailRenderer: ❌ MISSING");
            }
            
            // Check ParticleSystems
            ParticleSystem[] particles = prefab.GetComponentsInChildren<ParticleSystem>();
            Debug.Log($"  ParticleSystems: {particles.Length}");
            foreach (var ps in particles)
            {
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                Debug.Log($"    - {ps.name}: lifetime={ps.main.startLifetime.constant}s, emission={ps.emission.rateOverTime.constant}, material={renderer.sharedMaterial?.name ?? "NULL"}");
            }
            
            // Check AutoFollowVFX
            var autoFollow = prefab.GetComponent<NavalCommand.VFX.AutoFollowVFX>();
            Debug.Log($"  AutoFollowVFX: {(autoFollow != null ? $"✓ (delay={autoFollow.AutoDestructDelay}s)" : "❌ MISSING")}");
        }
    }
}
