using UnityEditor;
using UnityEngine;

namespace NavalCommand.Editor.Tooling
{
    /// <summary>
    /// Unified tools dashboard for all Naval Command development tools.
    /// </summary>
    public class ToolsDashboard : EditorWindow
    {
        [MenuItem("Tools/Naval Command Dashboard %#d", priority = 0)]  // Ctrl+Shift+D
        public static void Open()
        {
            GetWindow<ToolsDashboard>("Tools");
        }

        private void OnGUI()
        {
            EditorToolRegistry.Instance.DrawGUI();
        }
    }
}
