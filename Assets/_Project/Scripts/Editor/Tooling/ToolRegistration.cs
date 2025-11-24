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

            // World Generation
            reg.Register("World Generation", "Rebuild World (Force Update)", ContentGenerator.RebuildAllContent, "Regenerates all ships, weapons, and configs.");
            reg.Register("World Generation", "Generate Empty Hulls", ContentGenerator.GenerateEmptyHulls, "Creates empty hull prefabs for all weight classes.");
            reg.Register("World Generation", "Generate HUD", ContentGenerator.GenerateHUD, "Rebuilds the HUD UI.");

            // Interaction System
            reg.Register("Interaction System", "Generate Weapon Assets", AssetGenerator.Generate, "Migrates WeaponRegistry to ScriptableObjects.");
            reg.Register("Interaction System", "Validate System", InteractionSystemValidator.ShowWindow, "Checks for configuration errors.");

            // VFX
            reg.Register("VFX", "Rebuild VFX Assets", VFXAutomationTool.RebuildVFXAssets, "Regenerates materials and assigns them to prefabs.");
        }
    }
}
