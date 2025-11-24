using UnityEditor;
using NavalCommand.Utils;
using NavalCommand.Editor.Tooling;

namespace NavalCommand.Editor
{
    [InitializeOnLoad]
    public static class ToolRegistration
    {
        static ToolRegistration()
        {
            // Wait for Editor to be ready
            EditorApplication.delayCall += RegisterTools;
        }

        private static void RegisterTools()
        {
            var registry = EditorToolRegistry.Instance;

            // --- World Generation (世界生成) ---
            registry.Register("世界生成 (World Gen)", "重建世界 (Rebuild World)", () => {
                HierarchyRestorer.RestoreHierarchy();
            }, "重新生成所有 ScriptableObjects、Prefabs 和配置檔案。 (強制更新)");

            registry.Register("世界生成 (World Gen)", "生成空船殼 (Generate Hulls)", () => {
                ContentRebuilder.GenerateEmptyHulls();
            }, "為所有重量等級生成基礎的船殼 Prefab。");

            // --- Assets & VFX (資源與特效) ---
            registry.Register("資源與特效 (Assets)", "重建特效資源 (Rebuild VFX)", () => {
                Generators.VFXAssetGenerator.GenerateAll();
            }, "重新生成 VFX 材質並將其連結到 Prefab。修復粉紅色材質問題。");

            // --- Debugging (除錯) ---
            registry.Register("除錯工具 (Debug)", "檢查 Shader (Check Shaders)", () => {
                ShaderDebugger.CheckShaders();
            }, "列出專案中可用的 Shader 並輸出到 Log 檔案。");

            registry.Register("除錯工具 (Debug)", "檢查特效材質 (Check Materials)", () => {
                MaterialDebugger.CheckVFXMaterials();
            }, "檢查生成的材質是否正確指派了 Shader。");
            
            // --- UI ---
             registry.Register("介面 (UI)", "生成 HUD (Generate HUD)", () => {
                ContentRebuilder.GenerateHUD();
            }, "生成運行時的儀表板 UI。");
        }
    }
}
