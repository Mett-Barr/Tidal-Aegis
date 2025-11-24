using UnityEngine;
using UnityEditor;
using System.IO;

namespace NavalCommand.Editor
{
    public class VFXAutomationTool
    {
        private const string MATERIAL_PATH = "Assets/_Project/Generated/Materials";
        private const string PREFAB_PATH = "Assets/_Project/Prefabs/VFX";

        [MenuItem("Tools/Naval Command/Rebuild VFX Assets")]
        public static void RebuildVFXAssets()
        {
            EnsureDirectoryExists(MATERIAL_PATH);

            // 1. Regenerate Materials
            // Medium: Bright Orange/White (High Intensity)
            Material matExplosion = CreateMaterial("VFX_Mat_Explosion", new Color(4f, 1.5f, 0.5f, 1f), true); 
            
            // Small: Extreme HDR Orange for visibility (Very High Intensity)
            Material matExplosionSmall = CreateMaterial("VFX_Mat_ExplosionSmall", new Color(8f, 4f, 0.5f, 1f), true); 
            
            // Splash: Blue/White (Water) - Unchanged
            Material matSplash = CreateMaterial("VFX_Mat_Splash", new Color(0.5f, 0.8f, 1f, 0.5f), false);
            
            // Sparks: Yellow (Kinetic)
            Material matSparks = CreateMaterial("VFX_Mat_Sparks", Color.yellow, true);

            // 2. Link to Prefabs & Configure Particles
            LinkMaterialToPrefab("VFX_Explosion_Medium.prefab", matExplosion);
            
            // Special handling for Small Explosion: Make it HUGE for debugging
            LinkMaterialToPrefab("VFX_Explosion_Small.prefab", matExplosionSmall, 5.0f, 1.0f);
            
            LinkMaterialToPrefab("VFX_Splash_Water.prefab", matSplash);
            LinkMaterialToPrefab("VFX_Sparks_Kinetic.prefab", matSparks);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("<color=green><b>[VFX Automation]</b> VFX Assets Rebuilt Successfully!</color>");
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static Material CreateMaterial(string name, Color color, bool isAdditive)
        {
            // Use URP Particles Unlit for stability
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                // Fallback to Standard Particles if URP not found (should not happen in URP project)
                shader = Shader.Find("Particles/Standard Unlit");
                Debug.LogWarning($"[VFX Automation] URP Shader not found, falling back to {shader.name}");
            }

            Material mat = new Material(shader);
            mat.name = name;

            // Set Properties
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color); // Some shaders use _Color
            mat.SetColor("_EmissionColor", color); // Emission for glow
            
            // CRITICAL: URP Particles Unlit often needs a BaseMap even if just white
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", Texture2D.whiteTexture);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", Texture2D.whiteTexture);

            // Transparency Settings
            mat.SetFloat("_Surface", 1.0f); // 1 = Transparent
            mat.SetFloat("_Blend", isAdditive ? 1.0f : 0.0f); // 1 = Additive, 0 = Alpha
            
            // Keywords
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (isAdditive)
            {
                mat.EnableKeyword("_BLENDMODE_ADD");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            }
            else
            {
                mat.EnableKeyword("_BLENDMODE_ALPHA");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }
            
            mat.SetInt("_ZWrite", 0); // Disable ZWrite for transparent particles

            // Save Asset
            string fullPath = $"{MATERIAL_PATH}/{name}.mat";
            AssetDatabase.CreateAsset(mat, fullPath);
            Debug.Log($"[VFX Automation] Created Material: {fullPath}");

            return mat;
        }

        private static void LinkMaterialToPrefab(string prefabName, Material mat, float? overrideSize = null, float? overrideLifetime = null)
        {
            string path = $"{PREFAB_PATH}/{prefabName}";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
            {
                Debug.LogError($"[VFX Automation] Prefab not found at {path}");
                return;
            }

            // Modify Prefab contents
            var renderers = prefab.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterial = mat;
            }

            // Optional: Override Particle Settings
            if (overrideSize.HasValue || overrideLifetime.HasValue)
            {
                var ps = prefab.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    if (overrideSize.HasValue) main.startSize = overrideSize.Value;
                    if (overrideLifetime.HasValue) main.startLifetime = overrideLifetime.Value;
                }
            }

            EditorUtility.SetDirty(prefab);
            Debug.Log($"[VFX Automation] Linked {mat.name} to {prefabName}");
        }
    }
}
