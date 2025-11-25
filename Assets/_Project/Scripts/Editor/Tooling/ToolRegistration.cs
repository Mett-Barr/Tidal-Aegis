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
            registry.Register("資源與特效 (Assets)", "僅重建 VFX (VFX Only)", () => {
                Generators.VFXAssetGenerator.GenerateAll();
            }, "僅重新生成 VFX 材質（不重建整個世界）。用於快速迭代特效效果。注意：「重建世界」已包含此步驟。");

            // --- Debugging (除錯) ---
            registry.Register("除錯工具 (Debug)", "檢查 Shader (Check Shaders)", () => {
                ShaderDebugger.CheckShaders();
            }, "列出專案中可用的 Shader 並輸出到 Log 檔案。");

            registry.Register("除錯工具 (Debug)", "檢查特效材質 (Check Materials)", () => {
                MaterialDebugger.CheckVFXMaterials();
            }, "檢查生成的材質是否正確指派了 Shader。");

            registry.Register("除錯工具 (Debug)", "診斷彈道模型 (Diagnose Projectiles)", () => {
                ProjectileDiagnostics.CheckAllProjectiles();
            }, "檢查所有彈道 Prefab 的模型、材質和渲染器狀態，輸出詳細診斷報告。");

            registry.Register("除錯工具 (Debug)", "刷新彈道 Prefabs (Refresh Prefabs)", () => {
                AssetRefresher.RefreshProjectilePrefabs();
            }, "強制重新導入所有彈道 Prefab，解決 AssetDatabase 緩存問題。");
            
            // --- UI ---
             registry.Register("介面 (UI)", "生成 HUD (Generate HUD)", () => {
                ContentRebuilder.GenerateHUD();
            }, "生成運行時的儀表板 UI。");
        }
    }
}
