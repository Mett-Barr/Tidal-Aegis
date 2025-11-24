using UnityEditor;
using UnityEngine;

namespace NavalCommand.Editor.Tooling
{
    public class NavalCommandDashboard : EditorWindow
    {
        [MenuItem("Naval Command/Open Dashboard %#d", priority = 0)]
        public static void Open()
        {
            GetWindow<NavalCommandDashboard>("NC Dashboard");
        }

        private void OnGUI()
        {
            EditorToolRegistry.Instance.DrawGUI();
        }
    }
}
