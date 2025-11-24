using UnityEngine;
using UnityEditor;

public class MaterialDebugger
{
    [MenuItem("NavalCommand/Debug/Check VFX Materials")]
    public static void CheckVFXMaterials()
    {
        string[] matNames = new string[]
        {
            "VFX_Mat_Splash",
            "VFX_Mat_Explosion",
            "VFX_Mat_Sparks",
            "VFX_Mat_ExplosionSmall"
        };

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("--- Material Debugger ---");

        foreach (var name in matNames)
        {
            string path = $"Assets/_Project/Generated/Materials/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
            {
                sb.AppendLine($"Material: {name}");
                sb.AppendLine($"  Shader: {mat.shader.name}");
                sb.AppendLine($"  Render Queue: {mat.renderQueue}");
                sb.AppendLine($"  Has _BaseColor: {mat.HasProperty("_BaseColor")}");
                sb.AppendLine($"  Has _Color: {mat.HasProperty("_Color")}");
            }
            else
            {
                sb.AppendLine($"[MISSING] Material at {path}");
            }
        }
        
        sb.AppendLine("--- End Debug ---");
        System.IO.File.WriteAllText("material_debug_log.txt", sb.ToString());
        Debug.Log("Material debug log written to material_debug_log.txt");
    }
}
