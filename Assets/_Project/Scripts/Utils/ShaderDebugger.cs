using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class ShaderDebugger
{
    [MenuItem("NavalCommand/Debug/Check Shaders")]
    public static void CheckShaders()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("--- Shader Debugger ---");
        
        var pipeline = GraphicsSettings.currentRenderPipeline;
        sb.AppendLine($"Current Render Pipeline: {(pipeline != null ? pipeline.GetType().Name : "None (Built-in)")}");

        string[] targetShaders = new string[]
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Simple Lit",
            "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Particles/Unlit",
            "Universal Render Pipeline/Particles/Simple Lit",
            "Particles/Standard Unlit",
            "Standard",
            "Mobile/Particles/Alpha Blended"
        };

        foreach (var name in targetShaders)
        {
            Shader s = Shader.Find(name);
            if (s != null)
            {
                sb.AppendLine($"[FOUND] {name}");
            }
            else
            {
                sb.AppendLine($"[MISSING] {name}");
            }
        }
        
        sb.AppendLine("--- End Debug ---");
        
        string path = "shader_debug_log.txt";
        System.IO.File.WriteAllText(path, sb.ToString());
        Debug.Log($"Shader debug log written to {path}");
        AssetDatabase.Refresh();
    }
}
