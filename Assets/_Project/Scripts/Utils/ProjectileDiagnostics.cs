using UnityEngine;
using UnityEditor;
using System.Text;

namespace NavalCommand.Utils
{
    public class ProjectileDiagnostics
    {
        public static void CheckAllProjectiles()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== PROJECTILE DIAGNOSTICS ===\n");

            string[] projectileNames = new string[]
            {
                "Projectile_FlagshipGun",
                "Projectile_Missile",
                "Projectile_Torpedo",
                "Projectile_Autocannon",
                "Projectile_CIWS"
            };

            foreach (string name in projectileNames)
            {
                sb.AppendLine($"--- {name} ---");
                
                // Load Prefab
                string prefabPath = $"Assets/_Project/Prefabs/Projectiles/{name}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                
                if (prefab == null)
                {
                    sb.AppendLine($"  [ERROR] Prefab not found at {prefabPath}");
                    continue;
                }

                // Check Model GameObject
                Transform modelTransform = prefab.transform.Find("Model");
                if (modelTransform == null)
                {
                    sb.AppendLine("  [ERROR] Model GameObject not found");
                    continue;
                }

                sb.AppendLine($"  Model GameObject: FOUND");
                sb.AppendLine($"  Model Active: {modelTransform.gameObject.activeSelf}");
                sb.AppendLine($"  Model Children: {modelTransform.childCount}");

                // Check all MeshRenderers
                MeshRenderer[] renderers = modelTransform.GetComponentsInChildren<MeshRenderer>();
                sb.AppendLine($"  MeshRenderers Found: {renderers.Length}");

                for (int i = 0; i < renderers.Length; i++)
                {
                    MeshRenderer renderer = renderers[i];
                    sb.AppendLine($"\n  Renderer {i} ({renderer.gameObject.name}):");
                    sb.AppendLine($"    Enabled: {renderer.enabled}");
                    sb.AppendLine($"    Materials Count: {renderer.sharedMaterials.Length}");

                    foreach (Material mat in renderer.sharedMaterials)
                    {
                        if (mat == null)
                        {
                            sb.AppendLine("    [ERROR] Material is NULL");
                            continue;
                        }

                        sb.AppendLine($"    Material: {mat.name}");
                        sb.AppendLine($"      Shader: {mat.shader.name}");
                        
                        // Check key properties
                        if (mat.HasProperty("_BaseColor"))
                        {
                            Color baseColor = mat.GetColor("_BaseColor");
                            sb.AppendLine($"      _BaseColor: {baseColor} (Alpha: {baseColor.a})");
                        }
                        if (mat.HasProperty("_Color"))
                        {
                            Color color = mat.GetColor("_Color");
                            sb.AppendLine($"      _Color: {color} (Alpha: {color.a})");
                        }
                        if (mat.HasProperty("_Surface"))
                        {
                            float surface = mat.GetFloat("_Surface");
                            sb.AppendLine($"      _Surface: {surface} (0=Opaque, 1=Transparent)");
                        }
                        if (mat.HasProperty("_Blend"))
                        {
                            float blend = mat.GetFloat("_Blend");
                            sb.AppendLine($"      _Blend: {blend}");
                        }

                        sb.AppendLine($"      RenderQueue: {mat.renderQueue}");
                        sb.AppendLine($"      GlobalIllumination: {mat.globalIlluminationFlags}");
                    }
                }

                // Check Material Asset
                string matPath = $"Assets/_Project/Generated/Materials/Mat_{name}.mat";
                Material materialAsset = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (materialAsset != null)
                {
                    sb.AppendLine($"\n  Material Asset ({matPath}):");
                    sb.AppendLine($"    Shader: {materialAsset.shader.name}");
                    if (materialAsset.HasProperty("_BaseColor"))
                    {
                        sb.AppendLine($"    _BaseColor: {materialAsset.GetColor("_BaseColor")}");
                    }
                }
                else
                {
                    sb.AppendLine($"\n  [WARNING] Material asset not found at {matPath}");
                }

                sb.AppendLine();
            }

            sb.AppendLine("=== END DIAGNOSTICS ===");

            // Write to file
            string outputPath = "projectile_diagnostics.txt";
            System.IO.File.WriteAllText(outputPath, sb.ToString());
            
            Debug.Log($"Projectile diagnostics written to {outputPath}");
            Debug.Log(sb.ToString());
        }
    }
}
