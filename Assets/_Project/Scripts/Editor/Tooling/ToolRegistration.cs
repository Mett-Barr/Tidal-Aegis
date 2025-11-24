using UnityEditor;
using NavalCommand.Utils; // For ContentGenerator
using NavalCommand.Editor; // For AssetGenerator, VFXAutomationTool, Validator

namespace NavalCommand.Editor.Tooling
{
    [InitializeOnLoad]
    public static class ToolRegistration
    {
        static ToolRegistration()
        {
            RegisterAll();
        }

        private static void RegisterAll()
        {
            var reg = EditorToolRegistry.Instance;

            // Scene Management (Primary Tool)
            reg.Register("場景管理 (Scene)", "一鍵還原場景 (Restore Hierarchy)", HierarchyRestorer.RestoreHierarchy, "還原所有必要的場景物件 (GameManager, SpawningSystem, UI, Camera, Physics) 並修復設定。");

            // Advanced Tools (Consolidated)
            reg.Register("進階工具 (Advanced)", "重建所有資源 (Rebuild All)", ContentGenerator.RebuildAllContent, "重新生成所有船艦、武器數據與設定檔 (修復數據錯誤)。");
            reg.Register("進階工具 (Advanced)", "驗證系統 (Validate)", InteractionSystemValidator.ShowWindow, "檢查系統設定是否正確。");
            
            // Removed redundant individual generation tools as they are covered by RebuildAllContent or RestoreHierarchy
        }
    }
}
