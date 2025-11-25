using UnityEngine;
using UnityEditor;

namespace NavalCommand.Utils
{
    public static class RedDotDebugger
    {
        [MenuItem("Tools/Debug Tools/Find All Red Objects")]
        public static void FindRedObjects()
        {
            Debug.Log("=== SEARCHING FOR RED OBJECTS ===\n");
            
            int count = 0;
            
            // Find all ParticleSystems
            var allPS = GameObject.FindObjectsOfType<ParticleSystem>();
            Debug.Log($"Found {allPS.Length} active ParticleSystems:");
            foreach (var ps in allPS)
            {
                if (ps.particleCount > 0)
                {
                    var main = ps.main;
                    Color startColor = main.startColor.color;
                    
                    // Check if it's reddish
                    if (startColor.r > 0.5f && startColor.g < 0.5f)
                    {
                        count++;
                        Debug.LogWarning($"  🔴 RED PARTICLE: {ps.name} ({ps.transform.parent?.name})");
                        Debug.Log($"     - Position: {ps.transform.position}");
                        Debug.Log($"     - Count: {ps.particleCount}");
                        Debug.Log($"     - Color: {startColor}");
                        Debug.Log($"     - IsPlaying: {ps.isPlaying}");
                        Debug.Log($"     - Emission: {ps.emission.enabled}");
                        Debug.Log($"     - Renderer: {ps.GetComponent<ParticleSystemRenderer>().enabled}");
                    }
                }
            }
            
            // Find all projectiles
            var allProjectiles = GameObject.FindObjectsOfType<NavalCommand.Entities.Projectiles.ProjectileBehavior>();
            Debug.Log($"\nFound {allProjectiles.Length} active Projectiles:");
            foreach (var proj in allProjectiles)
            {
                Debug.Log($"  - {proj.name} at {proj.transform.position}");
            }
            
            // Find all VFX
            var allVFX = GameObject.FindObjectsOfType<NavalCommand.VFX.AutoFollowVFX>();
            Debug.Log($"\nFound {allVFX.Length} active Trail VFX:");
            foreach (var vfx in allVFX)
            {
                Debug.Log($"  - {vfx.name} at {vfx.transform.position}");
                
                var flames = vfx.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in flames)
                {
                    if (ps.name.Contains("Flame"))
                    {
                        Debug.Log($"      Flame: count={ps.particleCount}, playing={ps.isPlaying}, renderer={ps.GetComponent<ParticleSystemRenderer>().enabled}");
                    }
                }
            }
            
            // Find all mesh renderers with red materials
            var allRenderers = GameObject.FindObjectsOfType<MeshRenderer>();
            Debug.Log($"\nChecking {allRenderers.Length} MeshRenderers for red materials:");
            foreach (var renderer in allRenderers)
            {
                if (renderer.enabled && renderer.sharedMaterial != null)
                {
                    Color color = Color.white;
                    if (renderer.sharedMaterial.HasProperty("_Color"))
                    {
                        color = renderer.sharedMaterial.GetColor("_Color");
                    }
                    else if (renderer.sharedMaterial.HasProperty("_BaseColor"))
                    {
                        color = renderer.sharedMaterial.GetColor("_BaseColor");
                    }
                    
                    if (color.r > 0.5f && color.g < 0.5f)
                    {
                        Debug.LogWarning($"  🔴 RED MESH: {renderer.gameObject.name} at {renderer.transform.position}");
                        Debug.Log($"     Color: {color}");
                    }
                }
            }
            
            Debug.Log($"\n=== FOUND {count} RED PARTICLE SYSTEMS ===");
        }
    }
}
