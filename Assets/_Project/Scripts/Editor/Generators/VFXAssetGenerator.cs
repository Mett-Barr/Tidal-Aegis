using UnityEngine;
using UnityEditor;
using System.IO;

namespace NavalCommand.Editor.Generators
{
    public static class VFXAssetGenerator
    {
        private const string MATERIAL_PATH = "Assets/_Project/Generated/Materials";
        private const string PREFAB_PATH = "Assets/_Project/Prefabs/VFX";

        public static void GenerateAll()
        {
            EnsureDirectories();
            
            // 1. Explosion
            CreateVFX("VFX_Explosion", Color.red, "Universal Render Pipeline/Particles/Unlit");
            
            // 2. Muzzle Flash
            CreateVFX("VFX_MuzzleFlash", Color.yellow, "Universal Render Pipeline/Particles/Unlit");
            
            // 2b. Laser Muzzle Flash (Cyan, subtle)
            CreateVFX("VFX_MuzzleFlash_Laser", new Color(0f, 1f, 1f, 0.8f), "Universal Render Pipeline/Particles/Unlit");
            
            // 3. Water Splash
            CreateVFX("VFX_WaterSplash", Color.white, "Universal Render Pipeline/Particles/Unlit");
            
            // 4. Smoke
            CreateVFX("VFX_Smoke", Color.gray, "Universal Render Pipeline/Particles/Simple Lit");

            AssetDatabase.SaveAssets();
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(MATERIAL_PATH)) Directory.CreateDirectory(MATERIAL_PATH);
            if (!Directory.Exists(PREFAB_PATH)) Directory.CreateDirectory(PREFAB_PATH);
            AssetDatabase.Refresh();
        }

        private static void CreateVFX(string name, Color color, string shaderName)
        {
            // 1. Material
            Material mat = CreateMaterial(name, color, shaderName);

            // 2. Prefab
            CreatePrefab(name, mat);
        }

        private static Material CreateMaterial(string name, Color color, string shaderName)
        {
            string path = $"{MATERIAL_PATH}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            if (mat == null)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit"); // Fallback
                
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = Shader.Find(shaderName);
            }

            // Configure URP Particle Properties
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            
            // Assign White Texture to prevent Pink Error
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", Texture2D.whiteTexture);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", Texture2D.whiteTexture);

            // Transparency
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0);   // Alpha
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.renderQueue = 3000;

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void CreatePrefab(string name, Material mat)
        {
            string path = $"{PREFAB_PATH}/{name}.prefab";
            GameObject root = new GameObject(name);
            
            // Particle System
            var ps = root.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1f;
            main.startLifetime = 0.5f;
            main.startSpeed = 5f;
            main.startSize = 1f;
            main.loop = false;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 10) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            // Auto-Destruct Script (Optional, but good practice)
            // root.AddComponent<AutoDestroyVFX>(); 

            PrefabUtility.SaveAsPrefabAsset(root, path);
            GameObject.DestroyImmediate(root);
        }
    }
}
