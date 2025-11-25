using UnityEngine;
using UnityEditor;

namespace NavalCommand.Utils
{
    /// <summary>
    /// Comprehensive VFX debugger - finds ALL visual artifacts after missile impact
    /// </summary>
    public static class ComprehensiveVFXDebugger
    {
        [MenuItem("Tools/Debug Tools/Find All Visual Artifacts After Impact")]
        public static void FindAllArtifacts()
        {
            Debug.Log("=== SMART ARTIFACT SCAN (Filtering for RED/GREEN only) ===\n");
            
            int suspiciousCount = 0;

            // 1. Scan Particle Systems (Looking for Green Flame residue)
            var allPS = GameObject.FindObjectsOfType<ParticleSystem>();
            foreach (var ps in allPS)
            {
                // Filter: Only care about active/visible ones
                if (!ps.gameObject.activeInHierarchy) continue;

                var main = ps.main;
                Color color = main.startColor.color;
                
                // Check if GREEN (Flame) or RED
                bool isGreen = color.g > 0.8f && color.r < 0.5f;
                bool isRed = color.r > 0.8f && color.g < 0.5f;
                
                if (isGreen || isRed)
                {
                    suspiciousCount++;
                    string colorName = isGreen ? "<color=green>GREEN</color>" : "<color=red>RED</color>";
                    Debug.LogWarning($"[SUSPICIOUS PARTICLE] {colorName} | Name: {ps.name} | Parent: {ps.transform.parent?.name} | Count: {ps.particleCount} | Emitting: {ps.isEmitting}");
                }
            }
            
            // 2. Scan MeshRenderers (Looking for Red Missile Body residue)
            var allRenderers = GameObject.FindObjectsOfType<MeshRenderer>();
            foreach (var rend in allRenderers)
            {
                if (!rend.enabled || !rend.gameObject.activeInHierarchy) continue;
                if (rend.sharedMaterial == null) continue;
                
                Color color = Color.white;
                if (rend.sharedMaterial.HasProperty("_Color"))
                    color = rend.sharedMaterial.GetColor("_Color");
                else if (rend.sharedMaterial.HasProperty("_BaseColor"))
                    color = rend.sharedMaterial.GetColor("_BaseColor");
                
                // Check if RED (Missile Body) or GREEN
                bool isRed = color.r > 0.8f && color.g < 0.5f && color.b < 0.5f;
                bool isGreen = color.g > 0.8f && color.r < 0.5f;

                if (isRed || isGreen)
                {
                    suspiciousCount++;
                    string colorName = isGreen ? "<color=green>GREEN</color>" : "<color=red>RED</color>";
                    Debug.LogWarning($"[SUSPICIOUS MESH] {colorName} | Name: {rend.gameObject.name} | Parent: {rend.transform.parent?.name} | Pos: {rend.transform.position}");
                }
            }
            
            // 3. Scan TrailRenderers (Just in case)
            var allTrails = GameObject.FindObjectsOfType<TrailRenderer>();
            foreach (var trail in allTrails)
            {
                if (!trail.enabled || !trail.gameObject.activeInHierarchy) continue;
                
                // Check material color if possible, or just log active trails that aren't smoke (Smoke is usually grey/white)
                Color color = Color.white;
                if (trail.sharedMaterial != null && trail.sharedMaterial.HasProperty("_Color"))
                    color = trail.sharedMaterial.GetColor("_Color");
                    
                bool isRed = color.r > 0.8f && color.g < 0.5f;
                bool isGreen = color.g > 0.8f && color.r < 0.5f;
                
                if (isRed || isGreen)
                {
                    suspiciousCount++;
                    string colorName = isGreen ? "<color=green>GREEN</color>" : "<color=red>RED</color>";
                    Debug.LogWarning($"[SUSPICIOUS TRAIL] {colorName} | Name: {trail.gameObject.name} | Parent: {trail.transform.parent?.name}");
                }
            }

            if (suspiciousCount == 0)
            {
                Debug.Log("<color=cyan>CLEAN: No suspicious Red/Green artifacts found.</color>");
            }
            else
            {
                Debug.LogError($"FOUND {suspiciousCount} SUSPICIOUS OBJECTS! Check logs above.");
            }
        }
    }
}
