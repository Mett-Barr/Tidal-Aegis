// DEPRECATED
// This file has been refactored into:
// - NavalCommand.Editor.Generators.VFXAssetGenerator
// 
// Please use Naval Command > Dashboard to access the new tools.

namespace NavalCommand.Editor
{
    public static class VFXAutomationTool
    {
        public static void RebuildVFXAssets()
        {
            // Redirect to new generator
            Generators.VFXAssetGenerator.GenerateAll();
        }
    }
}
