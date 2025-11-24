using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NavalCommand.Editor.Tooling
{
    /// <summary>
    /// Central registry for all Naval Command editor tools.
    /// Uses a functional approach where tools are registered as lambdas.
    /// </summary>
    public class EditorToolRegistry
    {
        private static EditorToolRegistry _instance;
        public static EditorToolRegistry Instance => _instance ??= new EditorToolRegistry();

        private class ToolEntry
        {
            public string Category;
            public string Name;
            public Action Action;
            public string Description;
        }

        private readonly List<ToolEntry> _tools = new List<ToolEntry>();

        public void Register(string category, string name, Action action, string description = "")
        {
            // Remove existing if duplicate to allow hot-reloading
            _tools.RemoveAll(t => t.Category == category && t.Name == name);
            
            _tools.Add(new ToolEntry
            {
                Category = category,
                Name = name,
                Action = action,
                Description = description
            });
        }

        public void DrawGUI()
        {
            EditorGUILayout.LabelField("Naval Command Toolbox", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            var grouped = _tools.GroupBy(t => t.Category).OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    foreach (var tool in group)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button(tool.Name, GUILayout.Height(30)))
                            {
                                tool.Action?.Invoke();
                            }
                            
                            if (!string.IsNullOrEmpty(tool.Description))
                            {
                                EditorGUILayout.LabelField(tool.Description, EditorStyles.miniLabel, GUILayout.Width(200));
                            }
                        }
                    }
                }
                EditorGUILayout.Space();
            }
        }
    }
}
